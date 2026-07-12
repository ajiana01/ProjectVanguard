using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json;

Console.WriteLine("=== PROJECT VANGURD: TERMINAL CLIENT ===");

// define url server
Console.Write("Input Port Server: ");
string port = Console.ReadLine() ?? "5053";

var connection = new HubConnectionBuilder()
    .WithUrl($"http://localhost:{port}/battlehub")
    .Build();

// Register to hear messages from the server
connection.On("PlayerJoinded", (string name) =>
{
    Console.WriteLine($"\n[SERVER] {name} join the battle!");
});

connection.On("MatchStarted", (JsonElement matchState) =>
{
    Console.WriteLine("\n[SERVER] MATCH BEGINS! (Initiative Roll Completed)");
    Console.WriteLine($"> First Turn: {matchState.GetProperty("currentTurnPlayerId").GetString()}");
    Console.WriteLine("> Type 'end' for end your turn.");
});

connection.On("TurnChanged", (JsonElement matchState) =>
{
    Console.WriteLine($"\n[SERVER] CHANGE TURN! (Round {matchState.GetProperty("roundNumber").GetInt32()})");
    Console.WriteLine($"> Current Turn: {matchState.GetProperty("currentTurnPlayerId").GetString()}");
});

connection.On("Error", (string message) =>
{
    Console.WriteLine($"\n[WARNING] {message}");
});

connection.On("BattleLog", (string message) =>
{
    Console.WriteLine($"\n[BATTLE LOG] {message}");
});

// Mendengarkan update HP & AP setelah ada serangan
connection.On("MatchUpdated", (JsonElement matchState) =>
{
    Console.WriteLine("\n[STATUS UPDATE]");
    // JSON dari SignalR otomatis mengubah huruf depan menjadi kecil (camelCase)
    var players = matchState.GetProperty("players").EnumerateArray();
    foreach (var p in players)
    {
        string name = p.GetProperty("name").GetString() ?? "Unknown";
        int hp = p.GetProperty("hp").GetInt32();
        int ap = p.GetProperty("actionPoints").GetInt32();
        
        Console.WriteLine($"- {name} | HP: {hp} | AP: {ap}");
    }
});

// Mendengarkan jika game selesai (ada yang mati)
connection.On("MatchEnded", (JsonElement matchState) =>
{
    Console.WriteLine("\n=== PERTANDINGAN SELESAI ===");
    Console.WriteLine("Tekan Ctrl+C untuk keluar.");
});



// Mulai Koneksi
try
{
    await connection.StartAsync();
    Console.WriteLine($"\nSucceeded connect the server! (Your ID: {connection.ConnectionId})");
}
catch (Exception ex)
{
    Console.WriteLine($"Can't connect: {ex.Message}");
    return;
}

Console.Write("\nInput Match ID (ex: Room1): ");
string matchId = Console.ReadLine() ?? "Room1";

Console.Write("Input your character name: ");
string playerName = Console.ReadLine() ?? "Player1";

await connection.InvokeAsync("JoinMatch", matchId, playerName);

while (true)
{
    string? input = Console.ReadLine();
    
    if (input?.ToLower() == "end")
    {
        try 
        {
            // Coba kirim perintah ke server
            await connection.InvokeAsync("EndTurn", matchId);
        }
        catch (Exception ex)
        {
            // Jika server menolak (atau ada error), tangkap dan tampilkan dengan aman
            Console.WriteLine($"[SYSTEM ERROR] Gagal mengirim perintah: {ex.Message}");
        }
    }   
    else if (input?.ToLower() == "attack")
    {
        try { await connection.InvokeAsync("Attack", matchId); }
        catch (Exception ex) { Console.WriteLine($"[SYSTEM ERROR] {ex.Message}"); }
    }
}