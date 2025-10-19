namespace CounterpointConnector.Models;

public sealed class TicketRequest
{
    public string? CustomerAccountNo { get; init; }
    public List<TicketLineDto> Lines { get; init; } = new();
}
