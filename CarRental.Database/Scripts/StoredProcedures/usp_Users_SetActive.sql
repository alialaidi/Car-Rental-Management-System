CREATE OR ALTER PROCEDURE dbo.usp_Users_SetActive @UserId INT, @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Users SET IsActive=@IsActive WHERE UserId=@UserId;
    IF @@ROWCOUNT = 0 THROW 50044, 'User does not exist.', 1;
END;
GO
