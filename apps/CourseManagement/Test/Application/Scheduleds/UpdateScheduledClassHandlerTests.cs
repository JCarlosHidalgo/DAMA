using AutoMapper;

using Backend.Application.Scheduleds;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Scheduleds;
using Backend.Dtos;
using Backend.Dtos.Scheduleds.Input;
using Backend.Entities;
using Backend.Entities.Scheduleds;
using Backend.Results.Scheduleds;

using DAMA.Software.MySqlUnitOfWork;

using Moq;

namespace Test.Application.Scheduleds;

[TestFixture]
public class UpdateScheduledClassHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ScheduledClassId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid GroupId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private Mock<IScheduledClassDao> scheduledClassDao = null!;
    private Mock<IUnitOfWork> unitOfWork = null!;
    private Mock<ITransactionScope> transactionScope = null!;
    private Mock<IClaimContext> claimContext = null!;
    private Mock<IMapper> mapper = null!;
    private UpdateScheduledClassHandler handler = null!;

    [SetUp]
    public void SetUp()
    {
        scheduledClassDao = new Mock<IScheduledClassDao>(MockBehavior.Strict);
        unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);
        claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        mapper = new Mock<IMapper>(MockBehavior.Strict);

        transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);
        unitOfWork.Setup(work => work.BeginAsync()).ReturnsAsync(transactionScope.Object);
        claimContext.SetupGet(context => context.TenantId).Returns(TenantId);

        handler = new UpdateScheduledClassHandler(
            scheduledClassDao.Object,
            unitOfWork.Object,
            claimContext.Object,
            mapper.Object);
    }

    private static UpdateScheduledClassDto Payload(List<ClassTeacherDto> teachers) => new()
    {
        DayOfWeekIndex = 3,
        StartTime = new TimeOnly(9, 0),
        EndTime = new TimeOnly(10, 0),
        MaxStudentLimit = 0,
        Teachers = teachers
    };

    private static ScheduledClass Existing() => new()
    {
        Id = ScheduledClassId,
        TenantId = TenantId,
        GroupId = GroupId
    };

    [Test]
    public async Task Handle_WhenClassNotFound_ReturnsNotFoundWithoutTransaction()
    {
        var teacherId = Guid.NewGuid();
        var teacherDtos = new List<ClassTeacherDto> { new() { TeacherId = teacherId, TeacherName = "Profesor" } };
        var teacherEntities = new List<ClassTeacher> { new() { TeacherId = teacherId, TeacherName = "Profesor" } };
        UpdateScheduledClassDto payload = Payload(teacherDtos);

        mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(teacherDtos)).Returns(teacherEntities);
        scheduledClassDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, ScheduledClassId)).ReturnsAsync((ScheduledClass?)null);

        UpdateScheduledClassResult result = await handler.Handle(new UpdateScheduledClassCommand(ScheduledClassId, payload));

        Assert.That(result, Is.InstanceOf<UpdateScheduledClassResult.NotFound>());
        unitOfWork.Verify(work => work.BeginAsync(), Times.Never);
    }

    [Test]
    public async Task Handle_WhenGroupOverlap_ReturnsGroupOverlapConflictWithoutTransaction()
    {
        var teacherId = Guid.NewGuid();
        var teacherDtos = new List<ClassTeacherDto> { new() { TeacherId = teacherId, TeacherName = "Profesor" } };
        var teacherEntities = new List<ClassTeacher> { new() { TeacherId = teacherId, TeacherName = "Profesor" } };
        UpdateScheduledClassDto payload = Payload(teacherDtos);

        mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(teacherDtos)).Returns(teacherEntities);
        scheduledClassDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, ScheduledClassId)).ReturnsAsync(Existing());
        scheduledClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, GroupId, payload.DayOfWeekIndex, payload.StartTime, payload.EndTime, ScheduledClassId))
            .ReturnsAsync(true);

        UpdateScheduledClassResult result = await handler.Handle(new UpdateScheduledClassCommand(ScheduledClassId, payload));

        Assert.That(result, Is.InstanceOf<UpdateScheduledClassResult.GroupOverlapConflict>());
        unitOfWork.Verify(work => work.BeginAsync(), Times.Never);
    }

    [Test]
    public async Task Handle_WhenUpdateReturnsFalse_ReturnsNotFoundAndDoesNotCommit()
    {
        var teacherId = Guid.NewGuid();
        var teacherDtos = new List<ClassTeacherDto> { new() { TeacherId = teacherId, TeacherName = "A" } };
        var teacherEntities = new List<ClassTeacher> { new() { TeacherId = teacherId, TeacherName = "A" } };
        UpdateScheduledClassDto payload = Payload(teacherDtos);

        mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(teacherDtos)).Returns(teacherEntities);
        scheduledClassDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, ScheduledClassId)).ReturnsAsync(Existing());
        scheduledClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, GroupId, payload.DayOfWeekIndex, payload.StartTime, payload.EndTime, ScheduledClassId))
            .ReturnsAsync(false);
        scheduledClassDao
            .Setup(dao => dao.UpdateForTenantAsync(It.Is<ScheduledClassUpdate>(update => update.Id == ScheduledClassId), TenantId, transactionScope.Object))
            .ReturnsAsync(false);

        UpdateScheduledClassResult result = await handler.Handle(new UpdateScheduledClassCommand(ScheduledClassId, payload));

        Assert.That(result, Is.InstanceOf<UpdateScheduledClassResult.NotFound>());
        transactionScope.Verify(scope => scope.CommitAsync(), Times.Never);
        scheduledClassDao.Verify(dao => dao.ReplaceTeachersAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ClassTeacher>>(), It.IsAny<Guid>(), It.IsAny<ITransactionContext>()), Times.Never);
    }

    [Test]
    public async Task Handle_WhenUpdateSucceeds_ReplacesTeachersCommitsAndReturnsUpdated()
    {
        var teacherIdA = Guid.NewGuid();
        var teacherIdB = Guid.NewGuid();
        var teacherDtos = new List<ClassTeacherDto>
        {
            new() { TeacherId = teacherIdA, TeacherName = "A" },
            new() { TeacherId = teacherIdB, TeacherName = "B" }
        };
        var teacherEntities = new List<ClassTeacher>
        {
            new() { TeacherId = teacherIdA, TeacherName = "A" },
            new() { TeacherId = teacherIdB, TeacherName = "B" }
        };
        UpdateScheduledClassDto payload = Payload(teacherDtos);

        mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(teacherDtos)).Returns(teacherEntities);
        scheduledClassDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, ScheduledClassId)).ReturnsAsync(Existing());
        scheduledClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, GroupId, payload.DayOfWeekIndex, payload.StartTime, payload.EndTime, ScheduledClassId))
            .ReturnsAsync(false);
        scheduledClassDao
            .Setup(dao => dao.UpdateForTenantAsync(It.Is<ScheduledClassUpdate>(update => update.Id == ScheduledClassId), TenantId, transactionScope.Object))
            .ReturnsAsync(true);
        scheduledClassDao
            .Setup(dao => dao.ReplaceTeachersAsync(ScheduledClassId, teacherEntities, TenantId, transactionScope.Object))
            .Returns(Task.CompletedTask);
        transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        UpdateScheduledClassResult result = await handler.Handle(new UpdateScheduledClassCommand(ScheduledClassId, payload));

        Assert.That(result, Is.InstanceOf<UpdateScheduledClassResult.Updated>());
        transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
        scheduledClassDao.Verify(dao => dao.ReplaceTeachersAsync(ScheduledClassId, teacherEntities, TenantId, transactionScope.Object), Times.Once);
    }
}
