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

    private Mock<IScheduledClassDao> _scheduledClassDao = null!;
    private Mock<IClassGroupDao> _classGroupDao = null!;
    private Mock<IClassCreationCoordinator<ScheduledClass>> _coordinator = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private Mock<IClassBuilder> _classBuilder = null!;
    private Mock<IMapper> _mapper = null!;
    private CreateScheduledClassHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _scheduledClassDao = new Mock<IScheduledClassDao>(MockBehavior.Strict);
        _classGroupDao = new Mock<IClassGroupDao>(MockBehavior.Strict);
        _coordinator = new Mock<IClassCreationCoordinator<ScheduledClass>>(MockBehavior.Strict);
        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        _classBuilder = new Mock<IClassBuilder>(MockBehavior.Strict);
        _mapper = new Mock<IMapper>(MockBehavior.Strict);
        _claimContext.SetupGet(context => context.TenantId).Returns(TenantId);
        _handler = new CreateScheduledClassHandler(
            _scheduledClassDao.Object,
            _classGroupDao.Object,
            _coordinator.Object,
            _claimContext.Object,
            _classBuilder.Object,
            _mapper.Object);
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

        _mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(payload.Teachers)).Returns(teachers);
        _classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, GroupId)).ReturnsAsync(false);

        CreateScheduledClassResult result = await _handler.Handle(new CreateScheduledClassCommand(payload));

        Assert.That(result, Is.InstanceOf<CreateScheduledClassResult.GroupNotFound>());
        _scheduledClassDao.VerifyNoOtherCalls();
        _coordinator.VerifyNoOtherCalls();
        _classBuilder.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Handle_WhenGroupOverlap_ReturnsGroupOverlapConflictAndSkipsCoordinator()
    {
        var teacherId = Guid.NewGuid();
        CreateScheduledClassDto payload = ValidPayload(teacherId);
        List<ClassTeacher> teachers = TeacherEntities(teacherId);

        _mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(payload.Teachers)).Returns(teachers);
        _classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, GroupId)).ReturnsAsync(true);
        _scheduledClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, GroupId, payload.DayOfWeekIndex, payload.StartTime, payload.EndTime, null))
            .ReturnsAsync(true);

        CreateScheduledClassResult result = await _handler.Handle(new CreateScheduledClassCommand(payload));

        Assert.That(result, Is.InstanceOf<CreateScheduledClassResult.GroupOverlapConflict>());
        _coordinator.VerifyNoOtherCalls();
        _classBuilder.VerifyNoOtherCalls();
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

        _mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(payload.Teachers)).Returns(teachers);
        _classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, GroupId)).ReturnsAsync(true);
        _scheduledClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, GroupId, payload.DayOfWeekIndex, payload.StartTime, payload.EndTime, null))
            .ReturnsAsync(false);
        _classBuilder.Setup(builder => builder.BuildScheduledClass(TenantId, payload.CourseId, GroupId, payload, teachers)).Returns(built);
        _coordinator
            .Setup(coord => coord.CreateAsync(TenantId, payload.CourseId, "ref-1", "ScheduledClass", built.Id, built, teachers))
            .ReturnsAsync(new ClassCreationOutcome<ScheduledClass>.Created(built));
        _mapper.Setup(map => map.Map<GetScheduledClassDto>(built)).Returns(mapped);

        CreateScheduledClassResult result = await _handler.Handle(new CreateScheduledClassCommand(payload));

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

        _mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(payload.Teachers)).Returns(teachers);
        _classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, GroupId)).ReturnsAsync(true);
        _scheduledClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, GroupId, payload.DayOfWeekIndex, payload.StartTime, payload.EndTime, null))
            .ReturnsAsync(false);
        _classBuilder.Setup(builder => builder.BuildScheduledClass(TenantId, payload.CourseId, GroupId, payload, teachers)).Returns(built);
        _coordinator
            .Setup(coord => coord.CreateAsync(TenantId, payload.CourseId, "ref-1", "ScheduledClass", built.Id, built, teachers))
            .ReturnsAsync(new ClassCreationOutcome<ScheduledClass>.Replayed(prior));
        _mapper.Setup(map => map.Map<GetScheduledClassDto>(prior)).Returns(mappedPrior);

        CreateScheduledClassResult result = await _handler.Handle(new CreateScheduledClassCommand(payload));

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

        _mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(payload.Teachers)).Returns(teachers);
        _classGroupDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, GroupId)).ReturnsAsync(true);
        _scheduledClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, GroupId, payload.DayOfWeekIndex, payload.StartTime, payload.EndTime, null))
            .ReturnsAsync(false);
        _classBuilder.Setup(builder => builder.BuildScheduledClass(TenantId, payload.CourseId, GroupId, payload, teachers)).Returns(built);
        _coordinator
            .Setup(coord => coord.CreateAsync(TenantId, payload.CourseId, "ref-1", "ScheduledClass", built.Id, built, teachers))
            .ReturnsAsync(new ClassCreationOutcome<ScheduledClass>.CourseMissing());

        CreateScheduledClassResult result = await _handler.Handle(new CreateScheduledClassCommand(payload));

        Assert.That(result, Is.InstanceOf<CreateScheduledClassResult.CourseNotFound>());
    }
}
