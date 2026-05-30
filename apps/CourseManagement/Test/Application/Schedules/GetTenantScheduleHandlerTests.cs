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

    private Mock<IScheduledClassDao> scheduledClassDao = null!;
    private Mock<IUniqueClassDao> uniqueClassDao = null!;
    private Mock<IScheduleAssembler> scheduleAssembler = null!;
    private Mock<IClaimContext> claimContext = null!;
    private GetTenantScheduleHandler handler = null!;

    [SetUp]
    public void SetUp()
    {
        scheduledClassDao = new Mock<IScheduledClassDao>(MockBehavior.Strict);
        uniqueClassDao = new Mock<IUniqueClassDao>(MockBehavior.Strict);
        scheduleAssembler = new Mock<IScheduleAssembler>(MockBehavior.Strict);
        claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        claimContext.SetupGet(context => context.TenantId).Returns(TenantId);
        handler = new GetTenantScheduleHandler(scheduledClassDao.Object, uniqueClassDao.Object, scheduleAssembler.Object, claimContext.Object);
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

        scheduledClassDao.Setup(dao => dao.GetByTenantAsync(TenantId)).ReturnsAsync(scheduledClasses);
        uniqueClassDao.Setup(dao => dao.GetOnWeekForTenantAsync(TenantId, ClassDatePointer)).ReturnsAsync(uniqueClasses);
        scheduleAssembler
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

        GetTenantScheduleResult result = await handler.Handle(new GetTenantScheduleQuery(ClassDatePointer));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf<GetTenantScheduleResult.Found>());
            Assert.That(((GetTenantScheduleResult.Found)result).Schedule, Is.SameAs(schedule));
        });
    }
}
