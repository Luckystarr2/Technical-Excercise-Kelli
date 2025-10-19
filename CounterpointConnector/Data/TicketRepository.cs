using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Dapper;
using CounterpointConnector.Models;

namespace CounterpointConnector.Data;

public sealed class TicketRepository : ITicketRepository
{
    private readonly string _connStr;

    public TicketRepository(IConfiguration config)
    {
        _connStr = config.GetConnectionString("KgsDemo")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:KgsDemo");
    }

    public async Task<TicketResponse> CreateTicketAsync(string? customerAccountNo, IEnumerable<TicketLineDto> lines, CancellationToken ct)
    {
        var tvp = lines.ToTicketLineInputTvp();

        await using var conn = new SqlConnection(_connStr);
        await conn.OpenAsync(ct);

        var cmd = new SqlCommand("dbo.usp_CreateTicket", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        var p1 = new SqlParameter("@CustomerAccountNo", SqlDbType.NVarChar, 40)
        {
            Value = (object?)customerAccountNo ?? DBNull.Value
        };
        var p2 = new SqlParameter("@Lines", SqlDbType.Structured)
        {
            TypeName = "dbo.TicketLineInput",
            Value = tvp
        };

        cmd.Parameters.Add(p1);
        cmd.Parameters.Add(p2);

        using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            throw new InvalidOperationException("Stored procedure returned no rows.");

        var resp = new TicketResponse
        {
            TicketID = reader.GetInt64(reader.GetOrdinal("TicketID")),
            Subtotal = reader.GetDecimal(reader.GetOrdinal("Subtotal")),
            TaxAmount = reader.GetDecimal(reader.GetOrdinal("TaxAmount")),
            Total = reader.GetDecimal(reader.GetOrdinal("Total"))
        };

        return resp;
    }
}
