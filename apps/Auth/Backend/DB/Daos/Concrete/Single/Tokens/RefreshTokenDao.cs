using System.Data;

using Backend.DB.Daos.Abstract.Single.Tokens;
using Backend.Entities.Tenants;
using Backend.Entities.Tokens;
using Backend.Entities.Users;
using Backend.Transporters.Entities;

using DAMA.Software.MySqlUnitOfWork;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Daos.MySQL;

namespace Backend.DB.Daos.Concrete.Single.Tokens;

public sealed class RefreshTokenDao : IRefreshTokenWriteDao, IRefreshTokenReadDao
{
    private readonly MySqlConnection _connection;

    public RefreshTokenDao(MySqlConnection connection)
    {
        _connection = connection;
    }

    public async Task CreateAsync(RefreshToken refreshToken, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        const string sql = "INSERT INTO RefreshToken (Id, UserId, TokenHash, ExpiresAt, RevokedAt, CreatedAt) " +
                           "VALUES (@Id, @UserId, @TokenHash, @ExpiresAt, NULL, @CreatedAt);";
        MySqlCommand insertCommand = new MySqlCommand(sql, _connection, sqlTransaction);
        insertCommand.Parameters.AddWithValue("@Id", refreshToken.Id.ToString());
        insertCommand.Parameters.AddWithValue("@UserId", refreshToken.UserId.ToString());
        insertCommand.Parameters.AddWithValue("@TokenHash", refreshToken.TokenHash);
        insertCommand.Parameters.AddWithValue("@ExpiresAt", refreshToken.ExpiresAt);
        insertCommand.Parameters.AddWithValue("@CreatedAt", refreshToken.CreatedAt);

        await insertCommand.ExecuteNonQueryAsync();
    }

    public async Task RevokeAsync(Guid id, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        MySqlCommand command = new MySqlCommand("RevokeRefreshToken", _connection, sqlTransaction)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@tokenId", id.ToString());
        command.Parameters["@tokenId"].Direction = ParameterDirection.Input;

        await command.ExecuteNonQueryAsync();
    }

    public async Task RevokeAllForUserAsync(Guid userId, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        MySqlCommand command = new MySqlCommand("RevokeRefreshTokensForUser", _connection, sqlTransaction)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@ownerUserId", userId.ToString());
        command.Parameters["@ownerUserId"].Direction = ParameterDirection.Input;

        await command.ExecuteNonQueryAsync();
    }

    public async Task<RefreshTokenWithOwner?> GetByHashAsync(string tokenHash)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand command = new MySqlCommand("GetRefreshTokenByHash", _connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@searchTokenHash", tokenHash);
            command.Parameters["@searchTokenHash"].Direction = ParameterDirection.Input;

            MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                await reader.CloseAsync();
                return (RefreshTokenWithOwner?)null;
            }

            RefreshToken refreshToken = new RefreshToken
            {
                Id = reader.GetGuid("RefreshTokenId"),
                UserId = reader.GetGuid("RefreshUserId"),
                TokenHash = tokenHash,
                ExpiresAt = reader.GetDateTime("ExpiresAt"),
                RevokedAt = reader.IsDBNull(reader.GetOrdinal("RevokedAt"))
                    ? null
                    : reader.GetDateTime("RevokedAt"),
                CreatedAt = reader.GetDateTime("CreatedAt")
            };
            User user = new User
            {
                Id = reader.GetGuid("UserId"),
                UserName = reader.GetString("UserName"),
                PasswordHash = reader.GetString("PasswordHash"),
                Role = reader.GetString("Role")
            };
            Tenant tenant = new Tenant
            {
                Id = reader.GetGuid("TenantId"),
                Name = reader.GetString("TenantName"),
                Timezone = reader.GetString("Timezone")
            };

            await reader.CloseAsync();
            return (RefreshTokenWithOwner?)new RefreshTokenWithOwner(refreshToken, new UserWithTenant(user, tenant));
        });
    }
}
