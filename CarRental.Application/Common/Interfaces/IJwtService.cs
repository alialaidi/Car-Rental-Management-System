using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _CarRental.Application.Common.Interfaces
{
    public interface IJwtService
    {
        (string Token, DateTime ExpiresAtUtc) GenerateAccessToken(int userId, string username, IEnumerable<string> roles);

        (string Token, DateTime ExpiresAtUtc) GenerateRefreshToken();
    }
}
