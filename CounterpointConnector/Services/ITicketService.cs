using CounterpointConnector.Models;

namespace CounterpointConnector.Services;

public interface ITicketService
{
    Task<TicketResponse> CreateAsync(TicketRequest request, CancellationToken ct);
}
