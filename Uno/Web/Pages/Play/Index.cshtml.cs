using ConsoleApp;
using DAL;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Web.Pages.Play;

public class Index : PageModel
{
    private readonly AppDbContext _context;

    public readonly IGameRepository gameRepository;

    public Index(AppDbContext context)
    {
        //throw new Exception(context.Games.FirstOrDefault()!.Id.ToString());
        _context = context;
        gameRepository = new GameRepository(_context);
    }

    [BindProperty(SupportsGet = true)]
    public Guid GameId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid PlayerId { get; set; }
    public GameContainer? Game { get; set; }
    public Domain.Player? CurrentFullPlayer { get; set; }
    [BindProperty(SupportsGet = true)]
    public String? Action { get; set; }
    public string? ErrorMessage { get; set; }
    public bool myTurn = false;

    public IActionResult OnGet()
    {
        var gameState = gameRepository.Load(GameId);

        Game = new GameContainer() 
        {
            state = gameState,
            runningInWeb = true
        };
        
        foreach (var player in Game!.state.Players) {
            if (player.Id == PlayerId) {
                CurrentFullPlayer = player;
                break;
            }
        }

        if (CurrentFullPlayer == null)
        {
            return Page();
        }

        if (Game!.state.CurrentPlayer.Id == CurrentFullPlayer!.Id)
        {
            myTurn = true;
        }
        
        if (Action == "stacking")
        {
            foreach (var player in Game.state.Players)
            {
                if (player.Id == CurrentFullPlayer!.Id)
                {
                    player.Stacking = !player.Stacking;
                    break;
                }
            }
            Game.SaveGame();
            return RedirectToPage(new { GameId = GameId, PlayerId = PlayerId, Action = "null" });
        }
        if (Action == "prefColor")
        {
            foreach (var player in Game.state.Players)
            {
                if (player.Id == CurrentFullPlayer!.Id)
                {
                    List<string> colors = new List<string> {"Red", "Yellow", "Blue", "Green"};
                    player.PrefColor = colors[(colors.IndexOf(player.PrefColor!) + 1) % colors.Count];
                    break;
                }
            }
            Game.SaveGame();
            return RedirectToPage(new { GameId = GameId, PlayerId = PlayerId, Action = "null" });
        }
        if (Action == "prefSwap")
        {
            foreach (var player in Game.state.Players)
            {
                if (player.Id == CurrentFullPlayer!.Id)
                {
                    if (Game.state.Players.Count == 1) // Only you remaining? Swap with yourself
                    {
                        player.PrefSwap = -1;
                    }
                    else
                    {
                        List<Player> choices = Game.state.Players.Where(p => p.Id != CurrentFullPlayer!.Id).ToList();
                        player.PrefSwap = (player.PrefSwap + 1) % choices.Count;
                        break; 
                    }
                }
            }
            Game.SaveGame();
            return RedirectToPage(new { GameId = GameId, PlayerId = PlayerId, Action = "null" });
        }
        
        if (Action != "null" && Action != null)
        {
            if (!myTurn || Game.gameover)
            {
                return RedirectToPage(new { GameId = GameId, PlayerId = PlayerId, Action = "null" });
            }
            if (Action == "draw")
            {
                Game.state.CurrentPlayerDrew = true;
                Game.AttemptCardDraw(CurrentFullPlayer!);
                Game.SaveGame();
                return RedirectToPage(new { GameId = GameId, PlayerId = PlayerId, Action = "null" });
            }
            if (Action == "end")
            {
                Game.TurnChange(true, 1); // prompt means user input is expected instead of virtual state changes
                Game.state.CurrentPlayerDrew = false; // reset drawing
                Game.SaveGame();
                return RedirectToPage(new { GameId = GameId, PlayerId = PlayerId, Action = "null" });
            }
            if (Action == "uno")
            {
                Game.state.CurrentPlayerCalledUno = true;
                Game.SaveGame();
                return RedirectToPage(new { GameId = GameId, PlayerId = PlayerId, Action = "null" });
            }
            try
            {
                
                if (CurrentFullPlayer.Hand.Count == 2 && !Game.state.CurrentPlayerCalledUno)
                {
                    Game.PlayCard(CurrentFullPlayer!, CurrentFullPlayer!.Hand[int.Parse(Action)]);
                    Game.AttemptCardDraw(CurrentFullPlayer!);
                    Game.AttemptCardDraw(CurrentFullPlayer!);
                    Game.SaveGame();
                }
                else
                {
                    Game.PlayCard(CurrentFullPlayer!, CurrentFullPlayer!.Hand[int.Parse(Action)]);
                }
                return RedirectToPage(new { GameId = GameId, PlayerId = PlayerId, Action = "null" });
            }
            catch (Exception e)
            {
                //ErrorMessage = e.ToString(); // debug message
            }
        }
        return Page();
    }

    public Card? GetLast()
    {
        return Game!.state.DiscardPile.Count == 0 ? null : Game.state.DiscardPile.Last();
    }

    public String GetSymbol(Card card)
    {
        string symbol = card.Type.ToString();
        if (symbol == "Number")
        {
            symbol = card.Number.ToString();
        }
        if (symbol == "Reverse")
        {
            symbol = "⮂";
        }
        if (symbol == "Skip")
        {
            symbol = "\u2298";
        }
        if (symbol == "Draw2")
        {
            symbol = "+2";
        }
        if (symbol == "Draw4")
        {
            symbol = "+4";
        }
        if (symbol == "Blank")
        {
            symbol = "";
        }
        if (symbol == "Wild")
        {
            symbol = "";
        }
        return symbol;
    }
    
}

