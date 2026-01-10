using System;
namespace Domain.Entities;

/// <summary>
/// Чиста доменна модель користувача
/// Містить тільки бізнес-логіку та доменні дані
/// </summary>
public class User : BaseEntity<Guid>
{
    // Приватні поля для інкапсуляції
    private string? _name;
    private string? _surname;
    private string? _email;
    private string? _phoneNumber;
    private bool _isBlocked;

    // Конструктор для Entity Framework
    private User() { }

    /// <summary>
    /// Конструктор для створення нового користувача
    /// </summary>
    public User(Guid identityUserId, string? name = null, string? surname = null, string email = null, string? phoneNumber = null)
    {
        IdentityUserId = identityUserId;
        _name = name;
        _surname = surname;
        _email = email;
        _phoneNumber = phoneNumber;
        _isBlocked = false;
    }

    /// <summary>
    /// Зв'язок з Infrastructure Identity
    /// </summary>
    public Guid IdentityUserId { get; private set; }

    /// <summary>
    /// Ім'я користувача
    /// </summary>
    public string? Name
    {
        get => _name;
        private set => _name = value;
    }

    /// <summary>
    /// Прізвище користувача
    /// </summary>
    public string? Surname
    {
        get => _surname;
        private set => _surname = value;
    }
    /// <summary>
    /// Електронна пошта користувача
    /// </summary>
    public string? Email
    {
        get => _email;
        private set => _email = value;
    }

    /// <summary>
    /// Номер телефону користувача
    /// </summary>
    public string? PhoneNumber
    {
        get => _phoneNumber;
        private set => _phoneNumber = value;
    }


    /// <summary>
    /// Чи заблокований користувач
    /// </summary>
    public bool IsBlocked
    {
        get => _isBlocked;
        private set => _isBlocked = value;
    }
    public Guid? AvatarId { get; private set; }
    public virtual MediaImage? Avatar { get; private set; }


    // Бізнес-методи
    public void SetAvatar(MediaImage image)
    {
        // Логіка: аватар не прив'язаний до продукту, тому ProductId залишається null
        Avatar = image;
        AvatarId = image.Id;
    }
    /// <summary>
    /// Оновлює профіль користувача
    /// </summary>
    public void UpdateProfile(string? name, string? surname, string? imageUrl = null)
    {
        if (_isBlocked)
            throw new InvalidOperationException("Cannot update profile of blocked user");

        _name = name;
        _surname = surname;


        MarkAsUpdated();
    }

    /// <summary>
    /// Оновлює електронну пошту користувача
    /// </summary>
    public void UpdateEmail(string? email)
    {
        if (_isBlocked)
            throw new InvalidOperationException("Cannot update email of blocked user");

        _email = email;
        MarkAsUpdated();
    }

    /// <summary>
    /// Оновлює номер телефону користувача
    /// </summary>
    public void UpdatePhoneNumber(string? phoneNumber)
    {
        if (_isBlocked)
            throw new InvalidOperationException("Cannot update phone number of blocked user");

        _phoneNumber = phoneNumber;
        MarkAsUpdated();
    }

    /// <summary>
    /// Блокує користувача
    /// </summary>
    public void Block()
    {
        if (_isBlocked)
            throw new InvalidOperationException("User is already blocked");

        _isBlocked = true;
        MarkAsUpdated();
    }

    /// <summary>
    /// Розблоковує користувача
    /// </summary>
    public void Unblock()
    {
        if (!_isBlocked)
            throw new InvalidOperationException("User is not blocked");

        _isBlocked = false;
        MarkAsUpdated();
    }

    /// <summary>
    /// Оновлює аватар користувача
    /// </summary>
    public void UpdateAvatar(string imageUrl)
    {
        if (_isBlocked)
            throw new InvalidOperationException("Cannot update avatar of blocked user");


        MarkAsUpdated();
    }

    /// <summary>
    /// Видаляє аватар користувача
    /// </summary>
    public void RemoveAvatar()
    {
        Avatar = null;
        MarkAsUpdated();
    }
}
