using Bogus;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Entities.Identity;
using Infrastructure.Initializer;
using Infrastructure.Messaging.Contracts;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DebugController : ControllerBase
{
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly IUserClaimsPrincipalFactory<ApplicationUser> _claimsFactory;
	private readonly RoleManager<Infrastructure.Entities.Identity.RoleEntity> _roleManager;
	private readonly IImageService _imageService;
	private readonly IFileStorage _fileStorage;
	private readonly AppDbContext _dbContext;
	private readonly IHostEnvironment _hostEnvironment;
	private readonly IBus _bus;
	private readonly ILogger<DebugController> _logger;

	public DebugController(
		UserManager<ApplicationUser> userManager,
		IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
		RoleManager<Infrastructure.Entities.Identity.RoleEntity> roleManager,
		IImageService imageService,
		IFileStorage fileStorage,
		AppDbContext dbContext,
		IHostEnvironment hostEnvironment,
		IBus bus,
		ILogger<DebugController> logger)
	{
		_userManager = userManager;
		_claimsFactory = claimsFactory;
		_roleManager = roleManager;
		_imageService = imageService;
		_fileStorage = fileStorage;
		_dbContext = dbContext;
		_hostEnvironment = hostEnvironment;
		_bus = bus;
		_logger = logger;
	}

	// GET: /api/debug/user-claims?email=admin@example.com
	[AllowAnonymous]
	[HttpGet("user-claims")]
	public async Task<IActionResult> GetUserClaims([FromQuery] string email)
	{
		if (string.IsNullOrEmpty(email)) return BadRequest("Provide email query parameter");

		var user = await _userManager.FindByEmailAsync(email);
		if (user == null) return NotFound();

		// Create ClaimsPrincipal using the registered factory
		var principal = await _claimsFactory.CreateAsync(user);

		var claims = principal.Claims.Select(c => new { c.Type, c.Value }).ToList();
		var roles = await _userManager.GetRolesAsync(user);

		return Ok(new { Email = user.Email, UserName = user.UserName, Roles = roles, Claims = claims });
	}
	// POST: /api/debug/test-upload
    // Тестує повний пайплайн: ImageSharp -> FileStorage -> Database
    [HttpPost("test-upload")]
    [AllowAnonymous] // Або [Authorize], якщо хочете захистити
    public async Task<IActionResult> TestUpload(IFormFile file)
    {
        if (file == null || file.Length == 0) 
            return BadRequest("Файл не вибрано");

        try 
        {
            // 1. ОБРОБКА (ImageSharp)
            // Конвертуємо у WebP і робимо ресайз до 500px по ширині (приклад)
            using var rawStream = file.OpenReadStream();
            var processed = await _imageService.ProcessAsync(
                rawStream, 
                ImageResizeMode.KeepAspect, 
                500, 500);

            // 2. ЗАВАНТАЖЕННЯ (Local / S3 / Azure / R2)
            // Генеруємо нове ім'я з розширенням .webp
            var newFileName = Path.GetFileNameWithoutExtension(file.FileName) + processed.Extension;
            
            var storageKey = await _fileStorage.UploadAsync(
                processed.ImageStream, 
                newFileName, 
                processed.ContentType);

            // 3. ЗБЕРЕЖЕННЯ В БД (Entity Framework)
            // Створюємо сутність (використовуємо ваш публічний конструктор)
            var mediaImage = new MediaImage(
                storageKey, 
                processed.ContentType, 
                processed.Width, 
                processed.Height, 
                $"Test upload of {file.FileName}" // AltText
            );

            // Додаємо в контекст і зберігаємо
            _dbContext.Set<MediaImage>().Add(mediaImage);
            await _dbContext.SaveChangesAsync();

            // 4. РЕЗУЛЬТАТ
            return Ok(new 
            { 
                Message = "Успішно завантажено та збережено",
                DbId = mediaImage.Id,
                PublicUrl = _fileStorage.GetPublicUrl(storageKey),
                OriginalSize = file.Length,
                ProcessedSize = processed.ImageStream.Length, // Можна побачити наскільки WebP менший
                Dimensions = $"{processed.Width}x{processed.Height}",
                Format = processed.Extension
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message, StackTrace = ex.StackTrace });
        }
    }

    // POST: /api/debug/test-masstransit-email
    // Тестує відправку email через MassTransit SQL Transport
    [HttpPost("test-masstransit-email")]
    [AllowAnonymous]
    public async Task<IActionResult> TestMassTransitEmail([FromBody] TestEmailRequest request)
    {
        try
        {
            var to = request.To ?? "test@example.com";
            var correlationId = Guid.NewGuid().ToString();
            var messageId = Guid.NewGuid();
            
            _logger.LogInformation("[MASS TRANSIT EMAIL TEST] Starting email send test");
            _logger.LogInformation("[MASS TRANSIT EMAIL TEST] Recipient: {To}", to);
            _logger.LogInformation("[MASS TRANSIT EMAIL TEST] CorrelationId: {CorrelationId}", correlationId);
            _logger.LogInformation("[MASS TRANSIT EMAIL TEST] MessageId: {MessageId}", messageId);
            
            var command = new SendEmailCommand
            {
                MessageId = messageId,
                To = to,
                Subject = request.Subject ?? "Test Email via MassTransit",
                Body = request.Body ?? $"<h1>MassTransit Test Email</h1><p>This is a test email sent via MassTransit SQL Transport at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>",
                IsHtml = request.IsHtml ?? true,
                From = request.From,
                CorrelationId = correlationId,
                RequestedAt = DateTime.UtcNow
            };
            
            _logger.LogInformation("[MASS TRANSIT EMAIL TEST] Publishing SendEmailCommand to bus");
            _logger.LogInformation("[MASS TRANSIT EMAIL TEST] Bus Type: {BusType}", _bus.GetType().Name);
            
            await _bus.Publish(command);
            
            _logger.LogInformation("[MASS TRANSIT EMAIL TEST] Command published successfully");
            _logger.LogInformation("[MASS TRANSIT EMAIL TEST] Endpoint: send-email-queue");
            
            return Ok(new 
            { 
                Success = true,
                Message = "Email command published to MassTransit",
                Details = new
                {
                    Recipient = to,
                    MessageId = messageId,
                    CorrelationId = correlationId,
                    Subject = command.Subject,
                    Timestamp = DateTime.UtcNow,
                    Transport = "MassTransit SQL Transport",
                    Queue = "send-email-queue"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MASS TRANSIT EMAIL TEST] Failed to send email via MassTransit");
            return StatusCode(500, new 
            { 
                Success = false,
                Error = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    }
    
    // GET: /api/debug/masstransit-status
    // Перевіряє статус MassTransit bus
    [HttpGet("masstransit-status")]
    [AllowAnonymous]
    public IActionResult GetMassTransitStatus()
    {
        try
        {
            _logger.LogInformation("[MASS TRANSIT STATUS CHECK] Checking bus status");
            
            var busType = _bus.GetType().Name;
            var busAddress = _bus.Address?.ToString() ?? "N/A";
            
            _logger.LogInformation("[MASS TRANSIT STATUS CHECK] Bus Type: {BusType}", busType);
            _logger.LogInformation("[MASS TRANSIT STATUS CHECK] Bus Address: {BusAddress}", busAddress);
            
            return Ok(new 
            { 
                Status = "Active",
                BusType = busType,
                BusAddress = busAddress,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MASS TRANSIT STATUS CHECK] Error checking bus status");
            return StatusCode(500, new 
            { 
                Status = "Error",
                Error = ex.Message
            });
        }
    }
}

public class TestEmailRequest
{
    public string? To { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public bool? IsHtml { get; set; }
    public string? From { get; set; }
}
