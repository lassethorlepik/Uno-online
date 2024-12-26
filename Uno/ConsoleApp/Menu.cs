namespace ConsoleApp;
using Microsoft.Extensions.DependencyInjection;
using Domain;
using DAL;
using Microsoft.EntityFrameworkCore;

public class Menu {
    public static int selectionIndex; // Index of UI row selected in menu, starts from top at 0
    private static readonly string MenuText = ChooseMenuText(); // Choose a random slogan for main menu
    private static readonly RuleSet Ruleset = new(); // Init default ruleset
    private static readonly RefInt PlayerCount = new(1); // Player count is set to 1 by default
    private static readonly RefInt AICount = new(1); // Ai count is set to 1 by default
    private static readonly RefInt WildCards = new(1); // How many rounds to add to starting deck
    private static readonly RefInt SpecialCards = new(2); // How many rounds to add to starting deck
    private static readonly RefInt NumberCards = new(20); // How many rounds to add to starting deck
    private static readonly RefInt DrawLimitIsOne = new(1); // Set draw limit on by default (bool as int for "AssignVariable") 
    private static readonly RefInt CanPassPlus2And4 = new(0); // Set stacking off by default (bool as int for "AssignVariable") 
    public static readonly RefInt useDatabase = new(1); // DATABASE/JSON TOGGLE, also used in GameLogic
    public static MenuPage currentPage = CreateMenuStructure(); // Generate menu
    private static readonly List<string> GameNames = new(); // Used to hold names of all games (read from DB)
    public static string selectedFilename = "NULL"; // Temp variable to hold name of the game, that player chose to load.

    private static MenuPage CreateMenuStructure() {
        MenuPage saves = new MenuPage();

        MenuPage rules = new MenuPage();
        rules.AddMenuItem(new MenuItem("Wild card sets in deck", ClickOnVar).AssignVariable(WildCards, 0, 100));
        rules.AddMenuItem(new MenuItem("Special card sets in deck", ClickOnVar).AssignVariable(SpecialCards, 0, 100));
        rules.AddMenuItem(new MenuItem("Number card sets in deck", ClickOnVar).AssignVariable(NumberCards, 0, 100));
        rules.AddMenuItem(new MenuItem("Draw limit 1 card", ClickOnVar).AssignVariable(DrawLimitIsOne, 0, 1));
        rules.AddMenuItem(new MenuItem("Stacking +2/+4", ClickOnVar).AssignVariable(CanPassPlus2And4, 0, 1));

        MenuPage settings = new MenuPage();
        settings.AddMenuItem(new MenuItem("Human players", ClickOnVar).AssignVariable(PlayerCount, 1, 10));
        settings.AddMenuItem(new MenuItem("AI players", ClickOnVar).AssignVariable(AICount, 0, 10));
        settings.AddMenuItem(new MenuItem("Rules", Navigate, rules));
        settings.AddMenuItem(new MenuItem("Use DB", ClickOnVar).AssignVariable(useDatabase, 0, 1));

        MenuPage mainMenu = new MenuPage();
        mainMenu.AddMenuItem(new MenuItem("New Game", PromptNames));
        mainMenu.AddMenuItem(new MenuItem("Load Game", PromptLoad, saves));
        mainMenu.AddMenuItem(new MenuItem("Settings", Navigate, settings));
        mainMenu.AddMenuItem(new MenuItem("Exit", ExitGame));
        
        return mainMenu;
    }

    private static void ClickOnVar() {
        // What to do when user selects the highlighted variable in a menu
    }

    private static void Navigate() {
        MenuPage? target = currentPage.menuItems[selectionIndex].targetMenuPage;
        if (target == null) return;
        target.ParentSelectionIndex = selectionIndex;
        target.name = currentPage.menuItems[selectionIndex].displayName;
        selectionIndex = 0;
        currentPage = target;
        UpdateMenu();
    }

    public class MenuPage 
    {
        public string name = "Menu";
        public readonly List<MenuItem> menuItems = new();
        public MenuPage? Parent { get; set; }
        public int ParentSelectionIndex { get; set; }
        public bool BackExists { get; set; }

        public void AddMenuItem(MenuItem menuItem) {
            menuItems.Add(menuItem);
            if (menuItem.functionToRun == Navigate && menuItem.targetMenuPage != null) {
                menuItem.targetMenuPage.Parent = this;
            }
        }

