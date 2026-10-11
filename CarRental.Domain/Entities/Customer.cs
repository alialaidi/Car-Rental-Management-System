using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _CarRental.Domain.Entities
{
    public class Customer
    {
        public int CustomerId { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string DrivingLicenseNumber { get; set; } = string.Empty;
        public DateTime DrivingLicenseExpiry { get; set; }
        public string? Address { get; set; }
    }
}
