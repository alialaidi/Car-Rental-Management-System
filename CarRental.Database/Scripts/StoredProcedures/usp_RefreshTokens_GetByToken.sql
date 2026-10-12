CREATE OR ALTER PROCEDURE dbo.usp_RefreshTokens_GetByToken
    @Token NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TokenId, UserId, Token, ExpiresAt, RevokedAt, CreatedAt
    FROM dbo.RefreshTokens
    WHERE Token = @Token;
END;
GO
