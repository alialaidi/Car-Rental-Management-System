using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _CarRental.Application.Common.Interfaces
{
    public interface IPasswordHasher
    {
        string Hash(string plainTextPassword /* هذا الباسوور الخام*/ );
        bool Verify(string plainTextPassword, string passwordHash);
    }
}
