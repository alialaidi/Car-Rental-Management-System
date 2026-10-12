CREATE OR ALTER PROCEDURE dbo.usp_Payments_Create
    @ContractId INT, @PaymentMethod NVARCHAR(20), @Amount DECIMAL(18,2), @TransactionReference NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Total DECIMAL(18,2), @Status NVARCHAR(20), @Paid DECIMAL(18,2);
    SELECT @Total = TotalAmount, @Status = Status
    FROM dbo.RentalContracts WITH (UPDLOCK, HOLDLOCK) WHERE ContractId = @ContractId;
    IF @Total IS NULL THROW 50040, 'Contract not found.', 1;
    IF @Status = 'Cancelled' THROW 50002, 'Payments cannot be recorded for a cancelled contract.', 1;
    SELECT @Paid = COALESCE(SUM(Amount), 0) FROM dbo.Payments WHERE ContractId = @ContractId AND PaymentStatus IN ('Paid','Pending');
    IF @Amount <= 0 OR @Paid + @Amount > @Total THROW 50003, 'Payment exceeds outstanding balance.', 1;
    INSERT dbo.Payments(ContractId, PaymentMethod, Amount, TransactionReference)
    VALUES (@ContractId, @PaymentMethod, @Amount, @TransactionReference);
    SELECT CONVERT(INT, SCOPE_IDENTITY());
END;
GO
