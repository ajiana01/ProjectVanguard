namespace ProjectVanguard.Server.Models;

public class MatchState
{
    public string MatchId {get; set;} = string.Empty;
    public List<PlayerState> Players {get; set;} = new();

    //tracking current player turn
    public string CurrentTurnPlayerId {get;set;} = string.Empty;
    public int RoundNumber {get;set;} = 1;
    
    // battle status: Waiting, Ongoing, Finished
    public string Status {get;set;} = "Waiting";

}