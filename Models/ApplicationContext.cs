using Microsoft.EntityFrameworkCore;

namespace FinanceBot.Models;

public class ApplicationContext: DbContext
{
    private const string ConnectionString = 
        "Server=MYSQL8001.site4now.net;port=3306;Database=db_a8d48b_tg;password=R6vS#Jp_SC2VC!5;uid=a8d48b_tg";
    
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Expense> Expenses { get; set; } = null!;

    public ApplicationContext()
    {
        // Database.EnsureCreated();
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseMySql(
            ConnectionString,
            ServerVersion.AutoDetect(ConnectionString));
    } 
}