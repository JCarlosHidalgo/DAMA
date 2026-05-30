using AutoMapper;

using Backend.Application.Infrastructure;
using Backend.Application.Scheduleds;
using Backend.Builders;
using Backend.Claims;
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

    private Mock<IScheduledClassDao> scheduledClassDao = null!;
    private Mock<IClassCreationCoordinator<ScheduledClass>> coordinator = null!;
    private Mock<IClaimContext> claimContext = null!;
    private Mock<IClassBuilder> classBuilder = null!;
    private Mock<IMapper> mapper = null!;
    private CreateScheduledClassHandler handler = null!;

    [SetUp]
    public void SetUp()
    {
        scheduledClassDao = new Mock<IScheduledClassDao>(MockBehavior.Strict);
        coordinator = new Mock<IClassCreationCoordinator<ScheduledClass>>(MockBehavior.Strict);
        claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        classBuilder = new Mock<IClassBuilder>(MockBehavior.Strict);
        mapper = new Mock<IMapper>(MockBehavior.Strict);
        claimContext.SetupGet(context => context.TenantId).Returns(TenantId);
        handler = new CreateScheduledClassHandler(
            scheduledClassDao.Object,
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
        Teachers =
        [
            new ClassTeacherDto { TeacherId = teacherId, TeacherName = "Profesor" }
        ],
        ExternalReference = "ref-1"
    };

    [Test]
    public async Task Handle_WhenAnyTeacherHasOverlap_ReturnsTeacherConflictAndSkipsCoordinator()
    {
        var teacherId = Guid.NewGuid();
        CreateScheduledClassDto payload = ValidPayload(teacherId);
        var teachers = new List<ClassTeacher>
        {
            new() { TeacherId = teacherId, TeacherName = "Profesor" }
        };

        mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(payload.Teachers)).Returns(teachers);
        scheduledClassDao
            .Setup(dao => dao.HasOverlapForTeacherAsync(TenantId, teacherId, payload.DayOfWeekIndex, payload.StartTime, payload.EndTime, null))
            .ReturnsAsync(true);

        CreateScheduledClassResult result = await handler.Handle(new CreateScheduledClassCommand(payload));

        Assert.That(result, Is.InstanceOf<CreateScheduledClassResult.TeacherConflict>());
        var conflict = (CreateScheduledClassResult.TeacherConflict)result;
        Assert.Multiple(() =>
        {
            Assert.That(conflict.TeacherId, Is.EqualTo(teacherId));
            Assert.That(conflict.TeacherName, Is.EqualTo("Profesor"));
        });
        coordinator.VerifyNoOtherCalls();
        classBuilder.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Handle_WhenCoordinatorReturnsCreated_ReturnsCreatedWithMappedDto()
    {
        var teacherId = Guid.NewGuid();
        CreateScheduledClassDto payload = ValidPayload(teacherId);
        var teachers = new List<ClassTeacher>
        {
            new() { TeacherId = teacherId, TeacherName = "Profesor" }
        };
        var built = new ScheduledClass { Id = Guid.NewGuid(), TenantId = TenantId };
        var mapped = new GetScheduledClassDto
        {
            Id = built.Id,
            DayOfWeekIndex = payload.DayOfWeekIndex,
            MaxStudentLimit = payload.MaxStudentLimit,
            StartTime = payload.StartTime,
            EndTime = payload.EndTime,
            CourseId = payload.CourseId,
            Teachers = []
        };

        mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(payload.Teachers)).Returns(teachers);
        scheduledClassDao
            .Setup(dao => dao.HasOverlapForTeacherAsync(TenantId, teacherId, payload.DayOfWeekIndex, payload.StartTime, payload.EndTime, null))
            .ReturnsAsync(false);
        classBuilder.Setup(builder => builder.BuildScheduledClass(TenantId, payload.CourseId, payload, teachers)).Returns(built);
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
        var teachers = new List<ClassTeacher>
        {
            new() { TeacherId = teacherId, TeacherName = "Profesor" }
        };
        var built = new ScheduledClass { Id = Guid.NewGuid(), TenantId = TenantId };
        var prior = new ScheduledClass { Id = Guid.NewGuid(), TenantId = TenantId };
        var mappedPrior = new GetScheduledClassDto
        {
            Id = prior.Id,
            DayOfWeekIndex = payload.DayOfWeekIndex,
            MaxStudentLimit = payload.MaxStudentLimit,
            StartTime = payload.StartTime,
            EndTime = payload.EndTime,
            CourseId = payload.CourseId,
            Teachers = []
        };

        mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(payload.Teachers)).Returns(teachers);
        scheduledClassDao
            .Setup(dao => dao.HasOverlapForTeacherAsync(TenantId, teacherId, payload.DayOfWeekIndex, payload.StartTime, payload.EndTime, null))
            .ReturnsAsync(false);
        classBuilder.Setup(builder => builder.BuildScheduledClass(TenantId, payload.CourseId, payload, teachers)).Returns(built);
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
        var teachers = new List<ClassTeacher>
        {
            new() { TeacherId = teacherId, TeacherName = "Profesor" }
        };
        var built = new ScheduledClass { Id = Guid.NewGuid(), TenantId = TenantId };

        mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(payload.Teachers)).Returns(teachers);
        scheduledClassDao
            .Setup(dao => dao.HasOverlapForTeacherAsync(TenantId, teacherId, payload.DayOfWeekIndex, payload.StartTime, payload.EndTime, null))
            .ReturnsAsync(false);
        classBuilder.Setup(builder => builder.BuildScheduledClass(TenantId, payload.CourseId, payload, teachers)).Returns(built);
        coordinator
            .Setup(coord => coord.CreateAsync(TenantId, payload.CourseId, "ref-1", "ScheduledClass", built.Id, built, teachers))
            .ReturnsAsync(new ClassCreationOutcome<ScheduledClass>.CourseMissing());

        CreateScheduledClassResult result = await handler.Handle(new CreateScheduledClassCommand(payload));

        Assert.That(result, Is.InstanceOf<CreateScheduledClassResult.CourseNotFound>());
    }

    [Test]
    public async Task Handle_WhenSecondTeacherOverlaps_ReturnsTeacherConflictWithSecondTeacherDetails()
    {
        var firstTeacherId = Guid.NewGuid();
        var secondTeacherId = Guid.NewGuid();
        var payload = new CreateScheduledClassDto
        {
            DayOfWeekIndex = 3,
            MaxStudentLimit = 0,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            CourseId = Guid.NewGuid(),
            Teachers =
            [
                new ClassTeacherDto { TeacherId = firstTeacherId, TeacherName = "Profesor A" },
                new ClassTeacherDto { TeacherId = secondTeacherId, TeacherName = "Profesor B" }
            ],
            ExternalReference = null
        };
        var teachers = new List<ClassTeacher>
        {
            new() { TeacherId = firstTeacherId, TeacherName = "Profesor A" },
            new() { TeacherId = secondTeacherId, TeacherName = "Profesor B" }
        };

        mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(payload.Teachers)).Returns(teachers);
        scheduledClassDao
            .Setup(dao => dao.HasOverlapForTeacherAsync(TenantId, firstTeacherId, payload.DayOfWeekIndex, payload.StartTime, payload.EndTime, null))
            .ReturnsAsync(false);
        scheduledClassDao
            .Setup(dao => dao.HasOverlapForTeacherAsync(TenantId, secondTeacherId, payload.DayOfWeekIndex, payload.StartTime, payload.EndTime, null))
            .ReturnsAsync(true);

        CreateScheduledClassResult result = await handler.Handle(new CreateScheduledClassCommand(payload));

        Assert.That(result, Is.InstanceOf<CreateScheduledClassResult.TeacherConflict>());
        var conflict = (CreateScheduledClassResult.TeacherConflict)result;
        Assert.Multiple(() =>
        {
            Assert.That(conflict.TeacherId, Is.EqualTo(secondTeacherId));
            Assert.That(conflict.TeacherName, Is.EqualTo("Profesor B"));
        });
    }
}
