using Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.ValueObjects;

public class ShippingAddressTests
{
	[Fact]
	public void Constructor_WithValidData_CreatesAddress()
	{
		// Arrange & Act
		var address = new ShippingAddress(
			firstName: "John",
			lastName: "Doe",
			phoneNumber: "1234567890",
			email: "john@example.com",
			addressLine1: "123 Main St",
			addressLine2: "Apt 4B",
			city: "New York",
			state: "NY",
			postalCode: "10001",
			country: "USA"
		);

		// Assert
		address.FirstName.Should().Be("John");
		address.LastName.Should().Be("Doe");
		address.PhoneNumber.Should().Be("1234567890");
		address.Email.Should().Be("john@example.com");
		address.AddressLine1.Should().Be("123 Main St");
		address.AddressLine2.Should().Be("Apt 4B");
		address.City.Should().Be("New York");
		address.State.Should().Be("NY");
		address.PostalCode.Should().Be("10001");
		address.Country.Should().Be("USA");
	}

	[Theory]
	[InlineData(null, "Doe", "1234567890", "john@example.com", "123 Main St", "New York", "10001", "USA")]
	[InlineData("", "Doe", "1234567890", "john@example.com", "123 Main St", "New York", "10001", "USA")]
	[InlineData("   ", "Doe", "1234567890", "john@example.com", "123 Main St", "New York", "10001", "USA")]
	public void Constructor_WithEmptyFirstName_ThrowsArgumentException(string? firstName, string lastName, string phone, string email, string address1, string city, string postal, string country)
	{
		// Act
		Action act = () => new ShippingAddress(firstName!, lastName, phone, email, address1, null, city, null, postal, country);

		// Assert
		act.Should().Throw<ArgumentException>().WithMessage("*First name is required*");
	}

	[Theory]
	[InlineData("John", null, "1234567890", "john@example.com", "123 Main St", "New York", "10001", "USA")]
	[InlineData("John", "", "1234567890", "john@example.com", "123 Main St", "New York", "10001", "USA")]
	public void Constructor_WithEmptyLastName_ThrowsArgumentException(string firstName, string? lastName, string phone, string email, string address1, string city, string postal, string country)
	{
		// Act
		Action act = () => new ShippingAddress(firstName, lastName!, phone, email, address1, null, city, null, postal, country);

		// Assert
		act.Should().Throw<ArgumentException>().WithMessage("*Last name is required*");
	}

	[Theory]
	[InlineData("John", "Doe", null, "john@example.com", "123 Main St", "New York", "10001", "USA")]
	[InlineData("John", "Doe", "", "john@example.com", "123 Main St", "New York", "10001", "USA")]
	public void Constructor_WithEmptyPhoneNumber_ThrowsArgumentException(string firstName, string lastName, string? phone, string email, string address1, string city, string postal, string country)
	{
		// Act
		Action act = () => new ShippingAddress(firstName, lastName, phone!, email, address1, null, city, null, postal, country);

		// Assert
		act.Should().Throw<ArgumentException>().WithMessage("*Phone number is required*");
	}

	[Theory]
	[InlineData("John", "Doe", "1234567890", null, "123 Main St", "New York", "10001", "USA")]
	[InlineData("John", "Doe", "1234567890", "", "123 Main St", "New York", "10001", "USA")]
	public void Constructor_WithEmptyEmail_ThrowsArgumentException(string firstName, string lastName, string phone, string? email, string address1, string city, string postal, string country)
	{
		// Act
		Action act = () => new ShippingAddress(firstName, lastName, phone, email!, address1, null, city, null, postal, country);

		// Assert
		act.Should().Throw<ArgumentException>().WithMessage("*Email is required*");
	}

	[Theory]
	[InlineData("John", "Doe", "1234567890", "john@example.com", null, "New York", "10001", "USA")]
	[InlineData("John", "Doe", "1234567890", "john@example.com", "", "New York", "10001", "USA")]
	public void Constructor_WithEmptyAddressLine1_ThrowsArgumentException(string firstName, string lastName, string phone, string email, string? address1, string city, string postal, string country)
	{
		// Act
		Action act = () => new ShippingAddress(firstName, lastName, phone, email, address1!, null, city, null, postal, country);

		// Assert
		act.Should().Throw<ArgumentException>().WithMessage("*Address line 1 is required*");
	}

	[Theory]
	[InlineData("John", "Doe", "1234567890", "john@example.com", "123 Main St", null, "10001", "USA")]
	[InlineData("John", "Doe", "1234567890", "john@example.com", "123 Main St", "", "10001", "USA")]
	public void Constructor_WithEmptyCity_ThrowsArgumentException(string firstName, string lastName, string phone, string email, string address1, string? city, string postal, string country)
	{
		// Act
		Action act = () => new ShippingAddress(firstName, lastName, phone, email, address1, null, city!, null, postal, country);

		// Assert
		act.Should().Throw<ArgumentException>().WithMessage("*City is required*");
	}

