using System.Collections.Concurrent;
using ProjectVanguard.Server.Models;

namespace ProjectVanguard.Server.Services;

public class MatchService
{
    private readonly ConcurrentDictionary<string, MatchState> _activeMatches = new();

    public MatchState GetOrCreateMatch(string matchId)
    {
        return _activeMatches.GetOrAdd(matchId, new MatchState{MatchId = matchId});    
    }

    public void AddPlayerToMatch(string matchId, PlayerState player)
    {
        var match = GetOrCreateMatch(matchId);


        // prevent duplicate player
        if(!match.Players.Any(p => p.ConnectionId == player.ConnectionId))
        {
            match.Players.Add(player);
        }
    }

    public MatchState? GetMatch(string matchId)
    {
        _activeMatches.TryGetValue(matchId, out var match);
        return match;
    }
}