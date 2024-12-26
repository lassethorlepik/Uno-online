using ConsoleApp;
using DAL;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Web.Pages.JoinAs;

public class Index : PageModel
{
    private readonly AppDbContext _context;

    public readonly IGameRepository gameRepository;
    
    [BindProperty(SupportsGet = true)]
    public Guid GameId { get; set; }
    public GameContainer? Game { get; set; }

    public Index(AppDbContext context)
    {
        _context = context;
        gameRepository = new GameRepository(_context);
    }
    
    public void OnGet()
    {
        var gameState = gameRepository.Load(GameId);
        Game = new GameContainer() 
        {
            state = gameState
        };
    }
    
}

