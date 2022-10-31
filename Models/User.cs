using System.ComponentModel.DataAnnotations.Schema;

namespace FinanceBot.Models;

public class User
{
    public User(long id, string firstName, string username)
    {
        Id = id;
        FirstName = firstName;
        Username = username;
    }

    public long Id { get; set; }
    public string FirstName { get; set; }
    public string Username { get; set; }
}