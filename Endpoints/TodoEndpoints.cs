using Microsoft.EntityFrameworkCore;
using ZUMI_Backend.Data;
using ZUMI_Backend.Models;
using ZUMI_Backend.Models.DTOs;
using AutoMapper;

namespace ZUMI_Backend.Endpoints;

public static class TodoEndpoints
{
    public static void MapTodoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // POST /api/v1/todos/create
        endpoints.MapPost("/todos/create", async (Todo newTodo, ApplicationDbContext db) =>
        {
            db.Todos.Add(newTodo);
            await db.SaveChangesAsync();
            return Results.Created($"/api/v1/todos/{newTodo.Id}", newTodo);
        })
        .RequireAuthorization()
        .WithName("TodoCreate")
        .WithOpenApi();

        // GET /api/v1/todos/{id}
        endpoints.MapGet("/todos/{id:guid}", async (Guid id, ApplicationDbContext db, IMapper mapper) =>
        {
            var todo = await db.Todos.FindAsync(id);
            return todo != null ? Results.Ok(mapper.Map<TodoDto>(todo)) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithName("TodoRetrieve")
        .WithOpenApi();

        // PUT /api/v1/todos/{id}/update
        endpoints.MapPut("/todos/{id:guid}/update", async (Guid id, Todo updated, ApplicationDbContext db) =>
        {
            var existing = await db.Todos.FindAsync(id);
            if (existing == null) return Results.NotFound();
            // Update Properties hier ergänzen
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("TodoUpdate")
        .WithOpenApi();

        // DELETE /api/v1/todos/{id}/delete
        endpoints.MapDelete("/todos/{id:guid}/delete", async (Guid id, ApplicationDbContext db) =>
        {
            var todo = await db.Todos.FindAsync(id);
            if (todo == null) return Results.NotFound();
            db.Todos.Remove(todo);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("TodoDelete")
        .WithOpenApi();

        // GET /api/v1/projekte/{projekt_id}/todos - Todos für Projekt
        endpoints.MapGet("/projekte/{projekt_id:guid}/todos", async (Guid projekt_id, ApplicationDbContext db, IMapper mapper) =>
        {
            var todos = await db.Todos.Where(t => t.ProjektId == projekt_id).ToListAsync();
            return mapper.Map<List<TodoDto>>(todos);
        })
        .RequireAuthorization()
        .WithName("ProjektTodosList")
        .WithOpenApi();
    }
}