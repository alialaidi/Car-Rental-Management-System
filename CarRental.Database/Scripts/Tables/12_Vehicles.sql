-- =====================================================================
-- Table: Vehicles
-- Purpose: Fleet inventory.
-- =====================================================================
IF OBJECT_ID('dbo.Vehicles', 'U') IS NOT NULL DROP TABLE dbo.Vehicles;
GO

CREATE TABLE dbo.Vehicles
(
    VehicleId               INT IDENTITY(1,1)  NOT NULL,
    PlateNumber             NVARCHAR(20)       NOT NULL,
    Make                    NVARCHAR(100)      NOT NULL,
    Model                   NVARCHAR(100)      NOT NULL,
    Year                    INT                NOT NULL,
    CategoryId              INT                NOT NULL,
    FuelTypeId              INT                NOT NULL,
    TransmissionTypeId      INT                NOT NULL,
    SeatsCount              INT                NOT NULL,
    DailyPrice              DECIMAL(18,2)      NOT NULL,
    BranchId                INT                NOT NULL,
    Status                  NVARCHAR(20)       NOT NULL CONSTRAINT DF_Vehicles_Status DEFAULT ('Available'),
    CurrentMileage          INT                NOT NULL CONSTRAINT DF_Vehicles_CurrentMileage DEFAULT (0),

    CONSTRAINT PK_Vehicles PRIMARY KEY CLUSTERED (VehicleId),
    CONSTRAINT UQ_Vehicles_PlateNumber UNIQUE (PlateNumber),
    CONSTRAINT FK_Vehicles_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.VehicleCategories (CategoryId),
    CONSTRAINT FK_Vehicles_FuelTypes FOREIGN KEY (FuelTypeId) REFERENCES dbo.FuelTypes (FuelTypeId),
    CONSTRAINT FK_Vehicles_TransmissionTypes FOREIGN KEY (TransmissionTypeId) REFERENCES dbo.TransmissionTypes (TransmissionTypeId),
    CONSTRAINT FK_Vehicles_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches (BranchId),
    CONSTRAINT CK_Vehicles_Status CHECK (Status IN ('Available','Reserved','Rented','Maintenance')),
    CONSTRAINT CK_Vehicles_Year CHECK (Year >= 1990),
    CONSTRAINT CK_Vehicles_DailyPrice CHECK (DailyPrice >= 0),
    CONSTRAINT CK_Vehicles_CurrentMileage CHECK (CurrentMileage >= 0)
);
GO

CREATE NONCLUSTERED INDEX IX_Vehicles_BranchId    ON dbo.Vehicles (BranchId);
CREATE NONCLUSTERED INDEX IX_Vehicles_CategoryId   ON dbo.Vehicles (CategoryId);
CREATE NONCLUSTERED INDEX IX_Vehicles_Status        ON dbo.Vehicles (Status);
GO