        public void DisplayPage() {
            int i = 0;
            int maxLength = menuItems.Select(menuItem => menuItem.Draw(false).Length).Prepend(0).Max();
            if (Parent != null) {
                Console.WriteLine(GetPadding(name, maxLength) + "  " + name + "\n"); // print menu section header
                if (!BackExists) {
                    AddMenuItem(new MenuItem("Back", Back));
                    BackExists = true;
                }
            }
            foreach (MenuItem menuItem in menuItems) {
                string str = menuItem.Draw(false);
                string padding = GetPadding(str, maxLength);
                if (i == selectionIndex) { // item is selected
                    if (menuItem.isSlider) { // item is a changeable slider
                        Console.Write("\n" + padding + "< ");
                        menuItem.Draw(true);
                        Console.Write(" >");
                    } else { // item is a button
                        Console.Write("\n" + padding + "* ");
                        menuItem.Draw(true);
                        Console.Write(" *");
                    }
                } else { // item is not selected
                    Console.Write("\n" + padding + "  ");
                    menuItem.Draw(true);
                    Console.Write("  ");
                }
                i++;
            }
        }
    }

    private static void Back() {
        if (currentPage.Parent != null) {
            currentPage.menuItems.RemoveAt(currentPage.menuItems.Count - 1);
            currentPage.BackExists = false;
            selectionIndex = currentPage.ParentSelectionIndex;
            currentPage = currentPage.Parent;
            UpdateMenu();
        }
    }

    public static void UpdateMenu() {
        ConsoleUI.Clear();
        if (currentPage.Parent == null) {
            PrintLogo(); // top level shows logo
        }
        currentPage.DisplayPage();
    }

    public class MenuItem {
        public readonly string displayName;
        public readonly Action functionToRun;
        public readonly MenuPage? targetMenuPage; // if menu has functionToRun:navigate, this is the target menu it moves to.
        public RefInt variable = new(0); // if menuItem reflects a variable that can be changed by user
        public int minClamp; // clamp for variable
        public int maxClamp; // clamp for variable
        public bool isSlider;
        public MenuItem(string displayName, Action functionToRun) {
            this.displayName = displayName;
            this.functionToRun = functionToRun;
        }
        public MenuItem(string displayName, Action functionToRun, MenuPage? targetMenuPage) {
            this.displayName = displayName;
            this.functionToRun = functionToRun;
            this.targetMenuPage = targetMenuPage;
        }
        public MenuItem AssignVariable(RefInt variableIn, int minClampIn, int maxClampIn) {
            isSlider = true;
            variable = variableIn;
            minClamp = minClampIn;
            maxClamp = maxClampIn;
            return this;
        }
        public string Draw(bool writeToConsole) {
            if (writeToConsole) {
                if (isSlider) {
                    if (minClamp == 0 && maxClamp == 1) {
                        Console.Write(displayName + ": ");
                        Console.ForegroundColor = variable.value == 1 ? ConsoleColor.Green : ConsoleColor.Red;
                        Console.Write(variable.value == 1 ? "Enabled " : "Disabled");
                        Console.ForegroundColor = ConsoleColor.White;
                    } else {
                        Console.Write(displayName + ": " + variable.value);
                    }
                } else {
                    Console.Write(displayName);
                }
            } else {
                if (isSlider) {
                    if (minClamp == 0 && maxClamp == 1) {
                        return displayName + ": " + (variable.value == 1 ? "Enabled " : "Disabled");
                    }
                    return displayName + ": " + variable.value;
                }
                return displayName;
            }
            return "";
        }
    }

    private static string GetPadding(string str, int maxLength) {
        if (str.Length >= maxLength) return "";
        int padAmount = (maxLength - str.Length) / 2;
        return new string(' ', padAmount);
    }

    static string ChooseMenuText() {
        try
        {
            string fileLocation = @"menuTexts.txt";
            IEnumerable<string> textLines = File.ReadLines(fileLocation);
            int index = new Random().Next(textLines.Count());
            return textLines.ToList()[index];
        }
        catch (Exception e)
        {
            return "";
        }
    }

    private static void PrintLogo() {
        try
        {
            string fileLocation = @"menuAscii.txt";
            IEnumerable<string> lines = File.ReadLines(fileLocation);
            foreach (string line in lines) {
                Console.WriteLine(line);
            }
            Console.WriteLine(MenuText);
            Console.WriteLine("");
        }
        catch (Exception e)
        {
            // nothing
        }
        
    }

