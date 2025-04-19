using TodoApi.Models;
using TodoApi.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

IdentityModelEventSource.ShowPII = true;

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token like: Bearer {your token}"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var secretKey = builder.Configuration["Jwt:Key"] ?? Environment.GetEnvironmentVariable("Jwt_SECRET") 
    ?? throw new InvalidOperationException("JWT secret key not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secretKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddLogging();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5206") // Replace with your Blazor Web App port if different
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Unhandled exception");

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var errorResponse = new { error = "An unexpected error occurred." };
        await context.Response.WriteAsJsonAsync(errorResponse);
    }
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Get All
app.MapGet("/todo", async (TodoDbContext db, ILogger<Program> logger) =>
{
    logger.LogInformation("GET /todo called");
    var allTodos = await db.Todos.ToListAsync();
    return Results.Ok(allTodos);
}).RequireAuthorization();

// Get by Id
app.MapGet("/todo/{id}", async (int id, TodoDbContext db, ILogger<Program> logger) =>
{
    logger.LogInformation("GET /todo/{id} called with id={Id}", id, id);
    var item = await db.Todos.FindAsync(id);
    return item != null ? Results.Ok(item) : Results.NotFound();
}).RequireAuthorization();

// Create new item
app.MapPost("/todo", async (TodoCreateRequest newItem, TodoDbContext db, ILogger<Program> logger) =>
{
    logger.LogInformation("POST /todo called with title={Title}", newItem.Title);
    if (string.IsNullOrWhiteSpace(newItem.Title))
    {
        return Results.BadRequest("Title is required.");
    }
    var item = new TodoItem
    {
        Title = newItem.Title,
        IsComplete = newItem.IsComplete,
    };
    db.Todos.Add(item);
    await db.SaveChangesAsync();

    return Results.Created($"/todo/{item.Id}", item);
}).RequireAuthorization();

// Update item
app.MapPut("/todo/{id}", async (int id, TodoItem updatedItem, TodoDbContext db, ILogger<Program> logger) =>
{
    logger.LogInformation("PUT /todo/{id} called with id={Id}", id, id);
    var item = await db.Todos.FindAsync(id);
    if (item is null) return Results.NotFound();
    
    if (string.IsNullOrWhiteSpace(updatedItem.Title))
    {
        return Results.BadRequest("Title is required.");
    }

    item.Title = updatedItem.Title;
    item.IsComplete = updatedItem.IsComplete;
    await db.SaveChangesAsync();

    return Results.NoContent();
}).RequireAuthorization();

// Delete item
app.MapDelete("/todo/{id}", async (int id, TodoDbContext db, ILogger<Program> logger) =>
{
    logger.LogInformation("DELETE /todo/{id} called with id={Id}", id, id);
    var item = await db.Todos.FindAsync(id);
    if (item is null) return Results.NotFound();

    db.Todos.Remove(item);
    await db.SaveChangesAsync();

    return Results.NoContent();
}).RequireAuthorization();

// Register user
app.MapPost("/register", async (RegisterRequest request, TodoDbContext db, ILogger<Program> logger) =>
{
    logger.LogInformation("POST /register called with username={Username}", request.Username);
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest("Username and password are required.");
    }
    if (await db.Users.AnyAsync(u => u.Username == request.Username))
        return Results.BadRequest("Username already exists.");
    
    var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

    var user = new User
    {
        Username = request.Username,
        PasswordHash = passwordHash,
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Ok("User registered successfully");
});

// Login
app.MapPost("/login", async (HttpContext context, LoginRequest request, TodoDbContext db, ILogger<Program> logger) =>
{
    logger.LogInformation("POST /login called with username={Username}", request.Username);
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest("Username and password are required.");
    }
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
    if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
    {
        return Results.Unauthorized();
    }

    // Create JWT token
    var claims = new[] { new Claim(ClaimTypes.Name, user.Username ?? string.Empty) };
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
        claims: claims,
        expires: DateTime.Now.AddHours(1),
        signingCredentials: creds);

    var jwt = new JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(new { token = jwt });
});

app.Run();