namespace ConsoleApp;
using System.Text.Json;
using Domain;

public static class SaveHandler {
    public const string FileExtension = ".unosave";
    
    public static void Save(GameContainer gameContainer) {
        JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
        string saveGame = JsonSerializer.Serialize(gameContainer.state, options); // serialize current game to json
        string path = "saves/Game " + gameContainer.state.CreationTime + ".unosave";
        // Writing the save to a file
        File.WriteAllText(path, saveGame);
    }

    public static Game Load(string filename) {
        if (filename.EndsWith(".unosave")) {
            // Reading the contents of the file
            string readText = File.ReadAllText("saves/" + filename);
            Game? loadedGame = JsonSerializer.Deserialize<Game>(readText);
            if (loadedGame != null) {
                return loadedGame;
            } else {
                throw new Exception("Load failure: result was null.");
            }
        } else {
            throw new Exception("Attempted to load unexpected file.");
        }
    }
}