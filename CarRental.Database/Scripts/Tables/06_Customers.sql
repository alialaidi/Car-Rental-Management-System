-- =====================================================================
-- Table: Customers
-- Purpose: Customer profile, 1-to-1 with Users.
-- =====================================================================
IF OBJECT_ID('dbo.Customers', 'U') IS NOT NULL DROP TABLE dbo.Customers;
GO

CREATE TABLE dbo.Customers
(
    CustomerId              INT IDENTITY(1,1)  NOT NULL,
    UserId                  INT                NOT NULL,
    FullName                NVARCHAR(150)      NOT NULL,
    DrivingLicenseNumber    NVARCHAR(50)       NOT NULL,
    DrivingLicenseExpiry    DATE               NOT NULL,
    Address                 NVARCHAR(255)      NULL,

    CONSTRAINT PK_Customers PRIMARY KEY CLUSTERED (CustomerId),
    CONSTRAINT UQ_Customers_UserId UNIQUE (UserId),
    CONSTRAINT UQ_Customers_DrivingLicenseNumber UNIQUE (DrivingLicenseNumber),
    CONSTRAINT FK_Customers_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId) ON DELETE CASCADE
);
GO
