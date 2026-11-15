using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PokazTiney.Api.Data;
using PokazTiney.Api.Models;

namespace PokazTiney.Tests;

/// <summary>Тест виконується лише якщо задано змінну середовища TEST_CONNECTION_STRING (окрема порожня БД SQL Server).</summary>
public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING")))
            Skip = "Задайте TEST_CONNECTION_STRING (окрема тестова БД SQL Server), щоб запустити інтеграційні тести.";
    }
}

public class ApiFactory : WebApplicationFactory<Program>
{
    public ApiFactory()
    {
        // змінна середовища читається конфігурацією застосунку (ConnectionStrings:Default)
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__Default",
            Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING"));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Development");
}

public class ConcurrentBookingTests
{
    // NFR-PERF-5, TS-03: 100 паралельних запитів на одне місце одного сеансу, рівно один успішний
    [SqlServerFact]
    public async Task HundredParallelRequests_ForSameSeat_ExactlyOneSucceeds()
    {
        const int requests = 100;
        const string seat = "1-1";

        using var factory = new ApiFactory();
        using var warmup = factory.CreateClient();

        int sessionId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();

            var movie = new Movie { Title = "Тестовий фільм", Description = "Тест", Genre = "Тест", DurationMinutes = 90 };
            db.Movies.Add(movie);
            await db.SaveChangesAsync();

            var session = new Session { MovieId = movie.Id, StartTime = DateTime.Now.AddDays(2), Hall = "Зал тест", Price = 100m };
            db.Sessions.Add(session);
            await db.SaveChangesAsync();
            sessionId = session.Id;
        }

        var start = new TaskCompletionSource();
        var tasks = Enumerable.Range(0, requests).Select(async i =>
        {
            using var client = factory.CreateClient();
            await start.Task;
            var response = await client.PostAsJsonAsync("/api/orders", new
            {
                sessionId,
                seats = new[] { seat },
                email = $"tester{i}@example.com",
                phone = "+380501234567"
            });
            return response.StatusCode;
        }).ToList();

        start.SetResult();
        var codes = await Task.WhenAll(tasks);

        Assert.Equal(1, codes.Count(c => c == HttpStatusCode.OK));
        Assert.Equal(requests - 1, codes.Count(c => c == HttpStatusCode.Conflict));

        using var checkScope = factory.Services.CreateScope();
        var check = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var active = await check.Tickets.CountAsync(t =>
            t.SessionId == sessionId && t.SeatNumber == seat && t.Status != TicketStatus.Cancelled);
        Assert.Equal(1, active);
    }
}
