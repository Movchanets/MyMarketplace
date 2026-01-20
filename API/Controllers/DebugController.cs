using Bogus;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Entities.Identity;
using Infrastructure.Initializer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;

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

	public DebugController(
		UserManager<ApplicationUser> userManager,
		IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
		RoleManager<Infrastructure.Entities.Identity.RoleEntity> roleManager,
		IImageService imageService,
		IFileStorage fileStorage,
		AppDbContext dbContext,
		IHostEnvironment hostEnvironment)
	{
		_userManager = userManager;
		_claimsFactory = claimsFactory;
		_roleManager = roleManager;
		_imageService = imageService;
		_fileStorage = fileStorage;
		_dbContext = dbContext;
		_hostEnvironment = hostEnvironment;
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

}
