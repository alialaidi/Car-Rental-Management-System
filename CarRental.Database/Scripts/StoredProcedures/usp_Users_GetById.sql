CREATE OR ALTER PROCEDURE dbo.usp_Users_GetById
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT UserId, Username, Email, PasswordHash, PhoneNumber, IsActive, CreatedAt
    FROM dbo.Users
    WHERE UserId = @UserId;
END;
GO
