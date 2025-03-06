using Banking.Application.Repositories.Interfaces;
using Banking.Domain.Entities;
using Banking.Infrastructure.Database.Entities;
using HotChocolate.Authorization;
using Serilog;

namespace Banking.API.GraphQL
{
    [Authorize(Roles = new[] { "Admin" })]
    public class Query
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IFailedTransactionRepository _failedTransactionRepository;
        private readonly IBalanceHistoryRepository _balanceHistoryRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;


        public Query(
            IAccountRepository accountRepository,
            ITransactionRepository transactionRepository,
            IFailedTransactionRepository failedTransactionRepository,
            IBalanceHistoryRepository balanceHistoryRepository,
            IUserRepository userRepository,
            IRoleRepository roleRepository)
        {
            _accountRepository = accountRepository;
            _transactionRepository = transactionRepository;
            _failedTransactionRepository = failedTransactionRepository;
            _balanceHistoryRepository = balanceHistoryRepository;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
        }

        [GraphQLName("getAccounts")]
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

        [GraphQLName("getTransactions")]
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

        [GraphQLName("getUsers")]
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

        [GraphQLName("getFailedTransactions")]
        [UsePaging(IncludeTotalCount = true, MaxPageSize = 25)]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<FailedTransactionEntity> GetFailedTransactions()
        {
            try
            {
                return _failedTransactionRepository.GetAll();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to get Failed Transactions");
                throw new GraphQLException($"Failed to get Failed Transactions: {ex.Message}");
            }
        }

        [GraphQLName("getBalanceHistories")]
        [UsePaging(IncludeTotalCount = true, MaxPageSize = 25)]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<BalanceHistoryEntity> GetBalanceHistories()
        {
            try
            {
                return _balanceHistoryRepository.GetAll();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to get Balance Histories");
                throw new GraphQLException($"Failed to get Balance Histories: {ex.Message}");
            }
        }

        [GraphQLName("getRoles")]
        [UsePaging(IncludeTotalCount = true, MaxPageSize = 25)]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<RoleEntity> GetRoles()
        {
            try
            {
                return _roleRepository.GetAll();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to get Roles");
                throw new GraphQLException($"Failed to get Roles: {ex.Message}");
            }
        }
    }
}
