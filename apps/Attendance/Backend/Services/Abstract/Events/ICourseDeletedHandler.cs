using Backend.Events;
using Backend.Results.Events;

namespace Backend.Services.Abstract.Events;

public interface ICourseDeletedHandler
{
    Task<CourseDeletedOutcome> HandleAsync(CourseDeletedEvent courseDeletedEvent, CancellationToken cancellationToken);
}
