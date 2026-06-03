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
public class GetTenantScheduleHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateOnly ClassDatePointer = new(2025, 6, 1);

    private Mock<IScheduledClassDao> _scheduledClassDao = null!;
    private Mock<IUniqueClassDao> _uniqueClassDao = null!;
    private Mock<IScheduleAssembler> _scheduleAssembler = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private GetTenantScheduleHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _scheduledClassDao = new Mock<IScheduledClassDao>(MockBehavior.Strict);
        _uniqueClassDao = new Mock<IUniqueClassDao>(MockBehavior.Strict);
        _scheduleAssembler = new Mock<IScheduleAssembler>(MockBehavior.Strict);
        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        _claimContext.SetupGet(context => context.TenantId).Returns(TenantId);
        _handler = new GetTenantScheduleHandler(_scheduledClassDao.Object, _uniqueClassDao.Object, _scheduleAssembler.Object, _claimContext.Object);
    }

    [Test]
    public async Task Handle_DelegatesToAssemblerWithTenantScopedLoaders_ReturnsFound()
    {
        var scheduledClasses = new List<ScheduledClass>();
        var uniqueClasses = new List<UniqueClass>();
        var schedule = new GetCourseScheduleDto
        {
            ScheduledClasses = [],
            UniqueClasses = []
        };

        _scheduledClassDao.Setup(dao => dao.GetByTenantAsync(TenantId)).ReturnsAsync(scheduledClasses);
        _uniqueClassDao.Setup(dao => dao.GetOnWeekForTenantAsync(TenantId, ClassDatePointer)).ReturnsAsync(uniqueClasses);
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

        GetTenantScheduleResult result = await _handler.Handle(new GetTenantScheduleQuery(ClassDatePointer));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf<GetTenantScheduleResult.Found>());
            Assert.That(((GetTenantScheduleResult.Found)result).Schedule, Is.SameAs(schedule));
        });
    }
}
