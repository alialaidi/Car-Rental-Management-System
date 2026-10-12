using System.Data;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Interfaces.Repositories;
using _CarRental.Infrastructure.Persistence.Mappers;
using Microsoft.Data.SqlClient;

namespace _CarRental.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(SqlExecutor executor) : IUserRepository
{
    public Task<User?> GetByIdAsync(int userId) => executor.QuerySingleOrDefaultAsync(
        "usp_Users_GetById",
        UserMapper.Map,
        parameters => AddInt32(parameters, "@UserId", userId));

    public Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail) => executor.QuerySingleOrDefaultAsync(
        "usp_Users_GetByUsernameOrEmail",
        UserMapper.Map,
        parameters => AddNVarChar(parameters, "@UsernameOrEmail", 255, usernameOrEmail));

    public Task<bool> ExistsByUsernameAsync(string username) => executor.ExecuteScalarBooleanAsync(
        "usp_Users_Exists",
        parameters =>
        {
            AddNVarChar(parameters, "@Username", 50, username);
            parameters.Add("@Email", SqlDbType.NVarChar, 255).Value = DBNull.Value;
        });

    public Task<bool> ExistsByEmailAsync(string email) => executor.ExecuteScalarBooleanAsync(
        "usp_Users_Exists",
        parameters =>
        {
            parameters.Add("@Username", SqlDbType.NVarChar, 50).Value = DBNull.Value;
            AddNVarChar(parameters, "@Email", 255, email);
        });

    public async Task<int> CreateAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        try
        {
            return await executor.ExecuteScalarInt32Async("usp_Users_Create", parameters =>
            {
                AddNVarChar(parameters, "@Username", 50, user.Username);
                AddNVarChar(parameters, "@Email", 255, user.Email);
                AddNVarChar(parameters, "@PasswordHash", 255, user.PasswordHash);
                parameters.Add("@PhoneNumber", SqlDbType.NVarChar, 20).Value = (object?)user.PhoneNumber ?? DBNull.Value;
            });
        }
        catch (SqlException exception)
        {
            SqlErrorTranslator.ThrowIfKnown(exception);
            throw;
        }
    }

    public async Task AssignRoleAsync(int userId, int roleId)
    {
        try
        {
            await executor.ExecuteNonQueryAsync("usp_UserRoles_Assign", parameters =>
            {
                AddInt32(parameters, "@UserId", userId);
                AddInt32(parameters, "@RoleId", roleId);
            });
        }
        catch (SqlException exception)
        {
            SqlErrorTranslator.ThrowIfKnown(exception);
            throw;
        }
    }

    public Task<IReadOnlyList<string>> GetRoleNamesAsync(int userId) => executor.QueryAsync(
        "usp_UserRoles_GetNames",
        reader => reader.GetString(reader.GetOrdinal("RoleName")),
        parameters => AddInt32(parameters, "@UserId", userId));

    public Task<int?> GetRoleIdByNameAsync(string roleName) => executor.ExecuteScalarNullableInt32Async(
        "usp_Roles_GetIdByName",
        parameters => AddNVarChar(parameters, "@RoleName", 50, roleName));

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> ListAsync(int page, int pageSize, string? search, bool? isActive)
    {
        var items = await executor.QueryAsync("usp_Users_List", UserMapper.Map, parameters =>
        {
            AddInt32(parameters, "@Page", page);
            AddInt32(parameters, "@PageSize", pageSize);
            parameters.Add("@Search", SqlDbType.NVarChar, 255).Value = (object?)search ?? DBNull.Value;
            parameters.Add("@IsActive", SqlDbType.Bit).Value = (object?)isActive ?? DBNull.Value;
        });
        var totalCount = await executor.ExecuteScalarInt32Async("usp_Users_Count", parameters =>
        {
            parameters.Add("@Search", SqlDbType.NVarChar, 255).Value = (object?)search ?? DBNull.Value;
            parameters.Add("@IsActive", SqlDbType.Bit).Value = (object?)isActive ?? DBNull.Value;
        });
        return (items, totalCount);
    }

    public async Task SetActiveAsync(int userId, bool isActive)
    {
        await ExecuteAdminChangeAsync("usp_Users_SetActive", parameters =>
        {
            AddInt32(parameters, "@UserId", userId);
            parameters.Add("@IsActive", SqlDbType.Bit).Value = isActive;
        });
    }

    public async Task RemoveRoleAsync(int userId, int roleId)
    {
        await ExecuteAdminChangeAsync("usp_UserRoles_Remove", parameters =>
        {
            AddInt32(parameters, "@UserId", userId);
            AddInt32(parameters, "@RoleId", roleId);
        });
    }

    private async Task ExecuteAdminChangeAsync(string procedure, Action<SqlParameterCollection> configure)
    {
        try { await executor.ExecuteNonQueryAsync(procedure, configure); }
        catch (SqlException exception) { SqlErrorTranslator.ThrowIfKnown(exception); throw; }
    }

    private static void AddInt32(SqlParameterCollection parameters, string name, int value) =>
        parameters.Add(name, SqlDbType.Int).Value = value;

    private static void AddNVarChar(SqlParameterCollection parameters, string name, int size, string value) =>
        parameters.Add(name, SqlDbType.NVarChar, size).Value = value;
}
