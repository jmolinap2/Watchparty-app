using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using WatchParty.Api.Auditing;
using WatchParty.Api.Middleware;
using WatchParty.Api.Realtime;
using WatchParty.Application;
using WatchParty.Application.Abstractions.Realtime;
using WatchParty.Infrastructure;
using WatchParty.Infrastructure.Persistence;
using WatchParty.Infrastructure.Security;
using WatchParty.Observability.Logging;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddWatchPartyFileLogger(builder.Configuration, "WatchParty.Api", "watchparty-api.log");

try
{
    var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
        ?? throw new InvalidOperationException("Jwt configuration is required.");
    if (string.IsNullOrWhiteSpace(jwtOptions.Key) || Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32)
    {
        throw new InvalidOperationException("Jwt:Key must be configured with at least 32 bytes.");
    }

    var postgresConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
        ?? builder.Configuration["Redis:ConnectionString"]
        ?? "localhost:6379";

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddOpenApi();
    builder.Services.AddHttpContextAccessor();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("WatchPartyClients", policy =>
        {
            var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? ["http://localhost:3000", "http://localhost:19006", "http://localhost:8081"];
            var allowedOrigins = origins
                .Select(origin => origin.TrimEnd('/'))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var allowDevTunnels = builder.Configuration.GetValue("Cors:AllowDevTunnels", builder.Environment.IsDevelopment());

            policy
                .SetIsOriginAllowed(origin =>
                {
                    if (allowedOrigins.Contains(origin.TrimEnd('/')))
                    {
                        return true;
                    }

                    return allowDevTunnels
                        && Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                        && string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                        && uri.Host.EndsWith(".devtunnels.ms", StringComparison.OrdinalIgnoreCase);
                })
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrWhiteSpace(accessToken) && path.StartsWithSegments("/hubs/room"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    builder.Services.AddSignalR();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddInfrastructureRuntime(builder.Configuration);
    builder.Services.AddScoped<IRoomRealtimeNotifier, RoomRealtimeNotifier>();
    builder.Services.AddHostedService<PresenceSweeper>();

    builder.Services
        .AddHealthChecks()
        .AddNpgSql(postgresConnectionString, name: "postgres")
        .AddRedis(redisConnectionString, name: "redis");

    var app = builder.Build();

    app.Logger.LogInformation(
        "WatchParty API starting. Environment={Environment}; LogFile={LogFile}; Postgres={Postgres}; Redis={Redis}.",
        app.Environment.EnvironmentName,
        WatchPartyLogPaths.ResolveLogFilePath(app.Configuration, "watchparty-api.log"),
        RedactConnectionString(postgresConnectionString),
        redisConnectionString);

    if (app.Environment.IsDevelopment())
    {
        app.MapGet("/", () => Results.Redirect("/swagger"));
        app.MapOpenApi();
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseWatchPartyExceptionHandling();
    app.UseHttpsRedirection();

    app.UseCors("WatchPartyClients");
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseWatchPartyRequestAuditing();

    app.MapControllers();
    app.MapHub<RoomHub>("/hubs/room");
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/ready");

    if (builder.Configuration.GetValue("Database:AutoMigrate", true))
    {
        var initLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        try
        {
            await DbInitializer.InitializeAsync(app.Services);
        }
        catch (Exception ex)
        {
            initLogger.LogCritical(ex,
                "Database initialization failed. Ensure PostgreSQL is running at {Connection}. " +
                "The API will start but all database operations will fail until the connection is restored.",
                RedactConnectionString(app.Configuration.GetConnectionString("DefaultConnection")));
        }
    }

    await app.RunAsync();
}
catch (Exception exception)
{
    WatchPartyStartupLogger.LogFatal(builder.Configuration, "WatchParty.Api", "watchparty-api.log", exception);
    throw;
}

static string? RedactConnectionString(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return connectionString;
    }

    var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    for (var index = 0; index < parts.Length; index++)
    {
        if (parts[index].StartsWith("Password=", StringComparison.OrdinalIgnoreCase)
            || parts[index].StartsWith("Pwd=", StringComparison.OrdinalIgnoreCase))
        {
            var key = parts[index].Split('=', 2)[0];
            parts[index] = $"{key}=***";
        }
    }

    return string.Join(';', parts);
}
