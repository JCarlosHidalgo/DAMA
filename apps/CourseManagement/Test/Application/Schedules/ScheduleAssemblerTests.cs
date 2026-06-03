using AutoMapper;

using Backend.Application.Schedules;
using Backend.Dtos.Scheduleds.Output;
using Backend.Dtos.Schedules.Output;
using Backend.Dtos.Uniques.Output;
using Backend.Entities.Scheduleds;
using Backend.Entities.Uniques;

using Moq;

namespace Test.Application.Schedules;

[TestFixture]
public class ScheduleAssemblerTests
{
    private Mock<IMapper> _mapper = null!;
    private ScheduleAssembler _assembler = null!;

    [SetUp]
    public void SetUp()
    {
        _mapper = new Mock<IMapper>(MockBehavior.Strict);
        _assembler = new ScheduleAssembler(_mapper.Object);
    }

    [Test]
    public async Task AssembleAsync_InvokesBothLoadersAndMapsResults()
    {
        var scheduledClasses = new List<ScheduledClass> { new() { Id = Guid.NewGuid() } };
        var uniqueClasses = new List<UniqueClass> { new() { Id = Guid.NewGuid() } };
        var mappedScheduled = new List<GetScheduledClassDto>();
        var mappedUnique = new List<GetUniqueClassDto>();

        _mapper.Setup(map => map.Map<List<ScheduledClass>, List<GetScheduledClassDto>>(scheduledClasses)).Returns(mappedScheduled);
        _mapper.Setup(map => map.Map<List<UniqueClass>, List<GetUniqueClassDto>>(uniqueClasses)).Returns(mappedUnique);

        bool scheduledLoaderInvoked = false;
        bool uniqueLoaderInvoked = false;

        GetCourseScheduleDto result = await _assembler.AssembleAsync(
            loadScheduled: () =>
            {
                scheduledLoaderInvoked = true;
                return Task.FromResult(scheduledClasses);
            },
            loadUnique: () =>
            {
                uniqueLoaderInvoked = true;
                return Task.FromResult(uniqueClasses);
            });

        Assert.Multiple(() =>
        {
            Assert.That(scheduledLoaderInvoked, Is.True);
            Assert.That(uniqueLoaderInvoked, Is.True);
            Assert.That(result.ScheduledClasses, Is.SameAs(mappedScheduled));
            Assert.That(result.UniqueClasses, Is.SameAs(mappedUnique));
        });
    }
}
