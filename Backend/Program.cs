using MySqlConnector;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Build the WebApplication
var builder = WebApplication.CreateBuilder(args);

// Load local development secrets (overrides appsettings)
builder.Configuration.AddJsonFile(
    "appsettings.Development.Local.json",
    optional: true,
    reloadOnChange: true
);

// Register MySqlConnection as transient service
builder.Services.AddTransient<MySqlConnection>(_ =>
    new MySqlConnection(builder.Configuration.GetConnectionString("MariaDB")!)
);

// Add Swagger/OpenAPI for testing
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Enable Swagger only in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Test route
app.MapGet("/", () => "Backend is running!");

// Get all users
app.MapGet("/users", async (MySqlConnection conn) =>
{
    await conn.OpenAsync();

    var users = new List<object>();

    using var cmd = new MySqlCommand("SELECT userID, username, email FROM users", conn);
    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        users.Add(new
        {
            UserID = reader.GetInt32("userID"),
            Username = reader.GetString("username"),
            Email = reader.GetString("email")
        });
    }

    return users;
});


// Get all songs/mp3s

app.MapGet("/songs", async (MySqlConnection conn) =>
{
    await conn.OpenAsync();

    var songs = new List<object>();

    using var cmd = new MySqlCommand(
        "SELECT NameOfSong, length, thumbnail, creatorUserID FROM mp3s",
        conn
    );
    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        songs.Add(new
        {
            NameOfSong = reader.GetString("NameOfSong"),
            Length = reader.GetTimeSpan("length"),
            Thumbnail = reader["thumbnail"] == DBNull.Value ? null : reader["thumbnail"],
            CreatorUserID = reader.GetInt32("creatorUserID")
        });
    }

    return songs;
});


app.Run();
