using Backend.Entities;

namespace Backend.DB.Daos.Abstract.Single;

public interface IOutboxEventDao : IOutboxDao<OutboxEvent>
{
}
