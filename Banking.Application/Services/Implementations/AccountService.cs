using Banking.Application.Repositories.Interfaces;
using Banking.Application.Services.Interfaces;

namespace Banking.Application.Implementations;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;
    public AccountService(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }
    public async Task<bool> IsAccountOwnerAsync(Guid accountId, Guid userId)
    {
        var account = await _accountRepository.GetByIdAsync(accountId);
        return account != null && account.UserId == userId;
    }

}
