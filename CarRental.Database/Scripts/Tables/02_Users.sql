-- =====================================================================
-- Table: Users
-- Purpose: Base authentication identity shared by Customers & Employees.
-- =====================================================================
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
GO

CREATE TABLE dbo.Users
(
    UserId          INT IDENTITY(1,1)      NOT NULL,
    Username        NVARCHAR(50)           NOT NULL,
    Email           NVARCHAR(255)          NOT NULL,
    PasswordHash    NVARCHAR(255)          NOT NULL,
    PhoneNumber     NVARCHAR(20)           NULL,
    IsActive        BIT                    NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
    CreatedAt       DATETIME2(3)           NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (UserId),
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);
GO
