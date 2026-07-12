# ⚔️ Project Vanguard: Server-Authoritative Multiplayer Backend

![.NET](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white)
![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)
![SignalR](https://img.shields.io/badge/SignalR-0078D4?style=for-the-badge&logo=microsoft&logoColor=white)
![Redis](https://img.shields.io/badge/redis-%23DD0031.svg?style=for-the-badge&logo=redis&logoColor=white)

Project Vanguard is a robust, server-authoritative multiplayer backend designed for turn-based tactical roguelite games (inspired by D&D mechanics). Built with **.NET / C#**, it uses **SignalR** for real-time bidirectional communication and **Redis** for high-speed, crash-resistant state persistence.

## ✨ Key Features (Engineering Highlights)

*   **🛡️ Server-Authoritative Architecture (Anti-Cheat):** The client only sends "intents" (e.g., "I want to attack"). The server validates Action Points (AP), rolls the RNG for damage, and broadcasts the official state. Clients cannot manipulate HP or turn orders.
*   **💾 Redis State Persistence:** Match states are strictly serialized and stored in Redis. If the .NET server crashes or restarts, ongoing battles are fully recovered without data loss.
*   **⚡ Real-Time Battle Log:** Broadcasts synchronized combat events to all connected clients instantly via SignalR Hubs.
*   **🎮 Console Client Simulator:** Includes a standalone C# Terminal Client for integration testing without needing the Unity game engine overhead.

## 🏗️ Architecture Overview

The system is strictly divided to ensure separation of concerns:
1.  **`BattleHub.cs`**: The SignalR entry point. Acts as the Dungeon Master, receiving client intents, validating turns, and broadcasting results.
2.  **`MatchService.cs`**: The core business logic and state manager. It acts as a bridge between the volatile server memory and the persistent Redis database.
3.  **`ProjectVanguard.Client`**: A text-based client to simulate multi-client concurrency and test graceful error handling.

## 🚀 Getting Started

### Prerequisites
*   [.NET 8.0 SDK](https://dotnet.microsoft.com/) (or newer)
*   [Redis Server](https://redis.io/download) running locally on port `6379`.

### Installation & Running Locally

1. **Start the Redis Server** (Linux):
   ```bash
    sudo systemctl start redis-server
   ```
2. Clone and Run the Server:
   ```bash
   git clone [https://github.com/ajiana01/ProjectVanguard.git](https://github.com/ajiana01/ProjectVanguard.git)
   cd ProjectVanguard/ProjectVanguard.Server
   dotnet run
   ```
3. Run the Client Simulator (Open a new terminal):
   ```bash
   cd ProjectVanguard/ProjectVanguard.Client
   dotnet run
   ```
   Note: Open two client terminals, join the same Match ID (e.g., `Room1`), and enjoy the text-based battle!

## 🧠 Technical Challenges Solved

During development, several critical challenges were addressed:
* **State Desync Mitigation:** Resolved race conditions where the SignalR broadcast occurred before the Redis database finalized the turn transition.
* **Graceful Error Handling:** Handled `Silent Failures` by implementing strict state null-checking and returning explicit error messages (e.g., "Not your turn!") to prevent the client application from crashing.
* **Lambda Type Inference:** Corrected C# delegate type resolution issues when binding SignalR listeners dynamically.

Developed by Aji Anomali
