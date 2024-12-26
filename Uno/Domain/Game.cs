
namespace Domain;

public class Game
{
    public Guid Id { get; set; } = Guid.NewGuid(); // Primary key, identifies game
    public string GameName { get; set; } = "DEFAULT_NAME"; // Used to save/load/display save files by hashing it to Id
    public List<Player> Players { get; set; } = new (); // Stores players
    public RuleSet UsedRuleSet { get; set; } = new(); // Stores rules chosen at the start of the game

    public Deck GameDeck { get; set; } = new(); // Deck object, contains cards
    public Player CurrentPlayer { get; set; } = new(); // Player from Players with index Turn
    public List<Card> DiscardPile { get; set; } = new(); // graveyard, currently played pile
    public int Turn { get; set; } = 0; // index of player from players in match
    public bool IsReversed { get; set; } = false; // move turns backwards

    public int StackCounter { get; set; } = 1; // How many players are stacking cards currently
    public List<Player> Winners { get; set; } = new(); // Used to display leaderboard at the end of the match
    public int TotalTurnCounter { get; set; } = 0; // Counts turns, nothing special
    public string CreationTime { get; set; } = DateTime.Now.ToString("HH.mm dd.MM.yyyy"); // Game start time
    public bool CurrentPlayerCalledUno { get; set; } = false; // Holds uno status of current player
    public bool CurrentPlayerDrew { get; set; } = false; // If current player has drawn a card
}