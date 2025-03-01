using Banking.Application.Repositories.Interfaces;
using Banking.Domain.Entities;
using Banking.Infrastructure.Database.Entities;
using HotChocolate.Authorization;
using Serilog;

namespace Banking.API.GraphQL
{
    [Authorize("Admin")]
    public class Query
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IUserRepository _userRepository;

        public Query(
            IAccountRepository accountRepository,
            ITransactionRepository transactionRepository,
            IUserRepository userRepository)
        {
            _accountRepository = accountRepository;
            _transactionRepository = transactionRepository;
            _userRepository = userRepository;
        }

        [UsePaging(IncludeTotalCount = true, MaxPageSize = 25)]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<AccountEntity> GetAccounts()
        {
            try
            {
                return _accountRepository.GetAll();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to get Accounts");
                throw new GraphQLException($"Failed to get Accounts: {ex.Message}");
            }
        }

        [UsePaging(IncludeTotalCount = true, MaxPageSize = 25)]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<TransactionEntity> GetTransactions()
        {
            try
            {
                return _transactionRepository.GetAll();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to get Transactions");
                throw new GraphQLException($"Failed to get Transactions: {ex.Message}");
            }
        }

        [UsePaging(IncludeTotalCount = true, MaxPageSize = 25)]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<UserEntity> GetUsers()
        {
            try
            {
                return _userRepository.GetAll();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to get Users");
                throw new GraphQLException($"Failed to get Users: {ex.Message}");
            }
        }
    }
}
