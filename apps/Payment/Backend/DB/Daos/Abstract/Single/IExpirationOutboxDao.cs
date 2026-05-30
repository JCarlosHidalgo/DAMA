using Backend.Entities;

namespace Backend.DB.Daos.Abstract.Single;

public interface IExpirationOutboxDao : IOutboxDao<ExpirationOutboxEvent>
{
}
