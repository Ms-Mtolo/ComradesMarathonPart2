using ComradesMarathonAPI.Data;
using ComradesMarathonAPI.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


// Database

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")
        ?? "Server=.\\SQLEXPRESS;Database=ComradesMarathonDb;Integrated Security=True;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;"));


// Session

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


// Seed roles on startup

// using Microsoft.EntityFrameworkCore;
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate(); // applies pending migrations and creates schema if needed

    if (!db.Roles.Any())
    {
        db.Roles.AddRange(
            new Role { RoleName = "Admin", Description = "Full system access" },
            new Role { RoleName = "Organiser", Description = "Can create and manage events" },
            new Role { RoleName = "Participant", Description = "Can enroll and view results" });
        db.SaveChanges();
    }
}


// Pipeline

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSession();

// Build a ClaimsPrincipal from session on every request
app.Use(async (context, next) =>
{
    var userId = context.Session.GetInt32("UserId");
    var role = context.Session.GetString("Role");

    if (userId.HasValue && !string.IsNullOrEmpty(role))
    {
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, userId.Value.ToString()),
            new(System.Security.Claims.ClaimTypes.Role, role)
        };
        context.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(claims, "SessionAuth"));
    }

    await next();
});

app.UseAuthorization();
app.MapControllers();
app.Run();


public partial class Program { }