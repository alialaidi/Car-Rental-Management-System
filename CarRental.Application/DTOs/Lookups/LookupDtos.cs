namespace _CarRental.Application.DTOs.Lookups;
public sealed class LookupRequest { public string Name { get; set; } = string.Empty; public string? Description { get; set; } }
public sealed record LookupDto(int Id,string Name,string? Description);
