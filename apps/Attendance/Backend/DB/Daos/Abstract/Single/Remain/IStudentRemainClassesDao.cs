using Backend.Entities.Remain;

using DAMA.Software.MySqlUnitOfWork;

using SQLDaosPackage.Daos;

namespace Backend.DB.Daos.Abstract.Single.Remain;

public interface IStudentRemainClassesDao : ISingleDao<StudentRemainClasses>
{
    Task IncrementAsync(Guid tenantId, Guid studentId, int delta, string? studentName, ITransactionContext transaction);

    Task<int> IncrementAllInTenantAsync(Guid tenantId, int delta, ITransactionContext transaction);

    Task<bool> TryDecrementAsync(Guid tenantId, Guid studentId, ITransactionContext transaction);

    Task<StudentRemainClasses?> ReadAsync(Guid tenantId, Guid studentId);
}
