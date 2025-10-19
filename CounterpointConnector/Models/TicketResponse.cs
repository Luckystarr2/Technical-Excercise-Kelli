namespace CounterpointConnector.Models;

public sealed class TicketResponse
{
    public long TicketID { get; init; }
    public decimal Subtotal { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal Total { get; init; }
}
