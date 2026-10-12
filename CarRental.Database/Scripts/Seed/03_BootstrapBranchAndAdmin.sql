-- Supply a precomputed password hash through SQLCMD; never commit a real password.
:on error exit
USE CarRentalDB;
GO

DECLARE @AdminPasswordHash NVARCHAR(255) = N'$(AdminPasswordHash)';
IF NULLIF(LTRIM(RTRIM(@AdminPasswordHash)), N'') IS NULL
    THROW 51002, 'SQLCMD variable AdminPasswordHash must contain a precomputed password hash.', 1;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Branches WHERE Name = N'Head Office' AND City = N'Amman')
BEGIN
    INSERT INTO dbo.Branches (Name, Address, City, Phone)
    VALUES (N'Head Office', N'Queen Rania Street', N'Amman', NULL);
END;
GO

DECLARE @AdminPasswordHash NVARCHAR(255) = N'$(AdminPasswordHash)';
DECLARE @AdminUserId INT;
DECLARE @AdminRoleId INT;

SELECT @AdminUserId = UserId FROM dbo.Users WHERE Username = N'admin';
IF @AdminUserId IS NULL
BEGIN
    INSERT INTO dbo.Users (Username, Email, PasswordHash, IsActive)
    VALUES (N'admin', N'admin@carrental.local', @AdminPasswordHash, 1);
    SET @AdminUserId = CONVERT(INT, SCOPE_IDENTITY());
END;

SELECT @AdminRoleId = RoleId FROM dbo.Roles WHERE RoleName = N'Admin';
IF @AdminRoleId IS NULL
    THROW 51003, 'The Admin role is missing. Run Seed/01_Roles.sql first.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.UserRoles WHERE UserId = @AdminUserId AND RoleId = @AdminRoleId)
    INSERT INTO dbo.UserRoles (UserId, RoleId) VALUES (@AdminUserId, @AdminRoleId);
GO
