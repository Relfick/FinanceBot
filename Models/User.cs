namespace FinanceBot.Models;

public class User
{
    public User(ulong id, string first_name, string username)
    {
        this.id = id;
        this.first_name = first_name;
        this.username = username;
    }

    public ulong id { get; set; }
    public string first_name { get; set; }
    public string username { get; set; }
}