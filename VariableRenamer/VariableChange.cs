namespace VariableRenamer;

public class VariableChange
{
    public string OldName { get; set; } = "";
    public string NewName { get; set; } = "";
    public string Type { get; set; } = "";
    public int Line { get; set; }
}