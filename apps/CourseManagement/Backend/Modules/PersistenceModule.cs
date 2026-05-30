using Backend.DB.Utils;

using DAMA.Software.MySqlUnitOfWork;

using MySql.Data.MySqlClient;

namespace Backend.Modules;

public sealed class PersistenceModule : IServiceModule
{
    public int Order => 40;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<MySqlConnection>(_ => new MySqlConnection(DBConnector.GetConnectionString()));
        services.AddScoped<IUnitOfWork, MySqlUnitOfWork>();
    }
}
