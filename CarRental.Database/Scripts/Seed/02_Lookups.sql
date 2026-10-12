-- Idempotently create the baseline vehicle lookup values.
USE CarRentalDB;
GO

INSERT INTO dbo.FuelTypes (Name)
SELECT source.Name
FROM (VALUES (N'Petrol'), (N'Diesel'), (N'Electric'), (N'Hybrid')) AS source(Name)
WHERE NOT EXISTS (SELECT 1 FROM dbo.FuelTypes AS target WHERE target.Name = source.Name);
GO

INSERT INTO dbo.TransmissionTypes (Name)
SELECT source.Name
FROM (VALUES (N'Automatic'), (N'Manual')) AS source(Name)
WHERE NOT EXISTS (SELECT 1 FROM dbo.TransmissionTypes AS target WHERE target.Name = source.Name);
GO

INSERT INTO dbo.VehicleCategories (Name, Description)
SELECT source.Name, source.Description
FROM (VALUES
    (N'Economy', N'Affordable, fuel-efficient vehicles.'),
    (N'Compact', N'Compact passenger vehicles.'),
    (N'Sedan', N'Passenger sedans.'),
    (N'SUV', N'Sport utility vehicles.'),
    (N'Luxury', N'Premium vehicles.'),
    (N'Van', N'Passenger and cargo vans.')
) AS source(Name, Description)
WHERE NOT EXISTS (SELECT 1 FROM dbo.VehicleCategories AS target WHERE target.Name = source.Name);
GO
