using System.ComponentModel.DataAnnotations.Schema;

namespace FinanceBot.Models;

public enum WorkMode
{
    AddCategory,
    EditCategory,
    RemoveCategory,
    Default
}
public class UserWorkMode
{
    public int Id { get; set; }
    public long UserId { get; set; }
    public WorkMode WorkMode { get; set; }

    public UserWorkMode(long userId, WorkMode workMode)
    {
        UserId = userId;
        WorkMode = workMode;
    }
}