    public static void PromptMenu() {
        UpdateMenu();
        while (true) {
            MenuItem menuItem;
            ConsoleKey key = Console.ReadKey().Key;
            switch (key) {
                case ConsoleKey.UpArrow:
                    SetSelection(selectionIndex - 1);
                    break;
                case ConsoleKey.DownArrow:
                    SetSelection(selectionIndex + 1);
                    break;
                case ConsoleKey.LeftArrow:
                    menuItem = currentPage.menuItems[selectionIndex];
                    menuItem.variable.value--;
                    currentPage.menuItems[selectionIndex].variable.value = Math.Clamp(menuItem.variable.value, menuItem.minClamp, menuItem.maxClamp);
                    UpdateMenu();
                    break;
                case ConsoleKey.RightArrow:
                    menuItem = currentPage.menuItems[selectionIndex];
                    menuItem.variable.value++;
                    currentPage.menuItems[selectionIndex].variable.value = Math.Clamp(menuItem.variable.value, menuItem.minClamp, menuItem.maxClamp);
                    UpdateMenu();
                    break;
                case ConsoleKey.Enter:
                    Action func = currentPage.menuItems[selectionIndex].functionToRun;
                    currentPage.menuItems[selectionIndex].functionToRun();
                    if (func == PromptNames || func == ConsoleUI.ChooseSaveFile) {return;} // stop menu prompt if game is started
                    break;
            }
        }
    }

    private static void SetSelection(int i) {
        selectionIndex = Math.Clamp(i, 0, currentPage.menuItems.Count - 1);
        UpdateMenu();
    }

    private static void PromptNames() {
        ConsoleUI.Clear();
        GameNames.Clear();
        for (int i = 0; i < PlayerCount.value; i++) {
            Console.WriteLine("Enter name of player" + (i + 1));
            string? userInput = Console.ReadLine();
            string name;
            if (string.IsNullOrEmpty(userInput)) {
                name = "PLAYER-" + i;
            } else {
                name = userInput;
            }
            GameNames.Add(name);
        }
        Ruleset.DrawLimitIsOne = Convert.ToBoolean(DrawLimitIsOne.value);
        Ruleset.CanPassPlus2And4 = Convert.ToBoolean(CanPassPlus2And4.value);
        Ruleset.WildCards = WildCards.value;
        Ruleset.SpecialCards = SpecialCards.value;
        Ruleset.NumberCards = NumberCards.value;
        Main.StartNewGame(GameNames, AICount.value, Ruleset, true);
    }

    private static void PromptLoad() {
        List<string> matchingFiles;
        if (useDatabase.value == 1) {
            var pair = GetContext();
            matchingFiles = pair.Item1.GetAllNames();
        } else {
            string saveFilesPath = Environment.CurrentDirectory + "/saves";
            string[] files = Directory.GetFiles(saveFilesPath);
            matchingFiles = files.Where(file => file.EndsWith(SaveHandler.FileExtension)).ToList();
        }
        MenuPage? target = currentPage.menuItems[selectionIndex].targetMenuPage;
        if (target != null) {target.Parent = currentPage;}
        ConsoleUI.ListSaveFiles(matchingFiles);
        Navigate();
    }

    public static void StartLoading() {
        if (useDatabase.value == 1) {
            var pair = GetContext();
            Game game = pair.Item1.Load(GameContainer.GuidFromGameName(selectedFilename));
            GameContainer gameContainer = new() {
                state = game,
                serviceProvider = pair.Item2
            };
            gameContainer.TurnChange(true, 0); // start turn prompt function
        } else {
            Game game = SaveHandler.Load(selectedFilename);
            GameContainer gameContainer = new() {
                state = game,
            };
            gameContainer.TurnChange(true, 0); // start turn prompt function
        }
    }

    private static void ExitGame() {
        Environment.Exit(0);
    }

    public class RefInt { // Int encapsulated in object to change value dynamically, idk how to read/write with pointers in c#
        public int value;
        public RefInt(int value) {
            this.value = value;
        }
    }

    public static (IGameRepository, ServiceProvider) GetContext() {
        var connectionString = "DataSource=<%temppath%>uno.db;Cache=Shared";
        connectionString = connectionString.Replace("<%temppath%>", Path.GetTempPath());
        var contextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging()
            .Options;
        using var db = new AppDbContext(contextOptions);
        // apply all the migrations
        db.Database.Migrate();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IGameRepository, GameRepository>();
        var serviceProvider = services.BuildServiceProvider();
        var gameRepository = serviceProvider.GetService<IGameRepository>();
        if (gameRepository == null) {throw new Exception("Repository null");} 
        
        return (gameRepository, services.BuildServiceProvider());
    }
}

