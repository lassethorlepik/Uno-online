using ConsoleApp;
using DAL;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Web.Pages.CreateNewGame;

public class Index : PageModel
{
    private readonly AppDbContext _context;

    public readonly IGameRepository gameRepository;
    public GameContainer? Game { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public RuleSet? Rules { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? Names { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public int AiCount { get; set; }

    public Index(AppDbContext context)
    {
        _context = context;
        gameRepository = new GameRepository(_context);
    }
    
    public IActionResult OnGet()
    {
        List<string> namesList;
        if (Names is null)
        {
            namesList = new List<string>();
        }
        else
        {
            namesList = Names!.Split(',')
                .Select(s => s.Trim())
                .ToList();
            // false so game engine does not try to play it, we only want init and a save.
        }
        
        Main.StartNewGame(namesList, AiCount, Rules!, false);
        return Redirect("Games");
    }

}

