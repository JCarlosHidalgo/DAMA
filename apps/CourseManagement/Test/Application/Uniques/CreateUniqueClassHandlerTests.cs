using AutoMapper;

using Backend.Application.Infrastructure;
using Backend.Application.Uniques;
using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Groups;
using Backend.DB.Daos.Abstract.Single.Uniques;
using Backend.Dtos;
using Backend.Dtos.Uniques.Input;
using Backend.Dtos.Uniques.Output;
using Backend.Entities;
using Backend.Entities.Uniques;
using Backend.Results.Uniques;

using Moq;

namespace Test.Application.Uniques;

[TestFixture]
public class CreateUniqueClassHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid GroupId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private Mock<IUniqueClassDao> uniqueClassDao = null!;
    private Mock<IClassGroupDao> classGroupDao = null!;
    private Mock<IClassCreationCoordinator<UniqueClass>> coordinator = null!;
    private Mock<IClaimContext> claimContext = null!;
    private Mock<IClassBuilder> classBuilder = null!;
    private Mock<IMapper> mapper = null!;
    private CreateUniqueClassHandler handler = null!;

    [SetUp]
    public void SetUp()
    {
        uniqueClassDao = new Mock<IUniqueClassDao>(MockBehavior.Strict);
        classGroupDao = new Mock<IClassGroupDao>(MockBehavior.Strict);
        coordinator = new Mock<IClassCreationCoordinator<UniqueClass>>(MockBehavior.Strict);
        claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        classBuilder = new Mock<IClassBuilder>(MockBehavior.Strict);
        mapper = new Mock<IMapper>(MockBehavior.Strict);
        claimContext.SetupGet(context => context.TenantId).Returns(TenantId);
        handler = new CreateUniqueClassHandler(
            uniqueClassDao.Object,
            classGroupDao.Object,
            coordinator.Object,
            claimContext.Object,
            classBuilder.Object,
            mapper.Object);
    }

    private static CreateUniqueClassDto ValidPayload(Guid teacherId) => new()
    {
        Date = new DateOnly(2026, 7, 4),
        MaxStudentLimit = 0,
        StartTime = new TimeOnly(15, 0),
        EndTime = new TimeOnly(16, 0),
        CourseId = Guid.NewGuid(),
        GroupId = GroupId,
        Teachers =
        [
            new ClassTeacherDto { TeacherId = teacherId, TeacherName = "Profesor" }
        ],
        ExternalReference = "ref-1"
    };

    private static List<ClassTeacher> TeacherEntities(Guid teacherId) =>
    [
        new() { TeacherId = teacherId, TeacherName = "Profesor" }
    ];

    [Test]
    public async Task Handle_WhenGroupDoesNotExist_ReturnsGroupNotFoundAndSkipsCoordinator()
    {
        var teacherId = Guid.NewGuid();
        CreateUniqueClassDto payload = ValidPayload(teacherId);
        List<ClassTeacher> teachers = TeacherEntities(teacherId);

        mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(payload.Teachers)).Returns(teachers);
        classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, GroupId)).ReturnsAsync(false);

        CreateUniqueClassResult result = await handler.Handle(new CreateUniqueClassCommand(payload));

        Assert.That(result, Is.InstanceOf<CreateUniqueClassResult.GroupNotFound>());
        uniqueClassDao.VerifyNoOtherCalls();
        coordinator.VerifyNoOtherCalls();
        classBuilder.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Handle_WhenGroupOverlap_ReturnsGroupOverlapConflictAndSkipsCoordinator()
    {
        var teacherId = Guid.NewGuid();
        CreateUniqueClassDto payload = ValidPayload(teacherId);
        List<ClassTeacher> teachers = TeacherEntities(teacherId);

        mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(payload.Teachers)).Returns(teachers);
        classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, GroupId)).ReturnsAsync(true);
        uniqueClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, GroupId, payload.Date, payload.StartTime, payload.EndTime, null))
            .ReturnsAsync(true);

        CreateUniqueClassResult result = await handler.Handle(new CreateUniqueClassCommand(payload));

        Assert.That(result, Is.InstanceOf<CreateUniqueClassResult.GroupOverlapConflict>());
        coordinator.VerifyNoOtherCalls();
        classBuilder.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Handle_WhenCoordinatorReturnsCreated_ReturnsCreatedWithMappedDto()
    {
        var teacherId = Guid.NewGuid();
        CreateUniqueClassDto payload = ValidPayload(teacherId);
        List<ClassTeacher> teachers = TeacherEntities(teacherId);
        var built = new UniqueClass { Id = Guid.NewGuid(), TenantId = TenantId, GroupId = GroupId };
        var mapped = new GetUniqueClassDto
        {
            Id = built.Id,
            Date = payload.Date,
            MaxStudentLimit = payload.MaxStudentLimit,
            StartTime = payload.StartTime,
            EndTime = payload.EndTime,
            CourseId = payload.CourseId,
            GroupId = GroupId,
            GroupName = "Grupo",
            Teachers = []
        };

        mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(payload.Teachers)).Returns(teachers);
        classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, GroupId)).ReturnsAsync(true);
        uniqueClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, GroupId, payload.Date, payload.StartTime, payload.EndTime, null))
            .ReturnsAsync(false);
        classBuilder.Setup(builder => builder.BuildUniqueClass(TenantId, payload.CourseId, GroupId, payload, teachers)).Returns(built);
        coordinator
            .Setup(coord => coord.CreateAsync(TenantId, payload.CourseId, "ref-1", "UniqueClass", built.Id, built, teachers))
            .ReturnsAsync(new ClassCreationOutcome<UniqueClass>.Created(built));
        mapper.Setup(map => map.Map<GetUniqueClassDto>(built)).Returns(mapped);

        CreateUniqueClassResult result = await handler.Handle(new CreateUniqueClassCommand(payload));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf<CreateUniqueClassResult.Created>());
            Assert.That(((CreateUniqueClassResult.Created)result).UniqueClass, Is.SameAs(mapped));
        });
    }

    [Test]
    public async Task Handle_WhenCoordinatorReturnsReplayed_ReturnsReplayedFromIdempotencyWithMappedPrior()
    {
        var teacherId = Guid.NewGuid();
        CreateUniqueClassDto payload = ValidPayload(teacherId);
        List<ClassTeacher> teachers = TeacherEntities(teacherId);
        var built = new UniqueClass { Id = Guid.NewGuid(), TenantId = TenantId, GroupId = GroupId };
        var prior = new UniqueClass { Id = Guid.NewGuid(), TenantId = TenantId, GroupId = GroupId };
        var mappedPrior = new GetUniqueClassDto
        {
            Id = prior.Id,
            Date = payload.Date,
            MaxStudentLimit = payload.MaxStudentLimit,
            StartTime = payload.StartTime,
            EndTime = payload.EndTime,
            CourseId = payload.CourseId,
            GroupId = GroupId,
            GroupName = "Grupo",
            Teachers = []
        };

        mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(payload.Teachers)).Returns(teachers);
        classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, GroupId)).ReturnsAsync(true);
        uniqueClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, GroupId, payload.Date, payload.StartTime, payload.EndTime, null))
            .ReturnsAsync(false);
        classBuilder.Setup(builder => builder.BuildUniqueClass(TenantId, payload.CourseId, GroupId, payload, teachers)).Returns(built);
        coordinator
            .Setup(coord => coord.CreateAsync(TenantId, payload.CourseId, "ref-1", "UniqueClass", built.Id, built, teachers))
            .ReturnsAsync(new ClassCreationOutcome<UniqueClass>.Replayed(prior));
        mapper.Setup(map => map.Map<GetUniqueClassDto>(prior)).Returns(mappedPrior);

        CreateUniqueClassResult result = await handler.Handle(new CreateUniqueClassCommand(payload));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf<CreateUniqueClassResult.ReplayedFromIdempotency>());
            Assert.That(((CreateUniqueClassResult.ReplayedFromIdempotency)result).UniqueClass, Is.SameAs(mappedPrior));
        });
    }

    [Test]
    public async Task Handle_WhenCoordinatorReturnsCourseMissing_ReturnsCourseNotFound()
    {
        var teacherId = Guid.NewGuid();
        CreateUniqueClassDto payload = ValidPayload(teacherId);
        List<ClassTeacher> teachers = TeacherEntities(teacherId);
        var built = new UniqueClass { Id = Guid.NewGuid(), TenantId = TenantId, GroupId = GroupId };

        mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(payload.Teachers)).Returns(teachers);
        classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, GroupId)).ReturnsAsync(true);
        uniqueClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, GroupId, payload.Date, payload.StartTime, payload.EndTime, null))
            .ReturnsAsync(false);
        classBuilder.Setup(builder => builder.BuildUniqueClass(TenantId, payload.CourseId, GroupId, payload, teachers)).Returns(built);
        coordinator
            .Setup(coord => coord.CreateAsync(TenantId, payload.CourseId, "ref-1", "UniqueClass", built.Id, built, teachers))
            .ReturnsAsync(new ClassCreationOutcome<UniqueClass>.CourseMissing());

        CreateUniqueClassResult result = await handler.Handle(new CreateUniqueClassCommand(payload));

        Assert.That(result, Is.InstanceOf<CreateUniqueClassResult.CourseNotFound>());
    }
}
