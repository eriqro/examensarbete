using MySqlConnector;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

// Set up the web app builder
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Pull in local dev settings (won't exist in production, that's fine)
builder.Configuration.AddJsonFile(
    "appsettings.Development.Local.json",
    optional: true,
    reloadOnChange: true
);

// Wire up database connection
builder.Services.AddTransient<MySqlConnection>(_ =>
    new MySqlConnection(builder.Configuration.GetConnectionString("MariaDB")!)
);

// Enable Swagger for API testing
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JWT authentication setup
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection.GetValue<string>("Key")!;
var jwtIssuer = jwtSection.GetValue<string>("Issuer");
var jwtAudience = jwtSection.GetValue<string>("Audience");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // Turn this on for production
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// Allow frontend to call our API
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Swagger is dev-only
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Quick sanity check endpoint
app.MapGet("/", () => "Backend is running!");

app.UseCors();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Builds JWT tokens for logged in users
string GenerateJwtToken(int userId, string username)
{
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var expires = DateTime.UtcNow.AddMinutes(jwtSection.GetValue<int>("ExpiresMinutes"));

    var claims = new[] {
        new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
        new Claim("username", username)
    };

    var token = new JwtSecurityToken(
        issuer: jwtIssuer,
        audience: jwtAudience,
        claims: claims,
        expires: expires,
        signingCredentials: creds
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}

// Create new account
app.MapPost("/auth/register", async (MySqlConnection conn, RegisterRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password) || string.IsNullOrWhiteSpace(req.Email))
        return Results.BadRequest(new { error = "username, email and password are required" });

    await conn.OpenAsync();

    // See if username or email already exists
    using (var check = new MySqlCommand("SELECT COUNT(*) FROM users WHERE username = @u OR email = @e", conn))
    {
        check.Parameters.AddWithValue("@u", req.Username);
        check.Parameters.AddWithValue("@e", req.Email);
        var existing = Convert.ToInt32(await check.ExecuteScalarAsync());
        if (existing > 0)
            return Results.Conflict(new { error = "username or email already taken" });
    }

    var hashed = BCrypt.Net.BCrypt.HashPassword(req.Password);

    using (var insert = new MySqlCommand("INSERT INTO users (username, email, hashpassword) VALUES (@u, @e, @h); SELECT LAST_INSERT_ID();", conn))
    {
        insert.Parameters.AddWithValue("@u", req.Username);
        insert.Parameters.AddWithValue("@e", req.Email);
        insert.Parameters.AddWithValue("@h", hashed);
        var idObj = await insert.ExecuteScalarAsync();
        var id = Convert.ToInt32(idObj);

        var token = GenerateJwtToken(id, req.Username);
        return Results.Ok(new { token, userId = id, username = req.Username });
    }
});

// Login existing user
app.MapPost("/auth/login", async (MySqlConnection conn, LoginRequest req) =>
{
    await conn.OpenAsync();

    using var cmd = new MySqlCommand("SELECT userID, username, hashpassword FROM users WHERE username = @u OR email = @u LIMIT 1", conn);
    cmd.Parameters.AddWithValue("@u", req.UsernameOrEmail);
    using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
        return Results.Unauthorized();

    var id = reader.GetInt32("userID");
    var username = reader.GetString("username");
    var storedHash = reader.GetString("hashpassword");

    if (!BCrypt.Net.BCrypt.Verify(req.Password, storedHash))
        return Results.Unauthorized();

    var token = GenerateJwtToken(id, username);
    return Results.Ok(new { token, userId = id, username });
});

// Get current logged in user info
app.MapGet("/auth/me", async (HttpContext http, MySqlConnection conn) =>
{
    var userIdClaim = http.User.FindFirst(JwtRegisteredClaimNames.Sub);
    if (userIdClaim == null)
        return Results.Unauthorized();

    if (!int.TryParse(userIdClaim.Value, out var userId))
        return Results.Unauthorized();

    await conn.OpenAsync();
    using var cmd = new MySqlCommand("SELECT userID, username, email FROM users WHERE userID = @id", conn);
    cmd.Parameters.AddWithValue("@id", userId);
    using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
        return Results.NotFound();

    return Results.Ok(new { userID = reader.GetInt32("userID"), username = reader.GetString("username"), email = reader.GetString("email") });
}).RequireAuthorization();

// List all users (probably for admin purposes)
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


// Fetch all songs from the database

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

// Data transfer objects for API
public record RegisterRequest(string Username, string Email, string Password);
public record LoginRequest(string UsernameOrEmail, string Password);
