namespace LivestockManager.Domain.Enums;

public enum InvoiceStatus
{
    Draft = 1,
    Confirmed = 2,
    Unpaid = 3,
    PartiallyPaid = 4,
    Paid = 5,
    Cancelled = 6,
    Voided = 7,
    Overdue = 8
}
