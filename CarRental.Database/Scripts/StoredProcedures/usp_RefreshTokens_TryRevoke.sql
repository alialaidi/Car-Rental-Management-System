CREATE OR ALTER PROCEDURE dbo.usp_RefreshTokens_TryRevoke
    @TokenId INT,
    @RevokedAt DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @RowsAffected INT;

    UPDATE dbo.RefreshTokens WITH (UPDLOCK, HOLDLOCK)
    SET RevokedAt = @RevokedAt
    WHERE TokenId = @TokenId
      AND RevokedAt IS NULL
      AND ExpiresAt > SYSUTCDATETIME();

    SET @RowsAffected = @@ROWCOUNT;
    SELECT CONVERT(BIT, CASE WHEN @RowsAffected = 1 THEN 1 ELSE 0 END);
END;
GO
