using Backend.Events;
using Backend.Results.Events;

namespace Backend.Services.Abstract.Events;

public interface IClassDeletedHandler
{
    Task<ClassDeletedOutcome> HandleAsync(ClassDeletedEvent classDeletedEvent, CancellationToken cancellationToken);
}
