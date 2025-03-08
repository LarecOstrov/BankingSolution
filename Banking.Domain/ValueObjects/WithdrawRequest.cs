namespace Banking.Domain.ValueObjects;

public record WithdrawRequest(Guid FromAccountId, decimal Amount);
