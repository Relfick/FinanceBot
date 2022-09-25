namespace FinanceBot.Models;

public enum ExpenseCategory
{
    Food,
    Clothes,
    Fun
}

public class Expense
{
    public int id { get; set; }
    public long userId { get; set; }
    public string name { get; set; }
    public ExpenseCategory expenseCategory { get; set; }
    public DateOnly date { get; set; }
    public TimeOnly time { get; set; }
    
    public Expense(int id, long userId, string name, ExpenseCategory expenseCategory, DateOnly date, TimeOnly time)
    {
        this.id = id;
        this.userId = userId;
        this.name = name;
        this.expenseCategory = expenseCategory;
        this.date = date;
        this.time = time;
    }

    
}