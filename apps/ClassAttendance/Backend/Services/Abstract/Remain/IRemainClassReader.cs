using Backend.Dtos.Remain.Output;
using Backend.Results.Remain;

namespace Backend.Services.Abstract.Remain;

public interface IRemainClassReader
{
    Task<GetRemainForStudentOutcome> GetForStudentAsync(Guid studentId);

    Task<RemainResponse> GetForCurrentStudentAsync();
}
