namespace FinanceBot.Models;

public class User
{
    public User(long id, string first_name, string username)
    {
        this.id = id;
        this.first_name = first_name;
        this.username = username;
    }

    public long id { get; set; }
    public string first_name { get; set; }
    public string username { get; set; }
}