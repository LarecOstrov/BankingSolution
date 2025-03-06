namespace Banking.Domain.ValueObjects;

public record RegisterRequest(string FullName, string Email, string Password, string Role);
