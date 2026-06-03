using Backend.Application.Schedules;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Scheduleds;
using Backend.DB.Daos.Abstract.Single.Uniques;
using Backend.Dtos.Schedules.Output;
using Backend.Entities.Scheduleds;
using Backend.Entities.Uniques;
using Backend.Results.Schedules;

using Moq;

namespace Test.Application.Schedules;

[TestFixture]
public class GetTeacherScheduleHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TeacherId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly DateOnly ClassDatePointer = new(2025, 6, 1);

    private Mock<IScheduledClassDao> _scheduledClassDao = null!;
    private Mock<IUniqueClassDao> _uniqueClassDao = null!;
    private Mock<IScheduleAssembler> _scheduleAssembler = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private GetTeacherScheduleHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _scheduledClassDao = new Mock<IScheduledClassDao>(MockBehavior.Strict);
        _uniqueClassDao = new Mock<IUniqueClassDao>(MockBehavior.Strict);
        _scheduleAssembler = new Mock<IScheduleAssembler>(MockBehavior.Strict);
        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        _claimContext.SetupGet(context => context.TenantId).Returns(TenantId);
        _claimContext.SetupGet(context => context.UserId).Returns(TeacherId);
        _handler = new GetTeacherScheduleHandler(_scheduledClassDao.Object, _uniqueClassDao.Object, _scheduleAssembler.Object, _claimContext.Object);
    }

    [Test]
    public async Task Handle_DelegatesToAssemblerWithTeacherScopedLoaders_ReturnsFound()
    {
        var scheduledClasses = new List<ScheduledClass>();
        var uniqueClasses = new List<UniqueClass>();
        var schedule = new GetCourseScheduleDto
        {
            ScheduledClasses = [],
            UniqueClasses = []
        };

        _scheduledClassDao.Setup(dao => dao.GetByTeacherForTenantAsync(TenantId, TeacherId)).ReturnsAsync(scheduledClasses);
        _uniqueClassDao.Setup(dao => dao.GetByTeacherOnWeekForTenantAsync(TenantId, TeacherId, ClassDatePointer)).ReturnsAsync(uniqueClasses);
        _scheduleAssembler
            .Setup(assembler => assembler.AssembleAsync(
                It.IsAny<Func<Task<List<ScheduledClass>>>>(),
                It.IsAny<Func<Task<List<UniqueClass>>>>()))
            .Returns(async (Func<Task<List<ScheduledClass>>> loadScheduled, Func<Task<List<UniqueClass>>> loadUnique) =>
            {
                List<ScheduledClass> resultScheduled = await loadScheduled();
                List<UniqueClass> resultUnique = await loadUnique();
                Assert.Multiple(() =>
                {
                    Assert.That(resultScheduled, Is.SameAs(scheduledClasses));
                    Assert.That(resultUnique, Is.SameAs(uniqueClasses));
                });
                return schedule;
            });

        GetTeacherScheduleResult result = await _handler.Handle(new GetTeacherScheduleQuery(ClassDatePointer));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf<GetTeacherScheduleResult.Found>());
            Assert.That(((GetTeacherScheduleResult.Found)result).Schedule, Is.SameAs(schedule));
        });
    }
}
