namespace FinanceBot.Models;

public class UserExpenseCategory
{
    public int id { get; set; }
    public long userId { get; set; }
    public string expenseCategory { get; set; }
    
    public UserExpenseCategory(long userId, string expenseCategory)
    {
        this.userId = userId;
        this.expenseCategory = expenseCategory;
    }
}