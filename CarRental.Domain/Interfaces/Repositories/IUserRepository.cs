using _CarRental.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _CarRental.Domain.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int userId);
        Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail);
        Task<bool> ExistsByUsernameAsync(string username);
        Task<bool> ExistsByEmailAsync(string email);
        Task<int> CreateAsync(User user);
        Task AssignRoleAsync(int userId, int roleId);
        Task<IReadOnlyList<string>> GetRoleNamesAsync(int userId);
        Task<int?> GetRoleIdByNameAsync(string roleName);
        Task<(IReadOnlyList<User> Items, int TotalCount)> ListAsync(int page, int pageSize, string? search, bool? isActive);
        Task SetActiveAsync(int userId, bool isActive);
        Task RemoveRoleAsync(int userId, int roleId);
    }
}
