CREATE OR ALTER PROCEDURE dbo.usp_Payments_UpdateStatus @PaymentId INT, @PaymentStatus NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ContractId INT, @Current NVARCHAR(20), @Amount DECIMAL(18,2), @Total DECIMAL(18,2), @ContractStatus NVARCHAR(20), @Paid DECIMAL(18,2);
    SELECT @ContractId = ContractId FROM dbo.Payments WHERE PaymentId = @PaymentId;
    IF @ContractId IS NULL THROW 50040, 'Payment not found.', 1;
    SELECT @Total = TotalAmount, @ContractStatus = Status FROM dbo.RentalContracts WITH (UPDLOCK, HOLDLOCK) WHERE ContractId = @ContractId;
    SELECT @Current = PaymentStatus, @Amount = Amount FROM dbo.Payments WITH (UPDLOCK, HOLDLOCK) WHERE PaymentId = @PaymentId;
    IF @ContractStatus = 'Cancelled' THROW 50002, 'Cancelled contracts cannot accept payment status changes.', 1;
    IF NOT ((@Current = 'Pending' AND @PaymentStatus IN ('Paid','Failed')) OR (@Current = 'Paid' AND @PaymentStatus = 'Refunded'))
        THROW 50002, 'Invalid payment status transition.', 1;
    IF @PaymentStatus = 'Paid'
    BEGIN
        SELECT @Paid = COALESCE(SUM(Amount), 0) FROM dbo.Payments
        WHERE ContractId = @ContractId AND PaymentId <> @PaymentId AND PaymentStatus IN ('Paid','Pending');
        IF @Paid + @Amount > @Total THROW 50003, 'Payment exceeds outstanding balance.', 1;
    END;
    UPDATE dbo.Payments SET PaymentStatus = @PaymentStatus WHERE PaymentId = @PaymentId;
END;
GO
