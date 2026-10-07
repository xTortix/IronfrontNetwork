using Ironfront.AuthService.Persistence;
using Ironfront.AuthService.Persistence.Entities;
using Ironfront.AuthService.Services;
using Ironfront.Shared.Constants;
using Ironfront.Shared.Contracts;
using Ironfront.Shared.Contracts.Auth;
using Microsoft.EntityFrameworkCore;
using System.Net;



var builder = WebApplication.CreateBuilder(args);

string? authDbConnection =
    Environment.GetEnvironmentVariable("IRONFRONT_AUTH_DB_CONNECTION");

if (string.IsNullOrWhiteSpace(authDbConnection))
{
    throw new InvalidOperationException(
        "Missing environment variable IRONFRONT_AUTH_DB_CONNECTION.");
}

builder.Services.AddDbContext<AuthDbContext>(options =>
{
    options.UseNpgsql(authDbConnection);
});

builder.Services.AddSingleton<PasswordHashService>();

builder.Services.AddSingleton<JwtTokenService>();

builder.WebHost.ConfigureKestrel(options =>
{
    int port = 5010;

    string? configuredPort = Environment.GetEnvironmentVariable("IRONFRONT_AUTHSERVICE_PORT");

    if (int.TryParse(configuredPort, out int parsedPort))
    {
        port = parsedPort;
    }

    // Internal service only.
    // Do not expose this directly to the internet.
    options.Listen(IPAddress.Loopback, port);
});

var app = builder.Build();

app.MapGet("/internal/health", () =>
{
    var status = new ServiceStatusDto
    {
        ServiceName = ServiceNames.AuthService,
        Status = "Online",
        ServerTimeUtc = DateTime.UtcNow,
        Environment = app.Environment.EnvironmentName
    };

    return Results.Ok(ApiResponse<ServiceStatusDto>.Ok(status));
});

app.MapGet("/internal/db-health", async (AuthDbContext dbContext) =>
{
    bool canConnect = await dbContext.Database.CanConnectAsync();

    var result = new
    {
        serviceName = ServiceNames.AuthService,
        database = "PostgreSQL",
        reachable = canConnect,
        serverTimeUtc = DateTime.UtcNow,
        environment = app.Environment.EnvironmentName
    };

    return Results.Ok(ApiResponse<object>.Ok(result));
});

app.MapPost("/internal/auth/register", async (
    RegisterRequest request,
    AuthDbContext dbContext,
    PasswordHashService passwordHashService) =>
{
    string username = request.Username.Trim();
    string email = request.Email.Trim();
    string password = request.Password;

    if (username.Length < 3 || username.Length > 32)
    {
        return Results.BadRequest(ApiResponse<RegisterResponse>.Fail(
            ErrorCodes.ValidationFailed,
            "Username must be between 3 and 32 characters."));
    }

    if (email.Length < 5 || email.Length > 254 || !email.Contains('@'))
    {
        return Results.BadRequest(ApiResponse<RegisterResponse>.Fail(
            ErrorCodes.ValidationFailed,
            "Email is invalid."));
    }

    if (password.Length < 8 || password.Length > 128)
    {
        return Results.BadRequest(ApiResponse<RegisterResponse>.Fail(
            ErrorCodes.ValidationFailed,
            "Password must be between 8 and 128 characters."));
    }

    string normalizedUsername = username.ToUpperInvariant();
    string normalizedEmail = email.ToUpperInvariant();

    bool usernameExists = await dbContext.Users
        .AnyAsync(x => x.NormalizedUsername == normalizedUsername);

    if (usernameExists)
    {
        return Results.Conflict(ApiResponse<RegisterResponse>.Fail(
            ErrorCodes.UsernameAlreadyExists,
            "Username already exists."));
    }

    bool emailExists = await dbContext.Users
        .AnyAsync(x => x.NormalizedEmail == normalizedEmail);

    if (emailExists)
    {
        return Results.Conflict(ApiResponse<RegisterResponse>.Fail(
            ErrorCodes.EmailAlreadyExists,
            "Email already exists."));
    }

    var user = new UserEntity
    {
        Id = Guid.NewGuid(),
        Username = username,
        NormalizedUsername = normalizedUsername,
        Email = email,
        NormalizedEmail = normalizedEmail,
        PasswordHash = passwordHashService.HashPassword(password),
        CreatedAtUtc = DateTime.UtcNow
    };

    dbContext.Users.Add(user);
    await dbContext.SaveChangesAsync();

    var response = new RegisterResponse
    {
        UserId = user.Id,
        Username = user.Username,
        Email = user.Email,
        CreatedAtUtc = user.CreatedAtUtc
    };

    return Results.Ok(ApiResponse<RegisterResponse>.Ok(response));
});

app.MapPost("/internal/auth/login", async (
    LoginRequest request,
    AuthDbContext dbContext,
    PasswordHashService passwordHashService,
    JwtTokenService jwtTokenService) =>
{
    string email = request.Email.Trim();
    string password = request.Password;

    if (email.Length < 3 || email.Length > 254)
    {
        return Results.BadRequest(ApiResponse<LoginResponse>.Fail(
            ErrorCodes.ValidationFailed,
            "Username or email is invalid."));
    }

    if (password.Length < 8 || password.Length > 128)
    {
        return Results.BadRequest(ApiResponse<LoginResponse>.Fail(
            ErrorCodes.ValidationFailed,
            "Password is invalid."));
    }

    string normalizedEmail = request.Email.Trim().ToUpperInvariant();

    var user = await dbContext.Users
        .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail);

    if (user is null)
    {
        return Results.Json(
            ApiResponse<LoginResponse>.Fail(
                ErrorCodes.InvalidCredentials,
                "Invalid username/email or password."),
            statusCode: StatusCodes.Status401Unauthorized);
    }

    bool passwordValid = passwordHashService.VerifyPassword(
        password,
        user.PasswordHash);

    if (!passwordValid)
    {
        return Results.Json(
            ApiResponse<LoginResponse>.Fail(
                ErrorCodes.InvalidCredentials,
                "Invalid username/email or password."),
            statusCode: StatusCodes.Status401Unauthorized);
    }

    JwtTokenResult token = jwtTokenService.CreateAccessToken(user);

    var response = new LoginResponse
    {
        UserId = user.Id,
        Username = user.Username,
        Email = user.Email,
        CreatedAtUtc = user.CreatedAtUtc,
        AccessToken = token.AccessToken,
        AccessTokenExpiresAtUtc = token.ExpiresAtUtc
    };

    return Results.Ok(ApiResponse<LoginResponse>.Ok(response));
});

app.Run();