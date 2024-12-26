using System.Text.Json;
using Domain.DB;
namespace DAL;

public class GameRepository : IGameRepository
{
    private readonly AppDbContext _dbContext;

    public GameRepository(AppDbContext context)
    {
        _dbContext = context;
    }

    public void Save(Domain.Game game)
    {
        var existingGame = _dbContext.Games.FirstOrDefault(g => g.Id == game.Id);
        if (existingGame == null) {
            var newGame = new Game()
            {
                Id = game.Id,
                State = JsonSerializer.Serialize(game),
            };
            _dbContext.Games.Add(newGame);
        }
        else {
            existingGame.UpdatedAtDt = DateTime.Now;
            existingGame.State = JsonSerializer.Serialize(game);
        }
        var changesSaved = _dbContext.SaveChanges();
    }

    public Domain.Game Load(Guid id)
    {
        var game = _dbContext.Games.FirstOrDefault(g => g.Id == id);
        if (game == null) {
            throw new Exception("Game not found in database. ID:" + id);
        }
        return JsonSerializer.Deserialize<Domain.Game>(game.State) ?? throw new Exception("Error deserializing game state.");
    }

    public List<string> GetAllNames()
    {
        var gameNames = new List<string>();
        foreach (var game in _dbContext.Games)
        {
            // not the best solution, processing whole games for names
            var deserializedGame = JsonSerializer.Deserialize<Domain.Game>(game.State);
            if (deserializedGame == null) {
                throw new Exception("Error deserializing game state when reading names.");
            }
            gameNames.Add(deserializedGame.GameName);
        }
        return gameNames;
    }
}