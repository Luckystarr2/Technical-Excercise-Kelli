using System.Threading;
using System.Threading.Tasks;
using CounterpointConnector.Models;

namespace CounterpointConnector.Data;

public interface ITicketRepository
{
    Task<TicketResponse> CreateTicketAsync(string? customerAccountNo, IEnumerable<TicketLineDto> lines, CancellationToken ct);
}
