using System.Text.Json;
using ProjectVanguard.Server.Models;
using StackExchange.Redis;

namespace ProjectVanguard.Server.Services;

public class MatchService
{
    private readonly IDatabase _db;

    public MatchService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public void SaveMatch(MatchState match)
    {
        string json = JsonSerializer.Serialize(match);
        _db.StringSet(match.MatchId, json);
    }

    public MatchState GetOrCreateMatch(string matchId)
    {
        var redisValue = _db.StringGet(matchId);
        if (redisValue.HasValue)
        {

            string jsonText = redisValue.ToString();

            return JsonSerializer.Deserialize<MatchState>(jsonText) ?? new MatchState {MatchId = matchId};
        }

        var newMatch = new MatchState {MatchId = matchId};
        SaveMatch(newMatch);

        return newMatch;
    }

    public void AddPlayerToMatch(string matchId, PlayerState player)
    {
        var match = GetOrCreateMatch(matchId);


        // prevent duplicate player
        if(!match.Players.Any(p => p.ConnectionId == player.ConnectionId))
        {
            match.Players.Add(player);
            SaveMatch(match);
        }
    }

    public MatchState? GetMatch(string matchId)
    {
        var redisValue = _db.StringGet(matchId);
        if (redisValue.HasValue)
        {
            string jsonText = redisValue.ToString();
            return JsonSerializer.Deserialize<MatchState>(jsonText);
        }
        return null;
    }
}