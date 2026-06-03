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

    private Mock<IUniqueClassDao> _uniqueClassDao = null!;
    private Mock<IClassGroupDao> _classGroupDao = null!;
    private Mock<IClassCreationCoordinator<UniqueClass>> _coordinator = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private Mock<IClassBuilder> _classBuilder = null!;
    private Mock<IMapper> _mapper = null!;
    private CreateUniqueClassHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _uniqueClassDao = new Mock<IUniqueClassDao>(MockBehavior.Strict);
        _classGroupDao = new Mock<IClassGroupDao>(MockBehavior.Strict);
        _coordinator = new Mock<IClassCreationCoordinator<UniqueClass>>(MockBehavior.Strict);
        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        _classBuilder = new Mock<IClassBuilder>(MockBehavior.Strict);
        _mapper = new Mock<IMapper>(MockBehavior.Strict);
        _claimContext.SetupGet(context => context.TenantId).Returns(TenantId);
        _handler = new CreateUniqueClassHandler(
            _uniqueClassDao.Object,
            _classGroupDao.Object,
            _coordinator.Object,
            _claimContext.Object,
            _classBuilder.Object,
            _mapper.Object);
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

        _mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(payload.Teachers)).Returns(teachers);
        _classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, GroupId)).ReturnsAsync(false);

        CreateUniqueClassResult result = await _handler.Handle(new CreateUniqueClassCommand(payload));

        Assert.That(result, Is.InstanceOf<CreateUniqueClassResult.GroupNotFound>());
        _uniqueClassDao.VerifyNoOtherCalls();
        _coordinator.VerifyNoOtherCalls();
        _classBuilder.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Handle_WhenGroupOverlap_ReturnsGroupOverlapConflictAndSkipsCoordinator()
    {
        var teacherId = Guid.NewGuid();
        CreateUniqueClassDto payload = ValidPayload(teacherId);
        List<ClassTeacher> teachers = TeacherEntities(teacherId);

        _mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(payload.Teachers)).Returns(teachers);
        _classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, GroupId)).ReturnsAsync(true);
        _uniqueClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, GroupId, payload.Date, payload.StartTime, payload.EndTime, null))
            .ReturnsAsync(true);

        CreateUniqueClassResult result = await _handler.Handle(new CreateUniqueClassCommand(payload));

        Assert.That(result, Is.InstanceOf<CreateUniqueClassResult.GroupOverlapConflict>());
        _coordinator.VerifyNoOtherCalls();
        _classBuilder.VerifyNoOtherCalls();
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

        _mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(payload.Teachers)).Returns(teachers);
        _classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, GroupId)).ReturnsAsync(true);
        _uniqueClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, GroupId, payload.Date, payload.StartTime, payload.EndTime, null))
            .ReturnsAsync(false);
        _classBuilder.Setup(builder => builder.BuildUniqueClass(TenantId, payload.CourseId, GroupId, payload, teachers)).Returns(built);
        _coordinator
            .Setup(coord => coord.CreateAsync(TenantId, payload.CourseId, "ref-1", "UniqueClass", built.Id, built, teachers))
            .ReturnsAsync(new ClassCreationOutcome<UniqueClass>.Created(built));
        _mapper.Setup(map => map.Map<GetUniqueClassDto>(built)).Returns(mapped);

        CreateUniqueClassResult result = await _handler.Handle(new CreateUniqueClassCommand(payload));

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

        _mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(payload.Teachers)).Returns(teachers);
        _classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, GroupId)).ReturnsAsync(true);
        _uniqueClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, GroupId, payload.Date, payload.StartTime, payload.EndTime, null))
            .ReturnsAsync(false);
        _classBuilder.Setup(builder => builder.BuildUniqueClass(TenantId, payload.CourseId, GroupId, payload, teachers)).Returns(built);
        _coordinator
            .Setup(coord => coord.CreateAsync(TenantId, payload.CourseId, "ref-1", "UniqueClass", built.Id, built, teachers))
            .ReturnsAsync(new ClassCreationOutcome<UniqueClass>.Replayed(prior));
        _mapper.Setup(map => map.Map<GetUniqueClassDto>(prior)).Returns(mappedPrior);

        CreateUniqueClassResult result = await _handler.Handle(new CreateUniqueClassCommand(payload));

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

        _mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(payload.Teachers)).Returns(teachers);
        _classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, GroupId)).ReturnsAsync(true);
        _uniqueClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, GroupId, payload.Date, payload.StartTime, payload.EndTime, null))
            .ReturnsAsync(false);
        _classBuilder.Setup(builder => builder.BuildUniqueClass(TenantId, payload.CourseId, GroupId, payload, teachers)).Returns(built);
        _coordinator
            .Setup(coord => coord.CreateAsync(TenantId, payload.CourseId, "ref-1", "UniqueClass", built.Id, built, teachers))
            .ReturnsAsync(new ClassCreationOutcome<UniqueClass>.CourseMissing());

        CreateUniqueClassResult result = await _handler.Handle(new CreateUniqueClassCommand(payload));

        Assert.That(result, Is.InstanceOf<CreateUniqueClassResult.CourseNotFound>());
    }
}
