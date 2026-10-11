using _CarRental.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _CarRental.Domain.Interfaces.Repositories
{
    public interface IRefreshTokenRepository
    {
        Task<int> CreateAsync(RefreshToken refreshToken);
        Task<RefreshToken?> GetByTokenAsync(string token);
        Task RevokeAsync(int tokenId, DateTime revokedAtUtc);
        Task RevokeAllForUserAsync(int userId, DateTime revokedAtUtc);// تأجيل
    }
}
