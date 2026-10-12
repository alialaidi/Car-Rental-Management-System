CREATE OR ALTER PROCEDURE dbo.usp_RefreshTokens_Create
    @UserId INT,
    @Token NVARCHAR(500),
    @ExpiresAt DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    INSERT INTO dbo.RefreshTokens (UserId, Token, ExpiresAt)
    VALUES (@UserId, @Token, @ExpiresAt);

    SELECT CONVERT(INT, SCOPE_IDENTITY()) AS TokenId;
END;
GO
