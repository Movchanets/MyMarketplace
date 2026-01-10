using Domain.Entities;

namespace Domain.Interfaces.Repositories;

/// <summary>
/// Repository для роботи з MediaImage.
/// Забезпечує операції CRUD та додаткові методи для роботи з зображеннями.
/// </summary>
public interface IMediaImageRepository
{
	/// <summary>
	/// Отримує MediaImage за унікальним ідентифікатором.
	/// </summary>
	/// <param name="id">Ідентифікатор зображення</param>
	/// <returns>MediaImage або null, якщо не знайдено</returns>
	Task<MediaImage?> GetByIdAsync(Guid id);

	/// <summary>
	/// Отримує MediaImage за ключем сховища (StorageKey).
	/// </summary>
	/// <param name="storageKey">Унікальний ключ у файловому сховищі</param>
	/// <returns>MediaImage або null, якщо не знайдено</returns>
	Task<MediaImage?> GetByStorageKeyAsync(string storageKey);

	/// <summary>
	/// Додає нове зображення до бази даних.
	/// </summary>
	/// <param name="mediaImage">Зображення для додавання</param>
	/// <returns>Додане зображення з ідентифікатором</returns>
	void Add(MediaImage mediaImage);

	/// <summary>
	/// Додає нове зображення до бази даних асинхронно.
	/// </summary>
	/// <param name="mediaImage">Зображення для додавання</param>
	Task AddAsync(MediaImage mediaImage);

	/// <summary>
	/// Оновлює існуюче зображення в базі даних.
	/// </summary>
	/// <param name="mediaImage">Зображення для оновлення</param>
	/// <returns>Оновлене зображення</returns>
	void Update(MediaImage mediaImage);

	/// <summary>
	/// Видаляє зображення з бази даних.
	/// </summary>
	/// <param name="mediaImage">Зображення для видалення</param>
	void Delete(MediaImage mediaImage);

	/// <summary>
	/// Отримує всі зображення з бази даних.
	/// </summary>
	/// <returns>Колекція всіх зображень</returns>
	Task<IEnumerable<MediaImage>> GetAllAsync();

	/// <summary>
	/// Отримує всі зображення для конкретного продукту.
	/// </summary>
	/// <param name="productId">Ідентифікатор продукту</param>
	/// <returns>Колекція зображень продукту</returns>
	Task<IEnumerable<MediaImage>> GetByProductIdAsync(Guid productId);

	/// <summary>
	/// Отримує зображення, які не прив'язані до продукту або користувача (orphaned).
	/// Корисно для очищення неіснуючих зображень.
	/// </summary>
	/// <returns>Колекція зображень без прив'язки</returns>
	Task<IEnumerable<MediaImage>> GetOrphanedImagesAsync();
}
