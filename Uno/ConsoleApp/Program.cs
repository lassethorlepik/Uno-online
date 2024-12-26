using ConsoleApp;
using Domain;
Console.CursorVisible = false;
Console.WriteLine("UNO");
// Total hours of precious life wasted: ~93
// If God wanted clean code, he would not have created me.

Menu.PromptMenu(); // Start main menu

public static class Main {
    public static void StartNewGame(List<string> playerNames, int aiPlayers, RuleSet ruleset, bool startGame) {
        List<Player> players = InitializePlayers(playerNames, aiPlayers);
        string gameName = GenerateGameName(); //GetGameName();

        GameContainer gameContainer = new(gameName, players, ruleset, startGame); // Start the game
    }

    private static List<Player> InitializePlayers(List<string> playerNames, int aiPlayers) {
        List<Player> players = new();
        int aiCounter = 1;

        foreach (var name in playerNames) {
            players.Add(new Player {
                Name = name,
                IsAi = false,
                Hand = new List<Card>(),
                Id = Guid.NewGuid()
            });
        }
        for (int i = 0; i < aiPlayers; i++) {
            players.Add(new Player {
                Name = $"CPU-{aiCounter++}",
                IsAi = true,
                Hand = new List<Card>(),
                Id = Guid.NewGuid()
            });
        }

        return players;
    }

    private static string GetGameName() {
        Console.WriteLine("\nEnter game name:\n");
        string? input = Console.ReadLine();
        return string.IsNullOrWhiteSpace(input) ? "DEFAULT_NAME" : input;
    }
    
    private static string GenerateGameName() {
        return "Game " + new Random().Next(0, 10001).ToString().PadLeft(5, '0') + " from " + DateTime.Now.ToString("HH.mm dd.MM.yyyy");
    }
}