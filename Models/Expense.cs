﻿namespace FinanceBot.Models;

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
    public int cost { get; set; }
    public DateTime date { get; set; }
    
    public Expense(long userId, string name, int cost, ExpenseCategory expenseCategory, DateTime date)
    {
        this.userId = userId;
        this.name = name;
        this.cost = cost;
        this.expenseCategory = expenseCategory;
        this.date = date;
    }

    
}