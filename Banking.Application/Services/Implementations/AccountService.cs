using Banking.Application.Repositories.Interfaces;
using Banking.Application.Services.Interfaces;
using Banking.Infrastructure.Config;
using Microsoft.Extensions.Options;
using System.Numerics;
using System.Text;

namespace Banking.Application.Implementations;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly BankInfo _bankInfo;
    private readonly Random _random = new Random();
    private readonly int _accountLength;
    private readonly string _countryCode;
    private readonly string _bankCode;
    public AccountService(IAccountRepository accountRepository,
        IOptions<BankInfo> bankInfo)
    {
        _accountRepository = accountRepository;
        _bankInfo = bankInfo.Value;
        _accountLength = _bankInfo.AccountLength;
        _countryCode = _bankInfo.Country;
        _bankCode = _bankInfo.Code;

    }
    public async Task<bool> IsAccountOwnerAsync(Guid accountId, Guid userId)
    {
        var account = await _accountRepository.GetByIdAsync(accountId);
        return account != null && account.UserId == userId;
    }

    public async Task<string> GenerateUniqueIBANAsync()
    {
        if (_countryCode.Length != 2 || !_countryCode.All(char.IsLetter))
            throw new ArgumentException("Invalid country code. Must be 2 letters.");

        if (!_bankCode.All(char.IsDigit))
            throw new ArgumentException("Invalid bank code. Must be numeric.");

        string iban;
        bool exists;

        do
        {
            var accountNumber = new string(Enumerable.Repeat("0123456789", _accountLength)
                                    .Select(s => s[_random.Next(s.Length)]).ToArray());

            string ibanWithoutChecksum = _countryCode + "00" + _bankCode + accountNumber;

            string checksum = CalculateIBANChecksum(ibanWithoutChecksum);

            iban = _countryCode + checksum + _bankCode + accountNumber;

            exists = await _accountRepository.ExistsAsync(iban);

        } while (exists);

        return iban;
    }

    #region Private Methods
    private static string CalculateIBANChecksum(string ibanWithoutChecksum)
    {

        string countryDigits = ConvertLettersToDigits(ibanWithoutChecksum.Substring(0, 2));


        string numericIban = ibanWithoutChecksum.Substring(4) + countryDigits + "00";


        BigInteger ibanNumber = BigInteger.Parse(numericIban);
        int checksum = 98 - (int)(ibanNumber % 97);

        return checksum.ToString("D2");
    }

    private static string ConvertLettersToDigits(string input)
    {
        var sb = new StringBuilder();
        foreach (char c in input)
        {
            int value = char.ToUpper(c) - 'A' + 10;
            sb.Append(value);
        }
        return sb.ToString();
    }
    #endregion
}
