namespace ProjectVanguard.Server.Models;

public class PlayerState
{
    public string ConnectionId {get; set;} = string.Empty;
    public string Name {get;set;} = string.Empty;
    public int HP {get; set;} = 100;
    public int MaxHP {get;set;} = 100;
    public int ActionPoints {get; set;} = 3;
    public int Initiative {get;set;} = 0;
    public bool IsAlive => HP > 0;
}