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
        if (match == null) return;

        await Clients.Group(matchId).SendAsync("PlayerJoinder", playerName);

        //if 2 player, start battle
        if(match != null && match.Players.Count == 2 && match.Status == "Waiting")
        {
            await StartMatch(match);
        }
        else if (match != null && match.Players.Count > 2)
        {
            // Opsi: Blokir pemain ke-3 agar tidak merusak permainan 1v1
            await Clients.Client(Context.ConnectionId).SendAsync("Error", "Ruangan sudah penuh!");
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

        _matchService.SaveMatch(match);

        await Clients.Group(match.MatchId).SendAsync("MatchStarted", match);
    }

    public async Task EndTurn(string matchId)
    {
        var match = _matchService.GetMatch(matchId);

        // PERBAIKAN: Beritahu klien kenapa aksinya ditolak
        if (match == null)
        {
            await Clients.Client(Context.ConnectionId).SendAsync("Error", "Data pertandingan hilang dari server (Redis kosong)!");
            return;
        }
        if (match.Status != "Ongoing")
        {
            await Clients.Client(Context.ConnectionId).SendAsync("Error", $"Tidak bisa aksi! Status pertandingan: {match.Status}");
            return;
        }

        if (match == null || match.Status != "Ongoing") return;

        // validate for anti-cheat
        if (match.CurrentTurnPlayerId != Context.ConnectionId)
        {
            await Clients.Client(Context.ConnectionId).SendAsync("Error", "Not your turn!");
            return;
        }

        // search index for player after turn
        int currentIndex = match.Players.FindIndex(p => p.ConnectionId == match.CurrentTurnPlayerId);
        string endingPlayerName = match.Players[currentIndex].Name;

        // move turn to next player
        int nextIndex = (currentIndex + 1) % match.Players.Count;
        match.CurrentTurnPlayerId = match.Players[nextIndex].ConnectionId;
        string nextPlayerName = match.Players[nextIndex].Name;

        // reset action points for next player turn
        match.Players[nextIndex].ActionPoints = 3;

        if (nextIndex == 0) match.RoundNumber++; //round will add if back to first player

        _matchService.SaveMatch(match);

        await Clients.Group(matchId).SendAsync("BattleLog", $"{endingPlayerName} end the turn.\nNow {nextPlayerName} Turn!");

        // tell all client that state changed
        await Clients.Group(matchId).SendAsync("TurnEnded", match);
    }

    public async Task Attack(string matchId)
    {
        var match = _matchService.GetMatch(matchId);

        if (match == null)
        {
            await Clients.Client(Context.ConnectionId).SendAsync("Error", "Data pertandingan hilang dari server!");
            return;
        }
        if (match.Status != "Ongoing")
        {
            await Clients.Client(Context.ConnectionId).SendAsync("Error", $"Tidak bisa menyerang! Status pertandingan: {match.Status}");
            return;
        }

        if (match == null || match.Status != "Ongoing") return;

        // 1. Validasi Giliran (Anti-Cheat)
        if (match.CurrentTurnPlayerId != Context.ConnectionId)
        {
            await Clients.Client(Context.ConnectionId).SendAsync("Error", "Bukan giliranmu!");
            return;
        }

        var attacker = match.Players.Find(p => p.ConnectionId == Context.ConnectionId);
        var defender = match.Players.Find(p => p.ConnectionId != Context.ConnectionId);

        if (attacker == null || defender == null) return;

        // 3. Validasi Action Points (AP)
        if (attacker.ActionPoints <= 0)
        {
            await Clients.Client(Context.ConnectionId).SendAsync("Error", "Action Points (AP) tidak cukup!");
            return;
        }

        attacker.ActionPoints -= 1;
        int damage = Random.Shared.Next(1, 9); // Menghasilkan angka 1 sampai 8
        defender.HP -= damage;

        await Clients.Group(matchId).SendAsync("BattleLog", $"⚔️ {attacker.Name} menyerang {defender.Name} dan memberikan {damage} damage!");

        if (defender.HP <= 0)
        {
            defender.HP = 0;
            match.Status = "Finished";

            _matchService.SaveMatch(match);

            await Clients.Group(matchId).SendAsync("BattleLog", $"💀 {defender.Name} telah gugur! {attacker.Name} MENANG!");
            await Clients.Group(matchId).SendAsync("MatchEnded", match);
            return;
        }

        _matchService.SaveMatch(match);

        await Clients.Group(matchId).SendAsync("MatchUpdated", match);
    }
}