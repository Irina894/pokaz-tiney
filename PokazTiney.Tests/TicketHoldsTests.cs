using PokazTiney.Api.Controllers;

namespace PokazTiney.Tests;

public class TicketHoldsTests
{
    [Theory]
    [InlineData("1-1", 1, 1)]
    [InlineData("3-4", 3, 4)]
    [InlineData("5-8", 5, 8)]
    public void TryParseSeat_ValidCodes_ReturnsRowAndSeat(string code, int row, int seat)
    {
        Assert.True(TicketHolds.TryParseSeat(code, out var r, out var s));
        Assert.Equal(row, r);
        Assert.Equal(seat, s);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("1-2-3")]
    [InlineData("a-b")]
    [InlineData("0-1")]
    [InlineData("6-1")]
    [InlineData("1-0")]
    [InlineData("1-9")]
    public void TryParseSeat_InvalidCodes_ReturnsFalse(string? code)
    {
        Assert.False(TicketHolds.TryParseSeat(code, out _, out _));
    }
}
