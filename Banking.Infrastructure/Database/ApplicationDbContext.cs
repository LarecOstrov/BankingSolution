using Banking.Domain.Entities;
using Banking.Infrastructure.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banking.Infrastructure.Database;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<UserEntity> Users { get; set; }
    public DbSet<AccountEntity> Accounts { get; set; }
    public DbSet<TransactionEntity> Transactions { get; set; }
    public DbSet<BalanceHistoryEntity> BalanceHistory { get; set; }
    public DbSet<RefreshTokenEntity> RefreshTokens { get; set; }
    public DbSet<RoleEntity> Roles { get; set; }
    public DbSet<FailedTransactionEntity> FailedTransactions { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountEntity>()
            .HasIndex(a => a.AccountNumber)
            .IsUnique();

        modelBuilder.Entity<AccountEntity>()
            .HasOne(a => a.User)
            .WithMany(u => u.Accounts)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TransactionEntity>()
            .HasOne(t => t.FromAccount)
            .WithMany(a => a.TransactionsFrom)
            .HasForeignKey(t => t.FromAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TransactionEntity>()
            .HasOne(t => t.ToAccount)
            .WithMany(a => a.TransactionsTo)
            .HasForeignKey(t => t.ToAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BalanceHistoryEntity>()
            .HasOne(bh => bh.Transaction)
            .WithOne()
            .HasForeignKey<BalanceHistoryEntity>(bh => bh.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BalanceHistoryEntity>()
            .HasOne(bh => bh.Account)
            .WithMany()
            .HasForeignKey(bh => bh.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RefreshTokenEntity>()
            .HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserEntity>()
            .HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TransactionEntity>()
            .Property(t => t.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<AccountEntity>()
            .Property(a => a.Balance)
            .HasPrecision(18, 2);

        modelBuilder.Entity<BalanceHistoryEntity>()
            .Property(bh => bh.NewBalance)
            .HasPrecision(18, 2);

        modelBuilder.Entity<UserEntity>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<AccountEntity>()
            .HasIndex(u => u.AccountNumber)
            .IsUnique();
    }


}
