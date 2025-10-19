using System.Data;
using System.Diagnostics;
using CounterpointConnector.Data;
using CounterpointConnector.Models;
using CounterpointConnector.Services;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ITicketRepository, TicketRepository>();
builder.Services.AddSingleton<ITicketService, TicketService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/api/tickets", async (TicketRequest req, ITicketService svc, ILoggerFactory lf, CancellationToken ct) =>
{
    var log = lf.CreateLogger("Tickets");
    var reqId = Activity.Current?.Id ?? Guid.NewGuid().ToString("n");

    try
    {
        var result = await svc.CreateAsync(req, ct);
        log.LogInformation("RequestId={RequestId} Created ticket {TicketId}", reqId, result.TicketID);
        return Results.Ok(result);
    }
    catch (BadHttpRequestException ex)
    {
        log.LogWarning(ex, "RequestId={RequestId} Bad request", reqId);
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (SqlException ex)
    {
        // Map your SQL THROW error numbers to HTTP
        var http = ex.Number switch
        {
            50003 => 409, // insufficient inventory
            50002 => 404, // customer not found
            50005 => 400, // sku missing
            50004 => 400, // duplicate sku in TVP
            _ => 500
        };
        log.LogWarning(ex, "RequestId={RequestId} SQL error {SqlNumber}", reqId, ex.Number);
        return Results.Problem(
            statusCode: http,
            title: "SQL error",
            detail: $"{ex.Message} (SQL #{ex.Number})");
    }
    catch (Exception ex)
    {
        log.LogError(ex, "RequestId={RequestId} Unhandled error", reqId);
        return Results.Problem(statusCode: 500, title: "Unexpected error", detail: ex.ToString());
    }
});

app.MapGet("/", () => Results.Redirect("/swagger", permanent: false));

app.Run();
