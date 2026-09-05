-- =====================================================================
-- Table: Reviews
-- Purpose: One review per completed rental contract.
-- Customer & Vehicle are resolved via RentalContracts (no redundant FKs).
-- =====================================================================
IF OBJECT_ID('dbo.Reviews', 'U') IS NOT NULL DROP TABLE dbo.Reviews;
GO

CREATE TABLE dbo.Reviews
(
    ReviewId    INT IDENTITY(1,1)  NOT NULL,
    ContractId   INT                NOT NULL,
    Rating        INT                NOT NULL,
    Comment        NVARCHAR(1000)     NULL,
    CreatedAt      DATETIME2(3)       NOT NULL CONSTRAINT DF_Reviews_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Reviews PRIMARY KEY CLUSTERED (ReviewId),
    CONSTRAINT UQ_Reviews_ContractId UNIQUE (ContractId),
    CONSTRAINT FK_Reviews_RentalContracts FOREIGN KEY (ContractId) REFERENCES dbo.RentalContracts (ContractId),
    CONSTRAINT CK_Reviews_Rating CHECK (Rating BETWEEN 1 AND 5)
);
GO
