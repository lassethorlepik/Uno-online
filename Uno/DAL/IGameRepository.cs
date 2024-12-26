namespace DAL;
using Domain;

public interface IGameRepository {
    void Save(Game game);
    Game Load(Guid id);
    List<string> GetAllNames();
}