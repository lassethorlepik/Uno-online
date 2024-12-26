using ConsoleApp;
using DAL;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Web.Pages.CreateGame;

public class Index : PageModel
{
    private readonly AppDbContext _context;

    public readonly IGameRepository gameRepository;
    
    public RuleSet rules = new() { // init default rules
        DrawLimitIsOne = true,
        CanPassPlus2And4 = false,
        WildCards = 1,
        SpecialCards = 2,
        NumberCards = 20
    };

    public Index(AppDbContext context)
    {
        //throw new Exception(context.Games.FirstOrDefault()!.Id.ToString());
        _context = context;
        gameRepository = new GameRepository(_context);
    }
    
    public void OnGet()
    {
        
    }
    
}

