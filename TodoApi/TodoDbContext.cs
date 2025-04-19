using Microsoft.EntityFrameworkCore;
using TodoApi.Models;

public class TodoDbContext : DbContext
{
  public TodoDbContext(DbContextOptions<TodoDbContext> options)
    : base(options) { }

  public DbSet<TodoItem> Todos => Set<TodoItem>();
  public DbSet<User> Users => Set<User>();
}