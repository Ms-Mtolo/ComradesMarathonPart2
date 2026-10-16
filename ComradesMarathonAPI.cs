using System.Security.Claims;
using ComradesMarathonAPI.Controllers;
using ComradesMarathonAPI.Data;
using ComradesMarathonAPI.DTOs;
using ComradesMarathonAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ComradesMarathonAPI.Tests;

public class EventsControllerTests
{
    
    // Helper: build an AppDbContext backed by EF Core InMemory
    
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    // Helper: build a controller with a fake logged-in user
    private static EventsController CreateController(AppDbContext db, int userId, string role)
    {
        var controller = new EventsController(db);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        return controller;
    }

    
    // GET /api/events
    
    [Fact]
    public async Task GetAll_ReturnsAllEvents()
    {
        using var db = CreateDb(nameof(GetAll_ReturnsAllEvents));
        db.Events.Add(new Event { EventId = 1, Title = "Comrades", OrganizerId = 1 });
        db.Events.Add(new Event { EventId = 2, Title = "Park Run", OrganizerId = 1 });
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId: 2, role: "Participant");

        var result = await controller.GetAll(null, null);

        var ok = Assert.IsType<OkObjectResult>(result);
        var events = Assert.IsAssignableFrom<IEnumerable<Event>>(ok.Value);
        Assert.Equal(2, events.Count());
    }

   
    // GET /api/events/{id} - success
    
    [Fact]
    public async Task GetById_ExistingEvent_ReturnsOk()
    {
        using var db = CreateDb(nameof(GetById_ExistingEvent_ReturnsOk));
        db.Events.Add(new Event { EventId = 1, Title = "Comrades", OrganizerId = 1 });
        await db.SaveChangesAsync();

        var controller = CreateController(db, 2, "Participant");

        var result = await controller.GetById(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        var ev = Assert.IsType<Event>(ok.Value);
        Assert.Equal("Comrades", ev.Title);
    }

    
    // GET /api/events/{id} — not found
   
    [Fact]
    public async Task GetById_NonExistingEvent_ReturnsNotFound()
    {
        using var db = CreateDb(nameof(GetById_NonExistingEvent_ReturnsNotFound));
        var controller = CreateController(db, 2, "Participant");

        var result = await controller.GetById(999);

        Assert.IsType<NotFoundResult>(result);
    }

    
    // POST /api/events — Organiser creates successfully
    
    [Fact]
    public async Task Create_AsOrganiser_ReturnsCreated()
    {
        using var db = CreateDb(nameof(Create_AsOrganiser_ReturnsCreated));
        var controller = CreateController(db, userId: 1, role: "Organiser");

        var dto = new EventDto
        {
            Title = "New Race",
            Description = "Fun run",
            Date = DateTime.UtcNow.AddDays(30),
            Location = "Cape Town",
            Status = "Open",
            EventType = "run"
        };

        var result = await controller.Create(dto);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var ev = Assert.IsType<Event>(created.Value);
        Assert.Equal("New Race", ev.Title);
        Assert.Equal(1, ev.OrganizerId);
    }

    
    // PUT /api/events/{id} — success
    
    [Fact]
    public async Task Update_AsOwner_ReturnsOk()
    {
        using var db = CreateDb(nameof(Update_AsOwner_ReturnsOk));
        db.Events.Add(new Event { EventId = 1, Title = "Old", OrganizerId = 1 });
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId: 1, role: "Organiser");
        var dto = new EventDto
        {
            Title = "Updated",
            Description = "Desc",
            Date = DateTime.UtcNow,
            Location = "Durban",
            Status = "Closed",
            EventType = "run"
        };

        var result = await controller.Update(1, dto);

        var ok = Assert.IsType<OkObjectResult>(result);
        var ev = Assert.IsType<Event>(ok.Value);
        Assert.Equal("Updated", ev.Title);
    }

    
    // PUT /api/events/{id} — not owner → Forbid
   
    [Fact]
    public async Task Update_AsDifferentOrganiser_ReturnsForbid()
    {
        using var db = CreateDb(nameof(Update_AsDifferentOrganiser_ReturnsForbid));
        db.Events.Add(new Event { EventId = 1, Title = "Old", OrganizerId = 1 });
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId: 2, role: "Organiser");
        var dto = new EventDto
        {
            Title = "Hacked",
            Description = "x",
            Date = DateTime.UtcNow,
            Location = "x",
            Status = "x",
            EventType = "run"
        };

        var result = await controller.Update(1, dto);

        Assert.IsType<ForbidResult>(result);
    }

    
    // DELETE /api/events/{id} — success
    
    [Fact]
    public async Task Delete_AsOwner_ReturnsNoContent()
    {
        using var db = CreateDb(nameof(Delete_AsOwner_ReturnsNoContent));
        db.Events.Add(new Event { EventId = 1, Title = "Delete Me", OrganizerId = 1 });
        await db.SaveChangesAsync();

        var controller = CreateController(db, userId: 1, role: "Organiser");

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
        Assert.Null(await db.Events.FindAsync(1));
    }

    
    // DELETE /api/events/{id} — non-existent → NotFound
    
    [Fact]
    public async Task Delete_NonExisting_ReturnsNotFound()
    {
        using var db = CreateDb(nameof(Delete_NonExisting_ReturnsNotFound));
        var controller = CreateController(db, userId: 1, role: "Organiser");

        var result = await controller.Delete(999);

        Assert.IsType<NotFoundResult>(result);
    }
}