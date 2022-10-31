using System.ComponentModel.DataAnnotations.Schema;

namespace FinanceBot.Models;


public class Expense
{
    public int Id { get; set; }
    public long UserId { get; set; }
    public string Name { get; set; }
    public string ExpenseCategory { get; set; }
    public int Cost { get; set; }
    public DateTime Date { get; set; }
    
    public Expense(long userId, string name, int cost, string expenseCategory, DateTime date)
    {
        UserId = userId;
        Name = name;
        Cost = cost;
        ExpenseCategory = expenseCategory;
        Date = date;
    }
}