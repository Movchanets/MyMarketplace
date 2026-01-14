using Infrastructure;
using Infrastructure.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Application.Interfaces;
using Testcontainers.PostgreSql;
using Respawn;
using Respawn.Graph;
using Npgsql;
using System.Threading;

namespace Infrastructure.IntegrationTests;

/// <summary>
/// Базовий клас для інтеграційних тестів
/// Підіймає реальну PostgreSQL БД через Testcontainers
/// </summary>
public abstract class TestBase : IAsyncLifetime
{
    protected readonly AppDbContext DbContext;
    protected readonly UserManager<ApplicationUser> UserManager;
    protected readonly RoleManager<RoleEntity> RoleManager;
    protected readonly Application.Interfaces.IUserService IdentityService;
    protected readonly Domain.Interfaces.Repositories.IUserRepository UserRepository;
    private readonly ServiceProvider _serviceProvider;

    private static PostgreSqlContainer? _sharedContainer;
    private static Task? _sharedContainerStartTask;
    private static readonly SemaphoreSlim SharedContainerLock = new(1, 1);

    private static Task<Respawner>? _respawnerTask;
    private static bool _schemaInitialized;

    protected TestBase()
    {
        // Дочікуємо запуск контейнера перед побудовою ServiceProvider
        var container = EnsureSharedContainerStartedAsync().GetAwaiter().GetResult();

        // Створюємо ServiceCollection для Dependency Injection
        var services = new ServiceCollection();

        // Додаємо логування (необхідне для Identity)
        services.AddLogging();

        // Налаштовуємо PostgreSQL БД через Testcontainers
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(container.GetConnectionString()));

