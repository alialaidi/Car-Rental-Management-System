-- =====================================================================
-- Table: RefreshTokens
-- Purpose: JWT refresh tokens issued per user session.
-- =====================================================================
IF OBJECT_ID('dbo.RefreshTokens', 'U') IS NOT NULL DROP TABLE dbo.RefreshTokens;
GO

CREATE TABLE dbo.RefreshTokens
(
    TokenId     INT IDENTITY(1,1)  NOT NULL,
    UserId      INT                NOT NULL,
    Token       NVARCHAR(500)      NOT NULL,
    ExpiresAt   DATETIME2(3)       NOT NULL,
    RevokedAt   DATETIME2(3)       NULL,
    CreatedAt   DATETIME2(3)       NOT NULL CONSTRAINT DF_RefreshTokens_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_RefreshTokens PRIMARY KEY CLUSTERED (TokenId),
    CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX IX_RefreshTokens_UserId ON dbo.RefreshTokens (UserId);
CREATE NONCLUSTERED INDEX IX_RefreshTokens_Token   ON dbo.RefreshTokens (Token);
GO
