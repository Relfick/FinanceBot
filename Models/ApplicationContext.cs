using Microsoft.EntityFrameworkCore;
using FinanceBot.Models;

namespace FinanceBot.Models;

public sealed class ApplicationContext: DbContext
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Expense> Expenses { get; set; } = null!;
    public DbSet<UserExpenseCategory> UserExpenseCategories { get; set; } = null!;
    public DbSet<UserWorkMode> UserWorkModes { get; set; } = null!;

    public ApplicationContext(DbContextOptions<ApplicationContext> options) 
        : base(options)
    {
        // Database.EnsureCreated();
    }
}