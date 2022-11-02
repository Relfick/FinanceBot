namespace FinanceBot.Models;

public class UserExpenseCategory
{
    public int Id { get; set; }
    public long UserId { get; set; }
    public string ExpenseCategory { get; set; }
    
    public UserExpenseCategory(long userId, string expenseCategory)
    {
        UserId = userId;
        ExpenseCategory = expenseCategory;
    }
}