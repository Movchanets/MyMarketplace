using System.Text;
using API;
using Application;
using Application.Interfaces;
using Application.Mapping;
using Infrastructure.Entities.Identity;
using Domain.Interfaces.Repositories;
using Infrastructure;
using Infrastructure.Initializer;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using API.Authorization;
using Scalar.AspNetCore;
using Serilog;
using API.Filters;
using API.ServiceCollectionExtensions;
using Infrastructure.Services.Images;

// Початкове базове логування (до налаштування з appsettings)
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting web application");

    var builder = WebApplication.CreateBuilder(args);

    // Add Aspire service defaults (OpenTelemetry, Health Checks, Service Discovery)
    builder.AddServiceDefaults();

    // Налаштування Serilog з конфігурації
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            path: "logs/log-.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"));

    // Controllers
    builder.Services.AddControllers();
    // HttpContextAccessor для доступу до HttpContext в сервісах
    builder.Services.AddHttpContextAccessor();
    // Application services (validators, MediatR behaviors, etc.)
    builder.Services.AddApplicationServices();

    // Scalar + OpenAPI
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi();


    // DbContext
    if (!builder.Environment.IsDevelopment())
    {
        // Use PostgreSQL in non-testing environments
        builder.Services.AddDbContext<AppDbContext>(opt =>
           opt.UseNpgsql(builder.Configuration.GetConnectionString("NeonConnection")));
    }
    else
    {

        builder.Services.AddDbContext<AppDbContext>(opt =>
          opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
    }
    // MediatR
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly);
    });
    // AutoMapper - scan Application assembly where AutoMapperProfile is located
    builder.Services.AddAutoMapper(cfg => cfg.LicenseKey = "<License Key Here>", typeof(AssemblyMarker).Assembly);

    // Repositories
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IMediaImageRepository, MediaImageRepository>();
    builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
    builder.Services.AddScoped<IStoreRepository, StoreRepository>();
    builder.Services.AddScoped<IProductRepository, ProductRepository>();
    builder.Services.AddScoped<ISkuRepository, SkuRepository>();
    builder.Services.AddScoped<ITagRepository, TagRepository>();
    builder.Services.AddScoped<IProductGalleryRepository, ProductGalleryRepository>();
    builder.Services.AddScoped<ISkuGalleryRepository, SkuGalleryRepository>();
    builder.Services.AddScoped<IAttributeDefinitionRepository, AttributeDefinitionRepository>();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    builder.Services.AddIdentity<ApplicationUser, RoleEntity>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 6;
            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+абвгґдеєжзиіїйклмнопрстуфхцчшщьюяАБВГҐДЕЄЖЗИІЇЙКЛМНОПРСТУФХЦЧШЩЬЮЯ";
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();
    // CORS: normalize configured origins (trim + remove trailing slashes)
    var rawCors = builder.Configuration["AllowedCorsOrigins"];
    string[] allowedOrigins;
    if (!string.IsNullOrWhiteSpace(rawCors))
    {
        allowedOrigins = rawCors
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(o => o.Trim().TrimEnd('/'))
            .Where(o => o.Length > 0)
            .Distinct()
            .ToArray();
        Console.WriteLine("Allowed CORS Origins: " + string.Join(", ", allowedOrigins));
    }
    else
    {
        allowedOrigins = new[] { "http://localhost:5173", "http://localhost:5188" };
    }

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });
    Log.Information("CORS configured with origins: {Origins}", string.Join(", ", allowedOrigins));
    // Memory cache for rate-limiting
    builder.Services.AddMemoryCache();
    builder.Services.AddRateLimiterFromConfiguration(builder.Configuration);
    // Redis distributed cache and Output Cache (uses Aspire if available)
    builder.AddRedisCache();
    // JWT Authentication
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IRoleService, RoleService>();
    builder.Services.AddScoped<IAdminUserService, AdminUserService>();
    builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ClaimsPrincipalFactory>();
    // Permission-based dynamic policies (policies like "Permission:users.read")
    builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
    builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
    // Email sender (SMTP) - reads SmtpSettings from configuration
    // Registered as singleton so hosted background service can consume it safely.
    builder.Services.AddSingleton<Application.Interfaces.IEmailService, SmtpEmailService>();
    // Background email queue and hosted service
    builder.Services.AddSingleton<BackgroundEmailQueue>();
    builder.Services.AddSingleton<IEmailQueue>(sp => sp.GetRequiredService<BackgroundEmailQueue>());
    builder.Services.AddHostedService<EmailSenderBackgroundService>();
    // Cloudflare Turnstile service
    builder.Services.AddHttpClient<Application.Interfaces.ITurnstileService, Infrastructure.Services.TurnstileService>();
    // Action filter which validates incoming Turnstile tokens when present
    builder.Services.AddScoped<TurnstileValidationFilter>();
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters();
        options.TokenValidationParameters.ValidateIssuer = true;
        options.TokenValidationParameters.ValidateAudience = true;
        options.TokenValidationParameters.ValidateLifetime = true;
        options.TokenValidationParameters.ValidateIssuerSigningKey = true;
        options.TokenValidationParameters.ValidIssuer = builder.Configuration["JwtSettings:Issuer"];
        options.TokenValidationParameters.ValidAudience = builder.Configuration["JwtSettings:Audience"];
        options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:AccessTokenSecret"]!));
    });
    // Image service
    builder.Services.AddScoped<IImageService, ImageSharpService>();
    // Передаємо builder.Environment у метод розширення
    builder.Services.AddFileStorage(builder.Configuration, builder.Environment);
    if (builder.Environment.IsDevelopment())
    {
        var contentRoot = builder.Environment.ContentRootPath;
        var webRoot = Path.Combine(contentRoot, "wwwroot");
        // Якщо папки немає - створюємо її фізично
        if (!Directory.Exists(webRoot))
        {
            Directory.CreateDirectory(webRoot);
        }
    }
    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        // Виводимо конфігурацію при старті в dev режимі
        var config = app.Configuration;
        var redisEnabled = config.GetValue<bool>("Redis:Enabled");
        var storageProvider = config["Storage:Provider"] ?? "local";
        var dbConnection = config.GetConnectionString("DefaultConnection");
        var dbHost = "unknown";
        if (!string.IsNullOrEmpty(dbConnection))
        {
            var hostMatch = System.Text.RegularExpressions.Regex.Match(dbConnection, @"Host=([^;]+)");
            dbHost = hostMatch.Success ? hostMatch.Groups[1].Value : "configured";
        }

        Log.Information("========== Development Configuration ==========");
        Log.Information("Redis Caching: {RedisEnabled}", redisEnabled ? "ENABLED" : "DISABLED");
        if (redisEnabled)
        {
            // Check Aspire connection string first, then fallback to config
            var aspireRedis = config.GetConnectionString("redis");
            var configRedis = config["Redis:ConnectionString"] ?? "localhost:6379";
            var actualRedis = aspireRedis ?? configRedis;
            Log.Information("Redis Connection: {RedisConnection} (Aspire: {AspireProvided})",
                actualRedis,
                aspireRedis != null ? "YES" : "NO");
        }
        Log.Information("Storage Provider: {StorageProvider}", storageProvider.ToUpper());
        Log.Information("Database Host: {DbHost}", dbHost);
        Log.Information("CORS Origins: {CorsOrigins}", config["AllowedCorsOrigins"] ?? "not configured");
        Log.Information("===============================================");

        app.UseStaticFiles();
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.Title = "This is my Scalar API";
            options.DarkMode = true;
            options.Favicon = "path";
            options.DefaultHttpClient = new KeyValuePair<ScalarTarget, ScalarClient>(ScalarTarget.CSharp, ScalarClient.RestSharp);
            options.HideModels = false;
            options.Layout = ScalarLayout.Modern;
            options.ShowSidebar = true;

            options.Authentication = new ScalarAuthenticationOptions();
            options.Authentication.PreferredSecuritySchemes = new[] { "Bearer" };
        });
    }

    // CORS must be before Authentication/Authorization to handle preflight OPTIONS requests
    app.UseCors("AllowFrontend");

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();
    // Output caching middleware (must be after authorization)
    app.UseOutputCache();

    app.MapControllers();

    // Map Aspire default endpoints (health, alive)
    app.MapDefaultEndpoints();

    // Skip seeding in Testing environment (handled by TestWebApplicationFactory)
    if (!app.Environment.IsEnvironment("Testing"))
    {
        await app.SeedDataAsync();
    }

    Log.Information("Application started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Required for WebApplicationFactory<Program> in integration tests
public partial class Program { }
