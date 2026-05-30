using System.Data;
using System.Text;

using Backend.DB.Daos.Abstract.Single.DebtTemplates;
using Backend.Entities.DebtTemplates;

using DAMA.Software.MySqlUnitOfWork;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Daos.MySQL;

namespace Backend.DB.Daos.Concrete.Single.DebtTemplates;

public sealed class DebtTemplateDao : MySQLSingleDao<DebtTemplate>, IDebtTemplateDao
{
    public DebtTemplateDao(MySqlConnection connection)
    {
        _tableName = "DebtTemplate";
        _connection = connection;
    }

    protected override DebtTemplate MapReaderToEntity()
    {
        _entity = new DebtTemplate
        {
            Id = _mySqlReader!.GetGuid("Id"),
            TenantId = _mySqlReader!.GetGuid("TenantId"),
            Description = _mySqlReader!.GetString("Description"),
            ClassQuantity = _mySqlReader!.GetInt32("ClassQuantity"),
            Cost = _mySqlReader!.GetInt32("Cost")
        };
        return _entity;
    }

    protected override List<DebtTemplate> MapReaderToEntitiesList()
    {
        _entitiesList = new List<DebtTemplate>();
        while (_mySqlReader!.Read())
        {
            _entity = new DebtTemplate
            {
                Id = _mySqlReader!.GetGuid("Id"),
                TenantId = _mySqlReader!.GetGuid("TenantId"),
                Description = _mySqlReader!.GetString("Description"),
                ClassQuantity = _mySqlReader!.GetInt32("ClassQuantity"),
                Cost = _mySqlReader!.GetInt32("Cost")
            };
            _entitiesList.Add(_entity);
        }
        _mySqlReader.Close();
        return _entitiesList;
    }

    protected override StringBuilder CreateCommandIntoStringBuilder(DebtTemplate template) =>
        throw new NotSupportedException("Use CreateAsync(DebtTemplate).");

    protected override StringBuilder UpdateCommandIntoStringBuilder(DebtTemplate template) =>
        throw new NotSupportedException("Use UpdateForTenantAsync.");

    public new async Task CreateAsync(DebtTemplate template)
    {
        await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            const string sql = "INSERT INTO DebtTemplate (Id, TenantId, Description, ClassQuantity, Cost) " +
                               "VALUES (@Id, @TenantId, @Description, @ClassQuantity, @Cost);";
            MySqlCommand insertCommand = new MySqlCommand(sql, _connection);
            insertCommand.Parameters.AddWithValue("@Id", template.Id);
            insertCommand.Parameters.AddWithValue("@TenantId", template.TenantId);
            insertCommand.Parameters.AddWithValue("@Description", template.Description);
            insertCommand.Parameters.AddWithValue("@ClassQuantity", template.ClassQuantity);
            insertCommand.Parameters.AddWithValue("@Cost", template.Cost);

            await insertCommand.ExecuteNonQueryAsync();
        });
    }

    public async Task CreateAsync(DebtTemplate template, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        const string sql = "INSERT INTO DebtTemplate (Id, TenantId, Description, ClassQuantity, Cost) " +
                           "VALUES (@Id, @TenantId, @Description, @ClassQuantity, @Cost);";
        MySqlCommand insertCommand = new MySqlCommand(sql, _connection, sqlTransaction);
        insertCommand.Parameters.AddWithValue("@Id", template.Id);
        insertCommand.Parameters.AddWithValue("@TenantId", template.TenantId);
        insertCommand.Parameters.AddWithValue("@Description", template.Description);
        insertCommand.Parameters.AddWithValue("@ClassQuantity", template.ClassQuantity);
        insertCommand.Parameters.AddWithValue("@Cost", template.Cost);

        await insertCommand.ExecuteNonQueryAsync();
    }

    public async Task<List<DebtTemplate>> GetByTenantAsync(Guid tenantId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand selectCommand = GetCommandStoredProcedure("GetDebtTemplatesByTenant");
            selectCommand.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            selectCommand.Parameters["@tenantId"].Direction = ParameterDirection.Input;

            _mySqlReader = (MySqlDataReader)await selectCommand.ExecuteReaderAsync();
            return MapReaderToEntitiesList();
        });
    }

    public async Task<DebtTemplate?> GetByIdForTenantAsync(Guid tenantId, Guid templateId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand selectCommand = GetCommandStoredProcedure("GetDebtTemplateByIdForTenant");
            selectCommand.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            selectCommand.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            selectCommand.Parameters.AddWithValue("@templateId", templateId.ToString());
            selectCommand.Parameters["@templateId"].Direction = ParameterDirection.Input;

            _mySqlReader = (MySqlDataReader)await selectCommand.ExecuteReaderAsync();
            if (!await _mySqlReader.ReadAsync())
            {
                _mySqlReader.Close();
                return null;
            }
            DebtTemplate template = MapReaderToEntity();
            _mySqlReader.Close();
            return (DebtTemplate?)template;
        });
    }

    public async Task<bool> UpdateForTenantAsync(Guid tenantId, Guid templateId, string description, int classQuantity, int cost)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand updateCommand = GetCommandStoredProcedure("UpdateDebtTemplateForTenant");
            updateCommand.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            updateCommand.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            updateCommand.Parameters.AddWithValue("@templateId", templateId.ToString());
            updateCommand.Parameters["@templateId"].Direction = ParameterDirection.Input;
            updateCommand.Parameters.AddWithValue("@description", description);
            updateCommand.Parameters["@description"].Direction = ParameterDirection.Input;
            updateCommand.Parameters.AddWithValue("@classQuantity", classQuantity);
            updateCommand.Parameters["@classQuantity"].Direction = ParameterDirection.Input;
            updateCommand.Parameters.AddWithValue("@cost", cost);
            updateCommand.Parameters["@cost"].Direction = ParameterDirection.Input;

            object? scalar = await updateCommand.ExecuteScalarAsync();
            return scalar != null && Convert.ToInt64(scalar) > 0;
        });
    }

    public async Task<bool> DeleteForTenantAsync(Guid tenantId, Guid templateId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand deleteCommand = GetCommandStoredProcedure("DeleteDebtTemplateForTenant");
            deleteCommand.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            deleteCommand.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            deleteCommand.Parameters.AddWithValue("@templateId", templateId.ToString());
            deleteCommand.Parameters["@templateId"].Direction = ParameterDirection.Input;

            object? scalar = await deleteCommand.ExecuteScalarAsync();
            return scalar != null && Convert.ToInt64(scalar) > 0;
        });
    }
}
