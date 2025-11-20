using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using MinimalApi.DapperDemo.Models; 

var builder = WebApplication.CreateBuilder(args);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registrar IDbConnection como Scoped
builder.Services.AddScoped<IDbConnection>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConnection")
                          ?? throw new InvalidOperationException("Missing connection string 'DefaultConnection'");
    return new SqlConnection(connectionString);
});
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
//Los endopoints 
// GET /todos - obtener todos
app.MapGet("/todos", async (IDbConnection db) =>
{
    const string sql = @"SELECT Id, Title, IsDone, CreatedAt FROM Todos ORDER BY CreatedAt DESC;";
    var todos = await db.QueryAsync<Todo>(sql);
    return Results.Ok(todos);
});

// GET /todos/{id} - obtener uno
app.MapGet("/todos/{id:int}", async (int id, IDbConnection db) =>
{
    const string sql = @"SELECT Id, Title, IsDone, CreatedAt FROM Todos WHERE Id = @Id;";
    var todo = await db.QueryFirstOrDefaultAsync<Todo>(sql, new { Id = id });

    return todo is null ? Results.NotFound() : Results.Ok(todo);
});

// POST /todos - crear
app.MapPost("/todos", async (TodoCreateRequest request, IDbConnection db) =>
{
    // Validación básica
    if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200)
        return Results.BadRequest("Title is required and must be <= 200 characters.");

    const string sql = @"
        INSERT INTO Todos (Title, IsDone, CreatedAt)
        VALUES (@Title, 0, SYSDATETIME());
        SELECT CAST(SCOPE_IDENTITY() as int);";

    var newId = await db.ExecuteScalarAsync<int>(sql, new { request.Title });

    var createdSql = @"SELECT Id, Title, IsDone, CreatedAt FROM Todos WHERE Id = @Id;";
    var created = await db.QueryFirstAsync<Todo>(createdSql, new { Id = newId });

    return Results.Created($"/todos/{created.Id}", created);
});

// PUT /todos/{id} - actualizar
app.MapPut("/todos/{id:int}", async (int id, TodoUpdateRequest request, IDbConnection db) =>
{
    // Validación
    if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200)
        return Results.BadRequest("Title is required and must be <= 200 characters.");

    const string existsSql = @"SELECT COUNT(1) FROM Todos WHERE Id = @Id;";
    var exists = await db.ExecuteScalarAsync<int>(existsSql, new { Id = id });
    if (exists == 0)
        return Results.NotFound();

    const string updateSql = @"
        UPDATE Todos
        SET Title = @Title,
            IsDone = @IsDone
        WHERE Id = @Id;";

    await db.ExecuteAsync(updateSql, new
    {
        Id = id,
        request.Title,
        request.IsDone
    });

    var updatedSql = @"SELECT Id, Title, IsDone, CreatedAt FROM Todos WHERE Id = @Id;";
    var updated = await db.QueryFirstAsync<Todo>(updatedSql, new { Id = id });

    return Results.Ok(updated);
});

// DELETE /todos/{id} - eliminar
app.MapDelete("/todos/{id:int}", async (int id, IDbConnection db) =>
{
    const string sql = @"DELETE FROM Todos WHERE Id = @Id;";
    var rows = await db.ExecuteAsync(sql, new { Id = id });

    return rows == 0 ? Results.NotFound() : Results.NoContent();
});


app.Run();
