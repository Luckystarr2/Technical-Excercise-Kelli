namespace CounterpointConnector.Models;

public sealed class TicketLineDto
{
    public string Sku { get; init; } = default!;
    public int Qty { get; init; }
    public decimal? OverridePrice { get; init; }
}
