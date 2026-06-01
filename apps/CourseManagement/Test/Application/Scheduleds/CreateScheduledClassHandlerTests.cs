using AutoMapper;

using Backend.Application.Infrastructure;
using Backend.Application.Scheduleds;
using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Groups;
using Backend.DB.Daos.Abstract.Single.Scheduleds;
using Backend.Dtos;
using Backend.Dtos.Scheduleds.Input;
using Backend.Dtos.Scheduleds.Output;
using Backend.Entities;
using Backend.Entities.Scheduleds;
using Backend.Results.Scheduleds;

using Moq;

namespace Test.Application.Scheduleds;

[TestFixture]
public class CreateScheduledClassHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid GroupId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private Mock<IScheduledClassDao> scheduledClassDao = null!;
    private Mock<IClassGroupDao> classGroupDao = null!;
    private Mock<IClassCreationCoordinator<ScheduledClass>> coordinator = null!;
    private Mock<IClaimContext> claimContext = null!;
    private Mock<IClassBuilder> classBuilder = null!;
    private Mock<IMapper> mapper = null!;
    private CreateScheduledClassHandler handler = null!;

    [SetUp]
    public void SetUp()
    {
        scheduledClassDao = new Mock<IScheduledClassDao>(MockBehavior.Strict);
        classGroupDao = new Mock<IClassGroupDao>(MockBehavior.Strict);
        coordinator = new Mock<IClassCreationCoordinator<ScheduledClass>>(MockBehavior.Strict);
        claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        classBuilder = new Mock<IClassBuilder>(MockBehavior.Strict);
        mapper = new Mock<IMapper>(MockBehavior.Strict);
        claimContext.SetupGet(context => context.TenantId).Returns(TenantId);
        handler = new CreateScheduledClassHandler(
            scheduledClassDao.Object,
            classGroupDao.Object,
            coordinator.Object,
            claimContext.Object,
            classBuilder.Object,
            mapper.Object);
    }

    private static CreateScheduledClassDto ValidPayload(Guid teacherId) => new()
    {
        DayOfWeekIndex = 2,
        MaxStudentLimit = 0,
        StartTime = new TimeOnly(9, 0),
        EndTime = new TimeOnly(10, 0),
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
    public async Task Handle_WhenGroupDoesNotExist_ReturnsGroupNotFoundAndSkipsOverlapAndCoordinator()
    {
        var teacherId = Guid.NewGuid();
        CreateScheduledClassDto payload = ValidPayload(teacherId);
        List<ClassTeacher> teachers = TeacherEntities(teacherId);

        mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(payload.Teachers)).Returns(teachers);
        classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, GroupId)).ReturnsAsync(false);

        CreateScheduledClassResult result = await handler.Handle(new CreateScheduledClassCommand(payload));

        Assert.That(result, Is.InstanceOf<CreateScheduledClassResult.GroupNotFound>());
        scheduledClassDao.VerifyNoOtherCalls();
        coordinator.VerifyNoOtherCalls();
        classBuilder.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Handle_WhenGroupOverlap_ReturnsGroupOverlapConflictAndSkipsCoordinator()
    {
        var teacherId = Guid.NewGuid();
        CreateScheduledClassDto payload = ValidPayload(teacherId);
        List<ClassTeacher> teachers = TeacherEntities(teacherId);

        mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(payload.Teachers)).Returns(teachers);
        classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, GroupId)).ReturnsAsync(true);
        scheduledClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, GroupId, payload.DayOfWeekIndex, payload.StartTime, payload.EndTime, null))
            .ReturnsAsync(true);

        CreateScheduledClassResult result = await handler.Handle(new CreateScheduledClassCommand(payload));

        Assert.That(result, Is.InstanceOf<CreateScheduledClassResult.GroupOverlapConflict>());
        coordinator.VerifyNoOtherCalls();
        classBuilder.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Handle_WhenCoordinatorReturnsCreated_ReturnsCreatedWithMappedDto()
    {
        var teacherId = Guid.NewGuid();
        CreateScheduledClassDto payload = ValidPayload(teacherId);
        List<ClassTeacher> teachers = TeacherEntities(teacherId);
        var built = new ScheduledClass { Id = Guid.NewGuid(), TenantId = TenantId, GroupId = GroupId };
        var mapped = new GetScheduledClassDto
        {
            Id = built.Id,
            DayOfWeekIndex = payload.DayOfWeekIndex,
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
        scheduledClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, GroupId, payload.DayOfWeekIndex, payload.StartTime, payload.EndTime, null))
            .ReturnsAsync(false);
        classBuilder.Setup(builder => builder.BuildScheduledClass(TenantId, payload.CourseId, GroupId, payload, teachers)).Returns(built);
        coordinator
            .Setup(coord => coord.CreateAsync(TenantId, payload.CourseId, "ref-1", "ScheduledClass", built.Id, built, teachers))
            .ReturnsAsync(new ClassCreationOutcome<ScheduledClass>.Created(built));
        mapper.Setup(map => map.Map<GetScheduledClassDto>(built)).Returns(mapped);

        CreateScheduledClassResult result = await handler.Handle(new CreateScheduledClassCommand(payload));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf<CreateScheduledClassResult.Created>());
            Assert.That(((CreateScheduledClassResult.Created)result).ScheduledClass, Is.SameAs(mapped));
        });
    }

    [Test]
    public async Task Handle_WhenCoordinatorReturnsReplayed_ReturnsReplayedFromIdempotencyWithMappedPrior()
    {
        var teacherId = Guid.NewGuid();
        CreateScheduledClassDto payload = ValidPayload(teacherId);
        List<ClassTeacher> teachers = TeacherEntities(teacherId);
        var built = new ScheduledClass { Id = Guid.NewGuid(), TenantId = TenantId, GroupId = GroupId };
        var prior = new ScheduledClass { Id = Guid.NewGuid(), TenantId = TenantId, GroupId = GroupId };
        var mappedPrior = new GetScheduledClassDto
        {
            Id = prior.Id,
            DayOfWeekIndex = payload.DayOfWeekIndex,
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
        scheduledClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, GroupId, payload.DayOfWeekIndex, payload.StartTime, payload.EndTime, null))
            .ReturnsAsync(false);
        classBuilder.Setup(builder => builder.BuildScheduledClass(TenantId, payload.CourseId, GroupId, payload, teachers)).Returns(built);
        coordinator
            .Setup(coord => coord.CreateAsync(TenantId, payload.CourseId, "ref-1", "ScheduledClass", built.Id, built, teachers))
            .ReturnsAsync(new ClassCreationOutcome<ScheduledClass>.Replayed(prior));
        mapper.Setup(map => map.Map<GetScheduledClassDto>(prior)).Returns(mappedPrior);

        CreateScheduledClassResult result = await handler.Handle(new CreateScheduledClassCommand(payload));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf<CreateScheduledClassResult.ReplayedFromIdempotency>());
            Assert.That(((CreateScheduledClassResult.ReplayedFromIdempotency)result).ScheduledClass, Is.SameAs(mappedPrior));
        });
    }

    [Test]
    public async Task Handle_WhenCoordinatorReturnsCourseMissing_ReturnsCourseNotFound()
    {
        var teacherId = Guid.NewGuid();
        CreateScheduledClassDto payload = ValidPayload(teacherId);
        List<ClassTeacher> teachers = TeacherEntities(teacherId);
        var built = new ScheduledClass { Id = Guid.NewGuid(), TenantId = TenantId, GroupId = GroupId };

        mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(payload.Teachers)).Returns(teachers);
        classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, GroupId)).ReturnsAsync(true);
        scheduledClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, GroupId, payload.DayOfWeekIndex, payload.StartTime, payload.EndTime, null))
            .ReturnsAsync(false);
        classBuilder.Setup(builder => builder.BuildScheduledClass(TenantId, payload.CourseId, GroupId, payload, teachers)).Returns(built);
        coordinator
            .Setup(coord => coord.CreateAsync(TenantId, payload.CourseId, "ref-1", "ScheduledClass", built.Id, built, teachers))
            .ReturnsAsync(new ClassCreationOutcome<ScheduledClass>.CourseMissing());

        CreateScheduledClassResult result = await handler.Handle(new CreateScheduledClassCommand(payload));

        Assert.That(result, Is.InstanceOf<CreateScheduledClassResult.CourseNotFound>());
    }
}
