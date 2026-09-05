-- =====================================================================
-- Table: MaintenanceRecords
-- Purpose: Maintenance history per vehicle (Step 10).
-- =====================================================================
IF OBJECT_ID('dbo.MaintenanceRecords', 'U') IS NOT NULL DROP TABLE dbo.MaintenanceRecords;
GO

CREATE TABLE dbo.MaintenanceRecords
(
    MaintenanceId           INT IDENTITY(1,1)  NOT NULL,
    VehicleId                 INT                NOT NULL,
    CreatedByEmployeeId        INT                NOT NULL,
    MaintenanceType             NVARCHAR(100)      NOT NULL,
    ProblemDescription          NVARCHAR(1000)     NULL,
    StartDate                   DATE               NOT NULL,
    ExpectedEndDate              DATE               NULL,
    ActualEndDate                 DATE               NULL,
    Cost                          DECIMAL(18,2)      NULL,
    Status                        NVARCHAR(20)       NOT NULL CONSTRAINT DF_MaintenanceRecords_Status DEFAULT ('InProgress'),

    CONSTRAINT PK_MaintenanceRecords PRIMARY KEY CLUSTERED (MaintenanceId),
    CONSTRAINT FK_MaintenanceRecords_Vehicles FOREIGN KEY (VehicleId) REFERENCES dbo.Vehicles (VehicleId),
    CONSTRAINT FK_MaintenanceRecords_Employee FOREIGN KEY (CreatedByEmployeeId) REFERENCES dbo.Employees (EmployeeId),
    CONSTRAINT CK_MaintenanceRecords_Status CHECK (Status IN ('InProgress','Completed','Cancelled')),
    CONSTRAINT CK_MaintenanceRecords_Cost CHECK (Cost IS NULL OR Cost >= 0)
);
GO

CREATE NONCLUSTERED INDEX IX_MaintenanceRecords_VehicleId ON dbo.MaintenanceRecords (VehicleId);
CREATE NONCLUSTERED INDEX IX_MaintenanceRecords_Status ON dbo.MaintenanceRecords (Status);
GO
