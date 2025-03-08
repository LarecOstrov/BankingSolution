namespace Banking.Domain.ValueObjects;

public record DepositRequest(Guid ToAccountId, decimal Amount);
