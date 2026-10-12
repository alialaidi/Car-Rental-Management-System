CREATE OR ALTER PROCEDURE dbo.usp_Users_Create
    @Username NVARCHAR(50),
    @Email NVARCHAR(255),
    @PasswordHash NVARCHAR(255),
    @PhoneNumber NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF EXISTS (SELECT 1 FROM dbo.Users WHERE Username = @Username)
        THROW 50020, 'Username already exists.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Users WHERE Email = @Email)
        THROW 50021, 'Email already exists.', 1;

    INSERT INTO dbo.Users (Username, Email, PasswordHash, PhoneNumber)
    VALUES (@Username, @Email, @PasswordHash, @PhoneNumber);

    SELECT CONVERT(INT, SCOPE_IDENTITY()) AS UserId;
END;
GO
