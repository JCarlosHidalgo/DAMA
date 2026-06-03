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

    private Mock<IScheduledClassDao> _scheduledClassDao = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<ITransactionScope> _transactionScope = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private Mock<IMapper> _mapper = null!;
    private UpdateScheduledClassHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _scheduledClassDao = new Mock<IScheduledClassDao>(MockBehavior.Strict);
        _unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        _transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);
        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        _mapper = new Mock<IMapper>(MockBehavior.Strict);

        _transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);
        _unitOfWork.Setup(work => work.BeginAsync()).ReturnsAsync(_transactionScope.Object);
        _claimContext.SetupGet(context => context.TenantId).Returns(TenantId);

        _handler = new UpdateScheduledClassHandler(
            _scheduledClassDao.Object,
            _unitOfWork.Object,
            _claimContext.Object,
            _mapper.Object);
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

        _mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(teacherDtos)).Returns(teacherEntities);
        _scheduledClassDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, ScheduledClassId)).ReturnsAsync((ScheduledClass?)null);

        UpdateScheduledClassResult result = await _handler.Handle(new UpdateScheduledClassCommand(ScheduledClassId, payload));

        Assert.That(result, Is.InstanceOf<UpdateScheduledClassResult.NotFound>());
        _unitOfWork.Verify(work => work.BeginAsync(), Times.Never);
    }

    [Test]
    public async Task Handle_WhenGroupOverlap_ReturnsGroupOverlapConflictWithoutTransaction()
    {
        var teacherId = Guid.NewGuid();
        var teacherDtos = new List<ClassTeacherDto> { new() { TeacherId = teacherId, TeacherName = "Profesor" } };
        var teacherEntities = new List<ClassTeacher> { new() { TeacherId = teacherId, TeacherName = "Profesor" } };
        UpdateScheduledClassDto payload = Payload(teacherDtos);

        _mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(teacherDtos)).Returns(teacherEntities);
        _scheduledClassDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, ScheduledClassId)).ReturnsAsync(Existing());
        _scheduledClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, GroupId, payload.DayOfWeekIndex, payload.StartTime, payload.EndTime, ScheduledClassId))
            .ReturnsAsync(true);

        UpdateScheduledClassResult result = await _handler.Handle(new UpdateScheduledClassCommand(ScheduledClassId, payload));

        Assert.That(result, Is.InstanceOf<UpdateScheduledClassResult.GroupOverlapConflict>());
        _unitOfWork.Verify(work => work.BeginAsync(), Times.Never);
    }

    [Test]
    public async Task Handle_WhenUpdateReturnsFalse_ReturnsNotFoundAndDoesNotCommit()
    {
        var teacherId = Guid.NewGuid();
        var teacherDtos = new List<ClassTeacherDto> { new() { TeacherId = teacherId, TeacherName = "A" } };
        var teacherEntities = new List<ClassTeacher> { new() { TeacherId = teacherId, TeacherName = "A" } };
        UpdateScheduledClassDto payload = Payload(teacherDtos);

        _mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(teacherDtos)).Returns(teacherEntities);
        _scheduledClassDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, ScheduledClassId)).ReturnsAsync(Existing());
        _scheduledClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, GroupId, payload.DayOfWeekIndex, payload.StartTime, payload.EndTime, ScheduledClassId))
            .ReturnsAsync(false);
        _scheduledClassDao
            .Setup(dao => dao.UpdateForTenantAsync(It.Is<ScheduledClassUpdate>(update => update.Id == ScheduledClassId), TenantId, _transactionScope.Object))
            .ReturnsAsync(false);

        UpdateScheduledClassResult result = await _handler.Handle(new UpdateScheduledClassCommand(ScheduledClassId, payload));

        Assert.That(result, Is.InstanceOf<UpdateScheduledClassResult.NotFound>());
        _transactionScope.Verify(scope => scope.CommitAsync(), Times.Never);
        _scheduledClassDao.Verify(dao => dao.ReplaceTeachersAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ClassTeacher>>(), It.IsAny<Guid>(), It.IsAny<ITransactionContext>()), Times.Never);
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

        _mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(teacherDtos)).Returns(teacherEntities);
        _scheduledClassDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, ScheduledClassId)).ReturnsAsync(Existing());
        _scheduledClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, GroupId, payload.DayOfWeekIndex, payload.StartTime, payload.EndTime, ScheduledClassId))
            .ReturnsAsync(false);
        _scheduledClassDao
            .Setup(dao => dao.UpdateForTenantAsync(It.Is<ScheduledClassUpdate>(update => update.Id == ScheduledClassId), TenantId, _transactionScope.Object))
            .ReturnsAsync(true);
        _scheduledClassDao
            .Setup(dao => dao.ReplaceTeachersAsync(ScheduledClassId, teacherEntities, TenantId, _transactionScope.Object))
            .Returns(Task.CompletedTask);
        _transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        UpdateScheduledClassResult result = await _handler.Handle(new UpdateScheduledClassCommand(ScheduledClassId, payload));

        Assert.That(result, Is.InstanceOf<UpdateScheduledClassResult.Updated>());
        _transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
        _scheduledClassDao.Verify(dao => dao.ReplaceTeachersAsync(ScheduledClassId, teacherEntities, TenantId, _transactionScope.Object), Times.Once);
    }
}
