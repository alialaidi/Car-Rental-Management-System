CREATE OR ALTER PROCEDURE dbo.usp_Payments_GetBalance @ContractId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.ContractId, c.TotalAmount,
        COALESCE(SUM(CASE WHEN p.PaymentStatus IN ('Paid','Refunded') THEN p.Amount ELSE 0 END), 0) AS PaidAmount,
        COALESCE(SUM(CASE WHEN p.PaymentStatus = 'Refunded' THEN p.Amount ELSE 0 END), 0) AS RefundedAmount
    FROM dbo.RentalContracts c LEFT JOIN dbo.Payments p ON p.ContractId = c.ContractId
    WHERE c.ContractId = @ContractId GROUP BY c.ContractId, c.TotalAmount;
END;
GO
