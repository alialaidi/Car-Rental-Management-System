using System.Data;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Interfaces.Repositories;
using _CarRental.Infrastructure.Persistence.Mappers;
using Microsoft.Data.SqlClient;

namespace _CarRental.Infrastructure.Persistence.Repositories;

internal sealed class CustomerRepository(SqlExecutor executor) : ICustomerRepository
{
    public Task<Customer?> GetByUserIdAsync(int userId) => executor.QuerySingleOrDefaultAsync(
        "usp_Customers_GetByUserId",
        CustomerMapper.Map,
        parameters => parameters.Add("@UserId", SqlDbType.Int).Value = userId);

    public Task<bool> ExistsByDrivingLicenseNumberAsync(string drivingLicenseNumber) => executor.ExecuteScalarBooleanAsync(
        "usp_Customers_ExistsByLicense",
        parameters => parameters.Add("@DrivingLicenseNumber", SqlDbType.NVarChar, 50).Value = drivingLicenseNumber);

    public async Task<int> CreateAsync(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);
        try
        {
            return await executor.ExecuteScalarInt32Async("usp_Customers_Create", parameters =>
            {
                parameters.Add("@UserId", SqlDbType.Int).Value = customer.UserId;
                parameters.Add("@FullName", SqlDbType.NVarChar, 150).Value = customer.FullName;
                parameters.Add("@DrivingLicenseNumber", SqlDbType.NVarChar, 50).Value = customer.DrivingLicenseNumber;
                parameters.Add("@DrivingLicenseExpiry", SqlDbType.Date).Value = customer.DrivingLicenseExpiry.Date;
                parameters.Add("@Address", SqlDbType.NVarChar, 255).Value = (object?)customer.Address ?? DBNull.Value;
            });
        }
        catch (SqlException exception)
        {
            SqlErrorTranslator.ThrowIfKnown(exception);
            throw;
        }
    }
}
