namespace Banking.Application.Services.Interfaces;

public interface IAccountService
{
    Task<bool> IsAccountOwnerAsync(Guid accountId, Guid userId);
    Task<string> GenerateUniqueIBANAsync();
}
