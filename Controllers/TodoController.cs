using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TodoApp.Data;
using TodoApp.Models;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TodoController : ControllerBase
{
    private readonly AppDbContext _context;

    public TodoController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetTodos()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.Name)!.Value);
        var todos = await _context.Todos.Where(t => t.UserId == userId).ToListAsync();
        return Ok(todos);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTodo(Todo todo)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.Name)!.Value);
        todo.UserId = userId;
        _context.Todos.Add(todo);
        await _context.SaveChangesAsync();
        return Ok(todo);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTodo(int id, Todo todo)
    {
        var existingTodo = await _context.Todos.FindAsync(id);
        if (existingTodo == null) return NotFound();

        var userId = int.Parse(User.FindFirst(ClaimTypes.Name)!.Value);
        if (existingTodo.UserId != userId) return Unauthorized();

        existingTodo.Title = todo.Title;
        existingTodo.Description = todo.Description;
        await _context.SaveChangesAsync();

        return Ok(existingTodo);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTodo(int id)
    {
        var existingTodo = await _context.Todos.FindAsync(id);
        if (existingTodo == null) return NotFound();

        var userId = int.Parse(User.FindFirst(ClaimTypes.Name)!.Value);
        if (existingTodo.UserId != userId) return Unauthorized();

        _context.Todos.Remove(existingTodo);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
