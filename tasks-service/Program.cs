using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHttpClient();

var app = builder.Build();

var usersUrl = builder.Configuration["Services:Users"] ?? "http://users-service:8080";
var notificationsUrl = builder.Configuration["Services:Notifications"] ?? "http://notifications-service:8080";

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var retries = 30;
    while (retries > 0)
    {
        try
        {
            if (await db.Database.CanConnectAsync())
            {
                await db.Database.ExecuteSqlRawAsync(
                    "CREATE TABLE IF NOT EXISTS \"Tasks\" (\"Id\" SERIAL PRIMARY KEY, \"Title\" TEXT NOT NULL, \"Completed\" BOOLEAN NOT NULL DEFAULT FALSE, \"UserId\" INTEGER NOT NULL);");
                break;
            }
        }
        catch
        {
        }

        retries--;
        await Task.Delay(2000);
    }
}

app.MapGet("/health", () => Results.Ok(new { status = "OK", service = "tasks" }));

app.MapGet("/tasks", async (AppDbContext db) =>
    await db.Tasks.OrderBy(t => t.Id).ToListAsync());

app.MapGet("/tasks/{id:int}", async (int id, AppDbContext db) =>
{
    var task = await db.Tasks.FindAsync(id);
    return task is null ? Results.NotFound() : Results.Ok(task);
});

app.MapPost("/tasks", async (TaskItem task, AppDbContext db, IHttpClientFactory httpFactory, ILogger<Program> logger) =>
{
    var http = httpFactory.CreateClient();

    var userResponse = await http.GetAsync($"{usersUrl}/users/{task.UserId}");
    if (!userResponse.IsSuccessStatusCode)
        return Results.BadRequest(new { error = $"Пользователь {task.UserId} не найден" });

    task.Id = 0;
    db.Tasks.Add(task);
    await db.SaveChangesAsync();
    logger.LogInformation("Создана задача {Id} для пользователя {UserId}", task.Id, task.UserId);

    try
    {
        await http.PostAsJsonAsync($"{notificationsUrl}/notifications",
            new { message = $"Создана задача '{task.Title}' для пользователя {task.UserId}" });
    }
    catch (Exception ex)
    {
        logger.LogWarning("Уведомление не отправлено: {Message}", ex.Message);
    }

    return Results.Created($"/tasks/{task.Id}", task);
});

app.Run();

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();
}

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public bool Completed { get; set; }
    public int UserId { get; set; }
}
