using Microsoft.AspNetCore.SignalR;
using ProjectVanguard.Server.Models;
using ProjectVanguard.Server.Services;

namespace ProjectVanguard.Server.Hub;

public class BattleHub : Microsoft.AspNetCore.SignalR.Hub
{
    private readonly MatchService _matchService;

    public BattleHub(MatchService matchService)
    {
        _matchService = matchService;
    }

    public async Task JoinMatch(string matchId, string playerName)
    {
        // join network SignalR
        await Groups.AddToGroupAsync(Context.ConnectionId, matchId);

        //record state player in server memory
        var player = new PlayerState
        {
            ConnectionId = Context.ConnectionId,
            Name = playerName,
            HP = 100,
            MaxHP = 100,
            ActionPoints = 3
        };
        _matchService.AddPlayerToMatch(matchId, player);

        var match = _matchService.GetMatch(matchId);
        await Clients.Group(matchId).SendAsync("PlayerJoinder", playerName);

        //if 2 player, start battle
        if(match != null && match.Players.Count == 2 && match.Status == "Waiting")
        {
            await StartMatch(match);
        }
    }

    private async Task StartMatch(MatchState match)
    {
        match.Status = "Ongoing";

        //roll initiative each player
        foreach (var player in match.Players)
        {
            player.Initiative = Random.Shared.Next(1,21); //RNG Dice 1-20
        }

        //Sort inititive by descending
        match.Players = match.Players.OrderByDescending(p => p.Initiative).ToList();

        // player with high initiative turn first
        match.CurrentTurnPlayerId = match.Players[0].ConnectionId;

        await Clients.Group(match.MatchId).SendAsync("MatchStarted", match);
    }

    public async Task EndTurn(string matchId, string playerName)
    {
        var match = _matchService.GetMatch(matchId);
        if (match == null || match.Status != "Ongoing") return;

        // validate for anti-cheat
        if (match.CurrentTurnPlayerId != Context.ConnectionId)
        {
            await Clients.Client(Context.ConnectionId).SendAsync("Error", "Not your turn!");
            return;
        }

        // search index for player after turn
        int currentIndex = match.Players.FindIndex(p => p.ConnectionId == match.CurrentTurnPlayerId);

        // move turn to next player
        int nextIndex = (currentIndex + 1) % match.Players.Count;
        match.CurrentTurnPlayerId = match.Players[nextIndex].ConnectionId;

        // reset action points for next player turn
        match.Players[nextIndex].ActionPoints = 3;

        if (nextIndex == 0) match.RoundNumber++; //round will add if back to first player

        // tell all client that state changed
        await Clients.Group(matchId).SendAsync("TurnEnded", playerName);
    }
}