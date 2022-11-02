using Microsoft.EntityFrameworkCore;
using FinanceBot.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceBot.Models;

public sealed class ApplicationContext: DbContext
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Expense> Expenses { get; set; } = null!;
    public DbSet<UserExpenseCategory> UserExpenseCategories { get; set; } = null!;

    public ApplicationContext(DbContextOptions<ApplicationContext> options) 
        : base(options)
    {
        // Database.EnsureCreated();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(UserConfigure);
        modelBuilder.Entity<Expense>(ExpenseConfigure);
        modelBuilder.Entity<UserExpenseCategory>(UserExpenseCategoryConfigure);
    }

    private void UserConfigure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.Id).HasColumnName("id");
        builder.Property(u => u.FirstName).HasColumnName("first_name");
        builder.Property(u => u.Username).HasColumnName("username");
    }

    private void ExpenseConfigure(EntityTypeBuilder<Expense> builder)
    {
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e=> e.Name).HasColumnName("name");
        builder.Property(e=> e.ExpenseCategory).HasColumnName("expense_category");
        builder.Property(e=> e.Cost).HasColumnName("cost");
        builder.Property(e=> e.Date).HasColumnName("date");
    }
    
    private void UserExpenseCategoryConfigure(EntityTypeBuilder<UserExpenseCategory> builder)
    {
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.UserId).HasColumnName("user_id");
        builder.Property(c=> c.ExpenseCategory).HasColumnName("expense_category");
    }
}