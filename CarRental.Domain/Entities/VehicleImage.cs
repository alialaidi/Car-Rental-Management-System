namespace _CarRental.Domain.Entities;
public sealed class VehicleImage { public int ImageId { get; set; } public int VehicleId { get; set; } public string ImageUrl { get; set; } = string.Empty; public bool IsPrimary { get; set; } }
