CREATE OR ALTER PROCEDURE dbo.usp_RefreshTokens_RevokeAllForUser
    @UserId INT,
    @RevokedAt DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.RefreshTokens
    SET RevokedAt = COALESCE(RevokedAt, @RevokedAt)
    WHERE UserId = @UserId;
END;
GO
