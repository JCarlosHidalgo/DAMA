using AutoMapper;

using Backend.Application.Uniques;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Uniques;
using Backend.Dtos;
using Backend.Dtos.Uniques.Input;
using Backend.Entities;
using Backend.Entities.Uniques;
using Backend.Results.Uniques;

using DAMA.Software.MySqlUnitOfWork;

using Moq;

namespace Test.Application.Uniques;

[TestFixture]
public class UpdateUniqueClassHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UniqueClassId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid GroupId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private Mock<IUniqueClassDao> _uniqueClassDao = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<ITransactionScope> _transactionScope = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private Mock<IMapper> _mapper = null!;
    private UpdateUniqueClassHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _uniqueClassDao = new Mock<IUniqueClassDao>(MockBehavior.Strict);
        _unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        _transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);
        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        _mapper = new Mock<IMapper>(MockBehavior.Strict);

        _transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);
        _unitOfWork.Setup(work => work.BeginAsync()).ReturnsAsync(_transactionScope.Object);
        _claimContext.SetupGet(context => context.TenantId).Returns(TenantId);

        _handler = new UpdateUniqueClassHandler(
            _uniqueClassDao.Object,
            _unitOfWork.Object,
            _claimContext.Object,
            _mapper.Object);
    }

    private static UpdateUniqueClassDto Payload(List<ClassTeacherDto> teachers) => new()
    {
        Date = new DateOnly(2025, 6, 1),
        StartTime = new TimeOnly(9, 0),
        EndTime = new TimeOnly(10, 0),
        MaxStudentLimit = 0,
        Teachers = teachers
    };

    private static UniqueClass Existing() => new()
    {
        Id = UniqueClassId,
        TenantId = TenantId,
        GroupId = GroupId
    };

    [Test]
    public async Task Handle_WhenClassNotFound_ReturnsNotFoundWithoutTransaction()
    {
        var teacherId = Guid.NewGuid();
        var teacherDtos = new List<ClassTeacherDto> { new() { TeacherId = teacherId, TeacherName = "Profesor" } };
        var teacherEntities = new List<ClassTeacher> { new() { TeacherId = teacherId, TeacherName = "Profesor" } };
        UpdateUniqueClassDto payload = Payload(teacherDtos);

        _mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(teacherDtos)).Returns(teacherEntities);
        _uniqueClassDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, UniqueClassId)).ReturnsAsync((UniqueClass?)null);

        UpdateUniqueClassResult result = await _handler.Handle(new UpdateUniqueClassCommand(UniqueClassId, payload));

        Assert.That(result, Is.InstanceOf<UpdateUniqueClassResult.NotFound>());
        _unitOfWork.Verify(work => work.BeginAsync(), Times.Never);
    }

    [Test]
    public async Task Handle_WhenGroupOverlap_ReturnsGroupOverlapConflictWithoutBeginningTransaction()
    {
        var conflictTeacherId = Guid.NewGuid();
        var teacherDtos = new List<ClassTeacherDto> { new() { TeacherId = conflictTeacherId, TeacherName = "Profesor" } };
        var teacherEntities = new List<ClassTeacher> { new() { TeacherId = conflictTeacherId, TeacherName = "Profesor" } };
        UpdateUniqueClassDto payload = Payload(teacherDtos);

        _mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(teacherDtos)).Returns(teacherEntities);
        _uniqueClassDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, UniqueClassId)).ReturnsAsync(Existing());
        _uniqueClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, GroupId, payload.Date, payload.StartTime, payload.EndTime, UniqueClassId))
            .ReturnsAsync(true);

        UpdateUniqueClassResult result = await _handler.Handle(new UpdateUniqueClassCommand(UniqueClassId, payload));

        Assert.That(result, Is.InstanceOf<UpdateUniqueClassResult.GroupOverlapConflict>());
        _unitOfWork.Verify(work => work.BeginAsync(), Times.Never);
    }

    [Test]
    public async Task Handle_WhenUpdateReturnsFalse_ReturnsNotFoundAndDoesNotCommit()
    {
        var teacherId = Guid.NewGuid();
        var teacherDtos = new List<ClassTeacherDto> { new() { TeacherId = teacherId, TeacherName = "A" } };
        var teacherEntities = new List<ClassTeacher> { new() { TeacherId = teacherId, TeacherName = "A" } };
        UpdateUniqueClassDto payload = Payload(teacherDtos);

        _mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(teacherDtos)).Returns(teacherEntities);
        _uniqueClassDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, UniqueClassId)).ReturnsAsync(Existing());
        _uniqueClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, GroupId, payload.Date, payload.StartTime, payload.EndTime, UniqueClassId))
            .ReturnsAsync(false);
        _uniqueClassDao
            .Setup(dao => dao.UpdateForTenantAsync(It.Is<UniqueClassUpdate>(update => update.Id == UniqueClassId), TenantId, _transactionScope.Object))
            .ReturnsAsync(false);

        UpdateUniqueClassResult result = await _handler.Handle(new UpdateUniqueClassCommand(UniqueClassId, payload));

        Assert.That(result, Is.InstanceOf<UpdateUniqueClassResult.NotFound>());
        _transactionScope.Verify(scope => scope.CommitAsync(), Times.Never);
        _uniqueClassDao.Verify(dao => dao.ReplaceTeachersAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ClassTeacher>>(), It.IsAny<Guid>(), It.IsAny<ITransactionContext>()), Times.Never);
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
        UpdateUniqueClassDto payload = Payload(teacherDtos);

        _mapper.Setup(map => map.Map<List<ClassTeacherDto>, List<ClassTeacher>>(teacherDtos)).Returns(teacherEntities);
        _uniqueClassDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, UniqueClassId)).ReturnsAsync(Existing());
        _uniqueClassDao
            .Setup(dao => dao.HasGroupOverlapAsync(TenantId, GroupId, payload.Date, payload.StartTime, payload.EndTime, UniqueClassId))
            .ReturnsAsync(false);
        _uniqueClassDao
            .Setup(dao => dao.UpdateForTenantAsync(It.Is<UniqueClassUpdate>(update => update.Id == UniqueClassId), TenantId, _transactionScope.Object))
            .ReturnsAsync(true);
        _uniqueClassDao
            .Setup(dao => dao.ReplaceTeachersAsync(UniqueClassId, teacherEntities, TenantId, _transactionScope.Object))
            .Returns(Task.CompletedTask);
        _transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        UpdateUniqueClassResult result = await _handler.Handle(new UpdateUniqueClassCommand(UniqueClassId, payload));

        Assert.That(result, Is.InstanceOf<UpdateUniqueClassResult.Updated>());
        _transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
        _uniqueClassDao.Verify(dao => dao.ReplaceTeachersAsync(UniqueClassId, teacherEntities, TenantId, _transactionScope.Object), Times.Once);
    }
}
