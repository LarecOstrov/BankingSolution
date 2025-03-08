namespace Banking.Domain.ValueObjects
{
    public record LoginResponse(string AccessToken, string RefreshToken);
}