        // Налаштовуємо ASP.NET Core Identity (як у реальному додатку)
        // Для тестів замінимо провайдера токенів на простий детермінований провайдер,
        // щоб уникнути викликів DPAPI / нативних методів шифрування під час тестів.
        var identityBuilder = services.AddIdentity<ApplicationUser, RoleEntity>(options =>
            {
                // Ті ж налаштування, що й у Program.cs
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // Додаємо простий тестовий провайдер токенів під назвою "Simple" і використовуємо його
        // для скидання паролю у тестовому середовищі.
        identityBuilder.AddTokenProvider<SimpleTestTokenProvider<ApplicationUser>>(SimpleTestTokenProvider<ApplicationUser>.ProviderName);
        services.Configure<Microsoft.AspNetCore.Identity.IdentityOptions>(opts =>
        {
            opts.Tokens.PasswordResetTokenProvider = SimpleTestTokenProvider<ApplicationUser>.ProviderName;
        });

        // Реєструємо репозиторії
        services.AddScoped<Domain.Interfaces.Repositories.IUserRepository, Infrastructure.Repositories.UserRepository>();
        services.AddScoped<Domain.Interfaces.Repositories.IMediaImageRepository, Infrastructure.Repositories.MediaImageRepository>();
        services.AddScoped<Domain.Interfaces.Repositories.IProductRepository, Infrastructure.Repositories.ProductRepository>();
        services.AddScoped<Application.Interfaces.IUnitOfWork, Infrastructure.Services.UnitOfWork>();

        // Мокаємо IFileStorage та IImageService (не потрібні для більшості тестів)
        var fileStorageMock = new Mock<IFileStorage>();
        fileStorageMock.Setup(x => x.UploadAsync(It.IsAny<System.IO.Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-storage-key.webp");
        fileStorageMock.Setup(x => x.GetPublicUrl(It.IsAny<string>()))
            .Returns<string>(key => $"/uploads/{key}");
        services.AddSingleton(fileStorageMock.Object);

        var imageServiceMock = new Mock<IImageService>();
        var processedStream = new System.IO.MemoryStream(new byte[] { 0x52, 0x49, 0x46, 0x46 });
        imageServiceMock.Setup(x => x.ProcessAsync(It.IsAny<System.IO.Stream>(), It.IsAny<ImageResizeMode>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessedImageResult(processedStream, "image/webp", ".webp", 256, 256));
        services.AddSingleton(imageServiceMock.Object);

        // Реєструємо AutoMapper
        var mapperConfig = new AutoMapper.MapperConfiguration(mc =>
        {
            mc.AddProfile(new Application.Mapping.AutoMapperProfile());
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        services.AddSingleton(mapperConfig.CreateMapper());

        // Реєструємо UserService
        services.AddScoped<Application.Interfaces.IUserService, Infrastructure.Services.UserService>();

        // Будуємо ServiceProvider (контейнер залежностей)
        _serviceProvider = services.BuildServiceProvider();

        // Отримуємо екземпляри сервісів
        DbContext = _serviceProvider.GetRequiredService<AppDbContext>();
        UserManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        RoleManager = _serviceProvider.GetRequiredService<RoleManager<RoleEntity>>();
        IdentityService = _serviceProvider.GetRequiredService<Application.Interfaces.IUserService>();
        UserRepository = _serviceProvider.GetRequiredService<Domain.Interfaces.Repositories.IUserRepository>();

    }

    /// <summary>
    /// Стартуємо контейнер та створюємо схему БД
    /// </summary>
    public async Task InitializeAsync()
    {
        var container = await EnsureSharedContainerStartedAsync();

        if (!_schemaInitialized)
        {
            await DbContext.Database.EnsureCreatedAsync();
            _schemaInitialized = true;
        }

        // Create Respawner only after schema exists
        _respawnerTask ??= CreateRespawnerAsync();

        // Швидко очищаємо дані між тестами, не дропаючи схему
        var respawner = await _respawnerTask;
        await using var connection = new NpgsqlConnection(container.GetConnectionString());
        await connection.OpenAsync();
        await respawner.ResetAsync(connection);
    }

    /// <summary>
    /// Очищення після тесту - зупиняємо контейнер
    /// </summary>
    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
        _serviceProvider?.Dispose();
    }

    private static async Task<Respawner> CreateRespawnerAsync()
    {
        var container = await EnsureSharedContainerStartedAsync();
        await using var connection = new NpgsqlConnection(container.GetConnectionString());
        await connection.OpenAsync();

        return await Respawner.CreateAsync(
            connection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = new[] { "public" },
                TablesToIgnore = new[] { new Table("public", "__EFMigrationsHistory") }
            });
    }

    private static async Task<PostgreSqlContainer> EnsureSharedContainerStartedAsync()
    {
        if (_sharedContainer is not null)
        {
            if (_sharedContainerStartTask is not null)
            {
                await _sharedContainerStartTask;
            }

            return _sharedContainer;
        }

        await SharedContainerLock.WaitAsync();
        try
        {
            if (_sharedContainer is not null)
            {
                if (_sharedContainerStartTask is not null)
                {
                    await _sharedContainerStartTask;
                }

                return _sharedContainer;
            }

            const int maxAttempts = 20;
            var delay = TimeSpan.FromSeconds(2);

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    _sharedContainer = new PostgreSqlBuilder()
                        .WithImage("postgres:16-alpine")
                        .WithDatabase("testdb")
                        .WithUsername("testuser")
                        .WithPassword("testpass")
                        .Build();

                    _sharedContainerStartTask = _sharedContainer.StartAsync();
                    await _sharedContainerStartTask;
                    return _sharedContainer;
                }
                catch (Exception ex) when (ex.GetType().Name == "DockerUnavailableException" && attempt < maxAttempts)
                {
                    await Task.Delay(delay);
                }
            }

            throw new InvalidOperationException(
                "Docker is not available. Start Docker Desktop (or ensure it is reachable at npipe://./pipe/docker_engine) and re-run the tests.");
        }
        finally
        {
            SharedContainerLock.Release();
        }
    }

    // Простий детермінований провайдер токенів для тестів.
    // Генерує токен на основі SecurityStamp користувача та purpose, і валідуює зворотно.
    public class SimpleTestTokenProvider<TUser> : Microsoft.AspNetCore.Identity.IUserTwoFactorTokenProvider<TUser>
        where TUser : class
    {
        public const string ProviderName = "Simple";

        public Task<string> GenerateAsync(string purpose, Microsoft.AspNetCore.Identity.UserManager<TUser> manager, TUser user)
        {
            // Використовуємо security stamp як основну секретну частину. Якщо його немає, використовуємо GUID.
            return Task.Run(async () =>
            {
                var stamp = await manager.GetSecurityStampAsync(user).ConfigureAwait(false);
                // Fallback: якщо stamp == null, беремо GUID
                if (string.IsNullOrEmpty(stamp)) stamp = Guid.NewGuid().ToString("N");
                var raw = stamp + ":" + purpose;
                return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));
            });
        }

        public Task<bool> ValidateAsync(string purpose, string token, Microsoft.AspNetCore.Identity.UserManager<TUser> manager, TUser user)
        {
            return Task.Run(async () =>
            {
                var expected = await GenerateAsync(purpose, manager, user).ConfigureAwait(false);
                return expected == token;
            });
        }

        public Task<bool> CanGenerateTwoFactorTokenAsync(Microsoft.AspNetCore.Identity.UserManager<TUser> manager, TUser user)
        {
            return Task.FromResult(true);
        }
    }
}
