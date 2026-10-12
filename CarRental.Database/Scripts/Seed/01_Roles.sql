-- Idempotently create the four application roles.
USE CarRentalDB;
GO

MERGE dbo.Roles AS target
USING (VALUES (N'Admin'), (N'BranchManager'), (N'Employee'), (N'Customer')) AS source(RoleName)
    ON target.RoleName = source.RoleName
WHEN NOT MATCHED BY TARGET THEN
    INSERT (RoleName) VALUES (source.RoleName);
GO
