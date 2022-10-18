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
    public int id { get; set; }
    public long userId { get; set; }
    public WorkMode workMode { get; set; }

    public UserWorkMode(long userId, WorkMode workMode)
    {
        this.userId = userId;
        this.workMode = workMode;
    }
}