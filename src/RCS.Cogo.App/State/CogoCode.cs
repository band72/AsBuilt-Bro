namespace RCS.Cogo.App.State;

public class CogoCode
{
    public string LocalCode { get; set; } = string.Empty;
    public string SystemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public CogoCode() { }

    public CogoCode(string local, string system, string desc)
    {
        LocalCode = local;
        SystemCode = system;
        Description = desc;
    }
}
