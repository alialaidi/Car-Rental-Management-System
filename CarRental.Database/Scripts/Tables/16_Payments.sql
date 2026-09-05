-- =====================================================================
-- Table: Payments
-- Purpose: Payments made against a rental contract (supports partial
--          payments, i.e. multiple rows per contract).
-- =====================================================================
IF OBJECT_ID('dbo.Payments', 'U') IS NOT NULL DROP TABLE dbo.Payments;
GO

CREATE TABLE dbo.Payments
(
    PaymentId               INT IDENTITY(1,1)  NOT NULL,
    ContractId               INT                NOT NULL,
    PaymentMethod             NVARCHAR(20)       NOT NULL,
    Amount                    DECIMAL(18,2)      NOT NULL,
    TransactionReference       NVARCHAR(100)      NULL,
    PaymentStatus              NVARCHAR(20)       NOT NULL CONSTRAINT DF_Payments_Status DEFAULT ('Pending'),
    PaymentDate                DATETIME2(3)       NOT NULL CONSTRAINT DF_Payments_PaymentDate DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Payments PRIMARY KEY CLUSTERED (PaymentId),
    CONSTRAINT FK_Payments_RentalContracts FOREIGN KEY (ContractId) REFERENCES dbo.RentalContracts (ContractId),
    CONSTRAINT CK_Payments_Method CHECK (PaymentMethod IN ('Cash','CreditCard','OnlinePayment','BankTransfer')),
    CONSTRAINT CK_Payments_Status CHECK (PaymentStatus IN ('Pending','Paid','Failed','Refunded')),
    CONSTRAINT CK_Payments_Amount CHECK (Amount >= 0)
);
GO

CREATE NONCLUSTERED INDEX IX_Payments_ContractId ON dbo.Payments (ContractId);
GO
