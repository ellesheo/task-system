using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

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
                    "CREATE TABLE IF NOT EXISTS \"Users\" (\"Id\" SERIAL PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Email\" TEXT NOT NULL);");
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

app.MapGet("/health", () => Results.Ok(new { status = "OK", service = "users" }));

app.MapGet("/users", async (AppDbContext db) =>
    await db.Users.OrderBy(u => u.Id).ToListAsync());

app.MapGet("/users/{id:int}", async (int id, AppDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    return user is null ? Results.NotFound() : Results.Ok(user);
});

app.MapPost("/users", async (User user, AppDbContext db, ILogger<Program> logger) =>
{
    user.Id = 0;
    db.Users.Add(user);
    await db.SaveChangesAsync();
    logger.LogInformation("Создан пользователь {Id} {Name}", user.Id, user.Name);
    return Results.Created($"/users/{user.Id}", user);
});

app.Run();

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}
