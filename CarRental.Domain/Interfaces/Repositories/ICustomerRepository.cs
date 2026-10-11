using _CarRental.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _CarRental.Domain.Interfaces.Repositories
{
    public interface ICustomerRepository
    {
        Task<Customer?> GetByUserIdAsync(int userId);
        Task<bool> ExistsByDrivingLicenseNumberAsync(string drivingLicenseNumber);
        Task<int> CreateAsync(Customer customer);
    }
}
