using Backend.Events;
using Backend.Results.Events;

namespace Backend.Services.Abstract.Events;

public interface IStudentRegisteredHandler
{
    Task<StudentRegisteredOutcome> HandleAsync(StudentRegisteredEvent studentRegisteredEvent, CancellationToken cancellationToken);
}
