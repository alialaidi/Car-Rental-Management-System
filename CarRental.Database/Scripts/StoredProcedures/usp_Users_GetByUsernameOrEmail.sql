CREATE OR ALTER PROCEDURE dbo.usp_Users_GetByUsernameOrEmail
    @UsernameOrEmail NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT UserId, Username, Email, PasswordHash, PhoneNumber, IsActive, CreatedAt
    FROM dbo.Users
    WHERE Username = @UsernameOrEmail OR Email = @UsernameOrEmail;
END;
GO
