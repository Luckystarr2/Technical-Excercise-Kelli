using System.Threading;
using System.Threading.Tasks;
using CounterpointConnector.Data;
using CounterpointConnector.Models;
using CounterpointConnector.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace CounterpointConnector.Tests;

public class TicketServiceTests
{
    [Fact]
    public async Task CreateAsync_HappyPath_ReturnsResponse()
    {
        var request = new TicketRequest
        {
            CustomerAccountNo = "CUST1001",
            Lines = new()
            {
                new TicketLineDto { Sku = "SKU-100", Qty = 2 }
            }
        };

        var expected = new TicketResponse
        {
            TicketID = 123,
            Subtotal = 19.98m,
            TaxAmount = 1.65m,
            Total = 21.63m
        };

        var repo = new Mock<ITicketRepository>();
        repo.Setup(r => r.CreateTicketAsync(request.CustomerAccountNo, request.Lines, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var svc = new TicketService(repo.Object);

        var actual = await svc.CreateAsync(request, CancellationToken.None);

        Assert.Equal(expected.TicketID, actual.TicketID);
        Assert.Equal(expected.Subtotal, actual.Subtotal);
        Assert.Equal(expected.TaxAmount, actual.TaxAmount);
        Assert.Equal(expected.Total, actual.Total);

        repo.Verify(r => r.CreateTicketAsync("CUST1001",
                                             It.IsAny<IEnumerable<TicketLineDto>>(),
                                             It.IsAny<CancellationToken>()),
                    Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NoLines_ThrowsBadRequest400()
    {
        var request = new TicketRequest
        {
            CustomerAccountNo = "CUST1001",
            Lines = new()
        };

        var repo = new Mock<ITicketRepository>();
        var svc = new TicketService(repo.Object);

        var ex = await Assert.ThrowsAsync<BadHttpRequestException>(
            () => svc.CreateAsync(request, CancellationToken.None));

        Assert.Equal(400, ex.StatusCode);
        repo.Verify(r => r.CreateTicketAsync(It.IsAny<string?>(), It.IsAny<IEnumerable<TicketLineDto>>(), It.IsAny<CancellationToken>()),
                    Times.Never);
    }
}