	[Theory]
	[InlineData("John", "Doe", "1234567890", "john@example.com", "123 Main St", "New York", null, "USA")]
	[InlineData("John", "Doe", "1234567890", "john@example.com", "123 Main St", "New York", "", "USA")]
	public void Constructor_WithEmptyPostalCode_ThrowsArgumentException(string firstName, string lastName, string phone, string email, string address1, string city, string? postal, string country)
	{
		// Act
		Action act = () => new ShippingAddress(firstName, lastName, phone, email, address1, null, city, null, postal!, country);

		// Assert
		act.Should().Throw<ArgumentException>().WithMessage("*Postal code is required*");
	}

	[Theory]
	[InlineData("John", "Doe", "1234567890", "john@example.com", "123 Main St", "New York", "10001", null)]
	[InlineData("John", "Doe", "1234567890", "john@example.com", "123 Main St", "New York", "10001", "")]
	public void Constructor_WithEmptyCountry_ThrowsArgumentException(string firstName, string lastName, string phone, string email, string address1, string city, string postal, string? country)
	{
		// Act
		Action act = () => new ShippingAddress(firstName, lastName, phone, email, address1, null, city, null, postal, country!);

		// Assert
		act.Should().Throw<ArgumentException>().WithMessage("*Country is required*");
	}

	[Fact]
	public void GetFullName_ReturnsFirstAndLastName()
	{
		// Arrange
		var address = new ShippingAddress(
			firstName: "John",
			lastName: "Doe",
			phoneNumber: "1234567890",
			email: "john@example.com",
			addressLine1: "123 Main St",
			addressLine2: null,
			city: "New York",
			state: "NY",
			postalCode: "10001",
			country: "USA"
		);

		// Act
		var fullName = address.GetFullName();

		// Assert
		fullName.Should().Be("John Doe");
	}

	[Fact]
	public void GetFormattedAddress_WithAddressLine2_ReturnsFormattedString()
	{
		// Arrange
		var address = new ShippingAddress(
			firstName: "John",
			lastName: "Doe",
			phoneNumber: "1234567890",
			email: "john@example.com",
			addressLine1: "123 Main St",
			addressLine2: "Apt 4B",
			city: "New York",
			state: "NY",
			postalCode: "10001",
			country: "USA"
		);

		// Act
		var formatted = address.GetFormattedAddress();

		// Assert
		formatted.Should().Be("123 Main St, Apt 4B, New York, NY 10001, USA");
	}

	[Fact]
	public void GetFormattedAddress_WithoutAddressLine2_ReturnsFormattedString()
	{
		// Arrange
		var address = new ShippingAddress(
			firstName: "John",
			lastName: "Doe",
			phoneNumber: "1234567890",
			email: "john@example.com",
			addressLine1: "123 Main St",
			addressLine2: null,
			city: "New York",
			state: "NY",
			postalCode: "10001",
			country: "USA"
		);

		// Act
		var formatted = address.GetFormattedAddress();

		// Assert
		formatted.Should().Be("123 Main St, New York, NY 10001, USA");
	}

	[Fact]
	public void Equals_WithSameValues_ReturnsTrue()
	{
		// Arrange
		var address1 = new ShippingAddress("John", "Doe", "1234567890", "john@example.com", "123 Main St", null, "New York", "NY", "10001", "USA");
		var address2 = new ShippingAddress("John", "Doe", "1234567890", "john@example.com", "123 Main St", null, "New York", "NY", "10001", "USA");

		// Act & Assert
		address1.Equals(address2).Should().BeTrue();
		(address1 == address2).Should().BeTrue();
	}

	[Fact]
	public void Equals_WithDifferentValues_ReturnsFalse()
	{
		// Arrange
		var address1 = new ShippingAddress("John", "Doe", "1234567890", "john@example.com", "123 Main St", null, "New York", "NY", "10001", "USA");
		var address2 = new ShippingAddress("Jane", "Doe", "1234567890", "jane@example.com", "123 Main St", null, "New York", "NY", "10001", "USA");

		// Act & Assert
		address1.Equals(address2).Should().BeFalse();
		(address1 == address2).Should().BeFalse();
	}

	[Fact]
	public void Equals_WithNull_ReturnsFalse()
	{
		// Arrange
		var address = new ShippingAddress("John", "Doe", "1234567890", "john@example.com", "123 Main St", null, "New York", "NY", "10001", "USA");

		// Act & Assert
		address.Equals(null).Should().BeFalse();
	}

	[Fact]
	public void GetHashCode_SameValues_ReturnsSameHashCode()
	{
		// Arrange
		var address1 = new ShippingAddress("John", "Doe", "1234567890", "john@example.com", "123 Main St", null, "New York", "NY", "10001", "USA");
		var address2 = new ShippingAddress("John", "Doe", "1234567890", "john@example.com", "123 Main St", null, "New York", "NY", "10001", "USA");

		// Act & Assert
		address1.GetHashCode().Should().Be(address2.GetHashCode());
	}
}
