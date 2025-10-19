using CounterpointConnector.Data;
using CounterpointConnector.Models;

namespace CounterpointConnector.Services;

public sealed class TicketService : ITicketService
{
    private readonly ITicketRepository _repo;

    public TicketService(ITicketRepository repo) => _repo = repo;

    public async Task<TicketResponse> CreateAsync(TicketRequest request, CancellationToken ct)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (request.Lines is null || request.Lines.Count == 0)
            throw new BadHttpRequestException("At least one line is required.", statusCode: 400);

        foreach (var line in request.Lines)
        {
            if (string.IsNullOrWhiteSpace(line.Sku))
                throw new BadHttpRequestException("SKU is required.", 400);
            if (line.Qty <= 0)
                throw new BadHttpRequestException("Qty must be > 0.", 400);
            if (line.Sku.Length > 40)
                throw new BadHttpRequestException("SKU too long (max 40).", 400);
        }

        return await _repo.CreateTicketAsync(request.CustomerAccountNo, request.Lines, ct);
    }
}
