namespace Banking.Domain.ValueObjects;

public record TransferRequest(Guid FromAccountId, Guid ToAccountId, decimal Amount);
