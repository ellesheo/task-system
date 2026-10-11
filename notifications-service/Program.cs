var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "OK", service = "notifications" }));

app.MapPost("/notifications", (NotificationMessage message, ILogger<Program> logger) =>
{
    logger.LogInformation("УВЕДОМЛЕНИЕ: {Message}", message.Message);
    return Results.Ok(new { delivered = true });
});

app.Run();

public record NotificationMessage(string Message);
