CREATE OR ALTER PROCEDURE dbo.usp_RefreshTokens_Revoke
    @TokenId INT,
    @RevokedAt DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.RefreshTokens
    SET RevokedAt = COALESCE(RevokedAt, @RevokedAt)
    WHERE TokenId = @TokenId;
END;
GO
