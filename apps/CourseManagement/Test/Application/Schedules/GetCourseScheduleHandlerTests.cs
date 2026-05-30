using Backend.Application.Schedules;
using Backend.DB.Daos.Abstract.Single.Scheduleds;
using Backend.DB.Daos.Abstract.Single.Uniques;
using Backend.Dtos.Schedules.Output;
using Backend.Entities.Scheduleds;
using Backend.Entities.Uniques;
using Backend.Results.Schedules;

using Moq;

namespace Test.Application.Schedules;

[TestFixture]
public class GetCourseScheduleHandlerTests
{
    private static readonly Guid CourseId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateOnly CurrentDate = new(2025, 6, 1);

    private Mock<IScheduledClassDao> scheduledClassDao = null!;
    private Mock<IUniqueClassDao> uniqueClassDao = null!;
    private Mock<IScheduleAssembler> scheduleAssembler = null!;
    private GetCourseScheduleHandler handler = null!;

    [SetUp]
    public void SetUp()
    {
        scheduledClassDao = new Mock<IScheduledClassDao>(MockBehavior.Strict);
        uniqueClassDao = new Mock<IUniqueClassDao>(MockBehavior.Strict);
        scheduleAssembler = new Mock<IScheduleAssembler>(MockBehavior.Strict);
        handler = new GetCourseScheduleHandler(scheduledClassDao.Object, uniqueClassDao.Object, scheduleAssembler.Object);
    }

    [Test]
    public async Task Handle_DelegatesToAssemblerWithCourseScopedLoaders_ReturnsFound()
    {
        var scheduledClasses = new List<ScheduledClass>();
        var uniqueClasses = new List<UniqueClass>();
        var schedule = new GetCourseScheduleDto
        {
            ScheduledClasses = [],
            UniqueClasses = []
        };

        scheduledClassDao.Setup(dao => dao.GetScheduledClassesByCourseIdAsync(CourseId)).ReturnsAsync(scheduledClasses);
        uniqueClassDao.Setup(dao => dao.GetUniqueClassesOnSameWeekByDateAsync(CourseId, CurrentDate)).ReturnsAsync(uniqueClasses);
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

        GetCourseScheduleResult result = await handler.Handle(new GetCourseScheduleQuery(CourseId, CurrentDate));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf<GetCourseScheduleResult.Found>());
            Assert.That(((GetCourseScheduleResult.Found)result).Schedule, Is.SameAs(schedule));
        });
    }
}
