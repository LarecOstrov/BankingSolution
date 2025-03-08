namespace Banking.Domain.ValueObjects;

public class TransactionNotificationEvent
{
    public Guid? FromUserId { get; set; }
    public Guid? ToUserId { get; set; }
    public decimal Amount { get; set; }
    public string? FromAccountNumber { get; set; }
    public string? ToAccountNumber { get; set; }
    public string? FromUserName { get; set; }
    public string? ToUserName { get; set; }
    public decimal? FromAccountBalance { get; set; }
    public decimal? ToAccountBalance { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
