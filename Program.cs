using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NovaWallet.Data;
using NovaWallet.Interfaces;
using NovaWallet.Middleware;
using NovaWallet.Repositories;
using NovaWallet.Services;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<NovaWalletDbContext>(opt =>
{
    opt.UseNpgsql(builder.Configuration.GetConnectionString("PostgresDatabase"));
});


var jwtKey = builder.Configuration["Jwt:Key"]
             ?? throw new InvalidOperationException("Jwt:Key is not configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "NovaWalletApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "NovaWalletClients";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("TransferPolicy", httpContext =>
    {
        var partitionKey = httpContext.User.FindFirst("sub")?.Value
                            ?? httpContext.Connection.RemoteIpAddress?.ToString()
                            ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";

        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "TooManyRequests",
            Detail = "Too many transfer attempts. Please slow down and try again shortly.",
            Type = "https://novawallet.firstbank.example/problems/rate-limit-exceeded",
            Instance = context.HttpContext.Request.Path
        };

        await context.HttpContext.Response.WriteAsJsonAsync(
            problem,
            cancellationToken);
    };
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();


builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<ITransferRepository, TransferRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<ITransferService, TransferService>();
builder.Services.AddScoped<IWalletService, WalletService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NovaWalletDbContext>();
    db.Database.Migrate();
}

app.UseExceptionHandler(_ => { });

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();


if (app.Environment.IsDevelopment())
{
    app.MapPost("/dev/token", (string customerId) =>
    {
        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
        );

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: new[]
            {
                new System.Security.Claims.Claim("sub", customerId)
            },
            expires: DateTime.UtcNow.AddHours(4),
            signingCredentials: credentials
        );

        return Results.Ok(new
        {
            token = tokenHandler.WriteToken(token)
        });
    });
}

app.Run();