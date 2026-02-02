namespace Domain.ValueObjects;

/// <summary>
/// Value object representing a shipping address
/// </summary>
public class ShippingAddress : IEquatable<ShippingAddress>
{
	public string FirstName { get; private set; }
	public string LastName { get; private set; }
	public string PhoneNumber { get; private set; }
	public string Email { get; private set; }
	public string AddressLine1 { get; private set; }
	public string? AddressLine2 { get; private set; }
	public string City { get; private set; }
	public string? State { get; private set; }
	public string PostalCode { get; private set; }
	public string Country { get; private set; }

	private ShippingAddress()
	{
		FirstName = string.Empty;
		LastName = string.Empty;
		PhoneNumber = string.Empty;
		Email = string.Empty;
		AddressLine1 = string.Empty;
		City = string.Empty;
		PostalCode = string.Empty;
		Country = string.Empty;
	}

	public ShippingAddress(
		string firstName,
		string lastName,
		string phoneNumber,
		string email,
		string addressLine1,
		string? addressLine2,
		string city,
		string? state,
		string postalCode,
		string country)
	{
		if (string.IsNullOrWhiteSpace(firstName))
			throw new ArgumentException("First name is required", nameof(firstName));

		if (string.IsNullOrWhiteSpace(lastName))
			throw new ArgumentException("Last name is required", nameof(lastName));

		if (string.IsNullOrWhiteSpace(phoneNumber))
			throw new ArgumentException("Phone number is required", nameof(phoneNumber));

		if (string.IsNullOrWhiteSpace(email))
			throw new ArgumentException("Email is required", nameof(email));

		if (string.IsNullOrWhiteSpace(addressLine1))
			throw new ArgumentException("Address line 1 is required", nameof(addressLine1));

		if (string.IsNullOrWhiteSpace(city))
			throw new ArgumentException("City is required", nameof(city));

		if (string.IsNullOrWhiteSpace(postalCode))
			throw new ArgumentException("Postal code is required", nameof(postalCode));

		if (string.IsNullOrWhiteSpace(country))
			throw new ArgumentException("Country is required", nameof(country));

		FirstName = firstName.Trim();
		LastName = lastName.Trim();
		PhoneNumber = phoneNumber.Trim();
		Email = email.Trim();
		AddressLine1 = addressLine1.Trim();
		AddressLine2 = addressLine2?.Trim();
		City = city.Trim();
		State = state?.Trim();
		PostalCode = postalCode.Trim();
		Country = country.Trim();
	}

	/// <summary>
	/// Returns the full name of the recipient
	/// </summary>
	public string GetFullName() => $"{FirstName} {LastName}";

	/// <summary>
	/// Returns the formatted address as a single string
	/// </summary>
	public string GetFormattedAddress()
	{
		var parts = new List<string>
		{
			AddressLine1
		};

		if (!string.IsNullOrWhiteSpace(AddressLine2))
			parts.Add(AddressLine2);

		parts.Add($"{City}, {State} {PostalCode}".Trim());
		parts.Add(Country);

		return string.Join(", ", parts);
	}

	public bool Equals(ShippingAddress? other)
	{
		if (other is null) return false;
		if (ReferenceEquals(this, other)) return true;

		return FirstName == other.FirstName &&
			   LastName == other.LastName &&
			   PhoneNumber == other.PhoneNumber &&
			   Email == other.Email &&
			   AddressLine1 == other.AddressLine1 &&
			   AddressLine2 == other.AddressLine2 &&
			   City == other.City &&
			   State == other.State &&
			   PostalCode == other.PostalCode &&
			   Country == other.Country;
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as ShippingAddress);
	}

	public override int GetHashCode()
	{
		var hash = new HashCode();
		hash.Add(FirstName);
		hash.Add(LastName);
		hash.Add(PhoneNumber);
		hash.Add(Email);
		hash.Add(AddressLine1);
		hash.Add(AddressLine2);
		hash.Add(City);
		hash.Add(State);
		hash.Add(PostalCode);
		hash.Add(Country);
		return hash.ToHashCode();
	}

	public static bool operator ==(ShippingAddress? left, ShippingAddress? right)
	{
		return Equals(left, right);
	}

	public static bool operator !=(ShippingAddress? left, ShippingAddress? right)
	{
		return !Equals(left, right);
	}
}
