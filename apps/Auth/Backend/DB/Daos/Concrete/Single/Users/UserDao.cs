using System.Data;

using Backend.DB.Daos.Abstract.Single.Users;
using Backend.Entities.Tenants;
using Backend.Entities.Users;
using Backend.Transporters.Entities;

using DAMA.Software.MySqlUnitOfWork;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Daos.MySQL;

namespace Backend.DB.Daos.Concrete.Single.Users;

public sealed class UserDao : MySQLSingleDao<User>,
    IUserAuthenticationDao, IUserRegistrationDao, IUserDirectoryDao
{
    private const int MaxFailedLoginAttempts = 5;
    private const int LockoutSeconds = 900;

    public UserDao(MySqlConnection connection)
    {
        _tableName = "User";
        _connection = connection;
    }

    private static User MapFullUser(MySqlDataReader dataReader) => new User
    {
        Id = dataReader.GetGuid("Id"),
        UserName = dataReader.GetString("UserName"),
        PasswordHash = dataReader.GetString("PasswordHash"),
        Role = dataReader.GetString("Role"),
        IsDeleted = dataReader.GetBoolean("IsDeleted")
    };

    private static User MapMinimalUser(MySqlDataReader dataReader) => new User
    {
        Id = dataReader.GetGuid("Id"),
        UserName = dataReader.GetString("UserName")
    };

    protected override User MapReaderToEntity()
    {
        _entity = MapFullUser(_mySqlReader!);
        return _entity;
    }

    protected override List<User> MapReaderToEntitiesList()
    {
        _entitiesList = new List<User>();
        while (_mySqlReader!.Read())
        {
            _entity = MapFullUser(_mySqlReader);
            _entitiesList.Add(_entity);
        }
        _mySqlReader.Close();
        return _entitiesList;
    }

    public async Task<bool> TryCreateAsync(User user, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        const string sql = "INSERT INTO User (Id, UserName, PasswordHash, Role) " +
                           "VALUES (@Id, @UserName, @PasswordHash, @Role);";
        MySqlCommand com = new MySqlCommand(sql, _connection, sqlTransaction);
        com.Parameters.AddWithValue("@Id", user.Id);
        com.Parameters.AddWithValue("@UserName", user.UserName);
        com.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
        com.Parameters.AddWithValue("@Role", user.Role);

        try
        {
            await com.ExecuteNonQueryAsync();
            return true;
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            return false;
        }
    }

    public async Task<List<User>> GetByRoleForTenantPagedAsync(Guid tenantId, string role, int pageOffset, int pageSize)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand sqlCommand = GetCommandStoredProcedure("GetUsersByRoleForTenantPaged");
            sqlCommand.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            sqlCommand.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            sqlCommand.Parameters.AddWithValue("@userRole", role);
            sqlCommand.Parameters["@userRole"].Direction = ParameterDirection.Input;
            sqlCommand.Parameters.AddWithValue("@pageOffset", pageOffset);
            sqlCommand.Parameters["@pageOffset"].Direction = ParameterDirection.Input;
            sqlCommand.Parameters.AddWithValue("@pageSize", pageSize);
            sqlCommand.Parameters["@pageSize"].Direction = ParameterDirection.Input;

            _mySqlReader = (MySqlDataReader)await sqlCommand.ExecuteReaderAsync();

            List<User> users = new List<User>();
            while (await _mySqlReader.ReadAsync())
            {
                users.Add(MapMinimalUser(_mySqlReader));
            }
            await _mySqlReader.CloseAsync();
            return users;
        });
    }

    public async Task<long> CountByRoleForTenantAsync(Guid tenantId, string role)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("CountUsersByRoleForTenant");
            com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            com.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@userRole", role);
            com.Parameters["@userRole"].Direction = ParameterDirection.Input;

            object? scalar = await com.ExecuteScalarAsync();
            return Convert.ToInt64(scalar);
        });
    }

    public async Task<User?> GetByIdForTenantAsync(Guid userId, Guid tenantId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("GetUserByIdForTenant");
            com.Parameters.AddWithValue("@userId", userId.ToString());
            com.Parameters["@userId"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            com.Parameters["@tenantId"].Direction = ParameterDirection.Input;

            _mySqlReader = (MySqlDataReader)await com.ExecuteReaderAsync();

            if (!await _mySqlReader.ReadAsync())
            {
                await _mySqlReader.CloseAsync();
                return null;
            }
            User user = MapReaderToEntity();
            await _mySqlReader.CloseAsync();
            return (User?)user;
        });
    }

    public async Task<User?> GetStudentByExactNameForTenantAsync(Guid tenantId, string userName)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand sqlCommand = GetCommandStoredProcedure("GetStudentByExactNameForTenant");
            sqlCommand.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            sqlCommand.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            sqlCommand.Parameters.AddWithValue("@userName", userName);
            sqlCommand.Parameters["@userName"].Direction = ParameterDirection.Input;

            _mySqlReader = (MySqlDataReader)await sqlCommand.ExecuteReaderAsync();

            if (!await _mySqlReader.ReadAsync())
            {
                await _mySqlReader.CloseAsync();
                return null;
            }
            User user = MapMinimalUser(_mySqlReader);
            await _mySqlReader.CloseAsync();
            return (User?)user;
        });
    }

    public async Task<int> SoftDeleteForTenantAsync(Guid userId, Guid tenantId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("SoftDeleteUserForTenant");
            com.Parameters.AddWithValue("@userId", userId.ToString());
            com.Parameters["@userId"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            com.Parameters["@tenantId"].Direction = ParameterDirection.Input;

            return await com.ExecuteNonQueryAsync();
        });
    }

    public async Task<int> TryUpdateUserNameForTenantAsync(Guid userId, Guid tenantId, string newUserName)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("UpdateUserNameForTenant");
            com.Parameters.AddWithValue("@userId", userId.ToString());
            com.Parameters["@userId"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            com.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@newUserName", newUserName);
            com.Parameters["@newUserName"].Direction = ParameterDirection.Input;

            try
            {
                return await com.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                return -1;
            }
        });
    }

    public async Task<UserWithTenant?> ReadUserWithTenantByUserNameAsync(string userName)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("GetUserWithTenantByUserName");
            com.Parameters.AddWithValue("@userName", userName);
            com.Parameters["@userName"].Direction = ParameterDirection.Input;

            _mySqlReader = (MySqlDataReader)await com.ExecuteReaderAsync();

            if (!await _mySqlReader.ReadAsync())
            {
                await _mySqlReader.CloseAsync();
                return null;
            }

            User user = new User
            {
                Id = _mySqlReader.GetGuid("Id"),
                UserName = _mySqlReader.GetString("UserName"),
                PasswordHash = _mySqlReader.GetString("PasswordHash"),
                Role = _mySqlReader.GetString("Role"),
                LockedUntil = _mySqlReader.IsDBNull(_mySqlReader.GetOrdinal("LockedUntil"))
                    ? null
                    : _mySqlReader.GetDateTime("LockedUntil")
            };
            Tenant tenant = new Tenant
            {
                Id = _mySqlReader.GetGuid("TenantId"),
                Name = _mySqlReader.GetString("Name"),
                Timezone = _mySqlReader.GetString("Timezone")
            };

            await _mySqlReader.CloseAsync();
            return (UserWithTenant?)new UserWithTenant(user, tenant);
        });
    }

    public async Task RegisterFailedLoginAttemptAsync(Guid userId)
    {
        await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand command = new MySqlCommand("RegisterFailedLoginAttempt", _connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@userId", userId.ToString());
            command.Parameters["@userId"].Direction = ParameterDirection.Input;
            command.Parameters.AddWithValue("@maxAttempts", MaxFailedLoginAttempts);
            command.Parameters["@maxAttempts"].Direction = ParameterDirection.Input;
            command.Parameters.AddWithValue("@lockoutSeconds", LockoutSeconds);
            command.Parameters["@lockoutSeconds"].Direction = ParameterDirection.Input;

            return await command.ExecuteNonQueryAsync();
        });
    }

    public async Task ResetFailedLoginAttemptsAsync(Guid userId, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        MySqlCommand command = new MySqlCommand("ResetFailedLoginAttempts", _connection, sqlTransaction)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@userId", userId.ToString());
        command.Parameters["@userId"].Direction = ParameterDirection.Input;

        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdatePasswordHashAsync(Guid userId, string passwordHash, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        MySqlCommand command = new MySqlCommand("UpdateUserPasswordHash", _connection, sqlTransaction)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@userId", userId.ToString());
        command.Parameters["@userId"].Direction = ParameterDirection.Input;
        command.Parameters.AddWithValue("@passwordHash", passwordHash);
        command.Parameters["@passwordHash"].Direction = ParameterDirection.Input;

        await command.ExecuteNonQueryAsync();
    }
}
