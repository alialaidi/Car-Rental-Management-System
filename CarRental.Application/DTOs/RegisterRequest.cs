using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _CarRental.Application.DTOs
{
    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }

        public string FullName { get; set; } = string.Empty;
        public string DrivingLicenseNumber { get; set; } = string.Empty;
        public DateTime DrivingLicenseExpiry { get; set; }
        public string? Address { get; set; }
    }
}
