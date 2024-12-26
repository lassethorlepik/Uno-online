namespace ConsoleApp;
using DAL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Domain;

using System;
using System.IO;

public class GameContainer // This is a container with helpful functions running game logic, Game (data) is stored in state.
{
    public Game state = new();
    public ServiceProvider? serviceProvider;
    private bool _firstTurn = true;
    public bool runningInWeb = false;
    public bool gameover = false;

    public GameContainer(string gameName, List<Player> playersList, RuleSet usedRuleSet, bool startGame) { // construct game
        Console.ForegroundColor = ConsoleColor.White;
        Console.BackgroundColor = ConsoleColor.Black;
        if (!runningInWeb)
        {
            //state.Turn = -1;
            ConsoleUI.Clear();
            Console.WriteLine("Game is starting");
        }
        state.Players = playersList;
        state.CurrentPlayer = state.Players[0];
        state.UsedRuleSet = usedRuleSet;
        PopulateDeck(state.GameDeck);
        InitialDraw();
        state.GameName = gameName;
        state.Id = GuidFromGameName(state.GameName);
        SaveGame();
        if (startGame)
        {
            TurnChange(true, 1);
        }

    }

    public GameContainer() { // deserialization constructor
        Console.ForegroundColor = ConsoleColor.White;
        Console.BackgroundColor = ConsoleColor.Black;
        if (!runningInWeb)
        {
            ConsoleUI.Clear();
        }
        Console.WriteLine("Game is starting");
    }

    public bool Playable(Card card) {
        if (state.DiscardPile.Count == 0 || card.Color == CardColor.Wild) {return true;} // first card is always playable, wild cards are always playable
        Card? lastPlayedCard = GetLast(state.DiscardPile);
        if (lastPlayedCard != null && lastPlayedCard.Type != card.Type)
            return lastPlayedCard.Color == card.Color; // same color stacks, otherwise card can not be played
        // last card was same type
        if (card.Type == CardType.Number) { // card is a number card
            if (lastPlayedCard == null || lastPlayedCard.Number == card.Number) { // numbers match
                return true;
            }
        } else { // same type, but not a number card
            return true;
        }
        return lastPlayedCard.Color == card.Color; // same color stacks, otherwise card can not be played
    }

    public void TurnChange(bool prompt, int multiplier) { // multiplier is for internal use to freely move a number of steps forward or backward.
        state.CurrentPlayerCalledUno = false;
        if (multiplier == 1) {
            state.TotalTurnCounter++;
        }
        if (state.Players.Count != 1)
        {
            if (state.IsReversed) {
                state.Turn--;
                if (state.Turn < 0) {state.Turn = state.Players.Count - 1;}
            } else {
                state.Turn = (state.Turn + 1 * multiplier) % state.Players.Count;
            }
        }
        else
        {
            state.Turn = 0;
        }
        if (state.Players.Count != 0)
        {
            try
            {
                state.CurrentPlayer = state.Players[state.Turn];
            }
            catch (Exception e)
            {
                // failsafe
            }
            
        }
        
        if (_firstTurn)
        {
            // Skip saving on first turn, because turn is made automatically while constructing game
            _firstTurn = false;
        }
        else {
            SaveGame();
        }

        if (prompt) {
            if (!runningInWeb)
            {
                ConsoleUI.NotifyTurnChange(state.CurrentPlayer.Name);
            }
            
            StartTurn();
        }
    }

    public void SaveGame()
    {
        if (Menu.useDatabase.value == 1)
        {
            try
            {
                    GetContext().Save(state);
            } catch 
            {
                var a = GetNewContext();
                if (a == null)
                {
                    Console.WriteLine("a null");
                }
                else
                {
                    a.Save(state); // save game state to file
                    Console.WriteLine("Saved");
                }
            }
        }
        else
        {
            SaveHandler.Save(this); // save game state to file
        }
    }

    private Player GetNextPlayer() {
        int tempTurn = state.Turn;
        if (state.IsReversed) {
            tempTurn--;
            if (tempTurn < 0) {tempTurn = state.Players.Count - 1;}
        } else {
            tempTurn = (tempTurn + 1) % state.Players.Count;
        }
        return state.Players[tempTurn];
    }

    public string GetCurrentPlayerName() {
        return state.CurrentPlayer.Name;
    }

    public void InitialDraw() { // Add seven cards to each players hand
        foreach(Player player in state.Players) { // for every player
            for (int i = 0; i < 7; i++) { // do seven times
                Draw(state.GameDeck, player); // draw a card
            }
            if (state.GameDeck.Cards.Count == 0) {
                if (!runningInWeb)
                {
                    ConsoleUI.Clear();
                }
                Console.WriteLine("\nNot enough cards for initial drawing round!");
                Console.WriteLine("\nAdd more cards in the settings.");
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
                Menu.PromptMenu();
                break;
            }
        }
    }

    public void AttemptCardDraw(Player player) { // makes specified player draw a card, may add checks later.
        Draw(state.GameDeck, player);
    }

    private CardColor ChooseWildCardColor() {
        if (state.CurrentPlayer.IsAi) {
            return AIChooseColor();
        } else {
            string[] choices = {"red", "yellow", "blue", "green"};
            ConsoleColor[] colors = {ConsoleColor.Red, ConsoleColor.Yellow, ConsoleColor.Blue, ConsoleColor.Green};
            int choice;
            if (!runningInWeb)
            {
                choice = ConsoleUI.SelectionPrompt(state, choices.ToList(), colors.ToList(), "\nPick a color from:\n", true);
            }
            else
            {
                List<string> prefColors = new List<string> {"Red", "Yellow", "Blue", "Green"};
                choice = 0;
                foreach (Player player in state.Players)
                {
                    if (player.Id == state.CurrentPlayer.Id)
                    {
                        choice = prefColors.IndexOf(player.PrefColor);
                        break;
                    }
                }
                
            }
            return (CardColor) choice;
        }
    }

    public static Card? GetLast(List<Card> cardList) {
        return cardList.Count == 0 ? null : cardList.Last();
    }

    public void GameOver()
    {
        gameover = true;
        SaveGame();
        if (!runningInWeb)
        {
            ConsoleUI.Clear();
            Console.WriteLine("Final Leaderboard\n");
            int i = 1;
            foreach (Player player in state.Winners) {
                Console.WriteLine(i + ". " + player.Name);
                i++;
            }
            Console.WriteLine("\nPress any key to end game...");
            Console.ReadKey();
            Menu.PromptMenu();
        }
    }

    private static List<Card> Shuffle(IEnumerable<Card> cardList) {
        return cardList.OrderBy(_=> Random.Shared.Next()).ToList();
    }
    
    private void PopulateDeck(Deck deck) {
        /*
        An Uno deck contains:

        4 blank
        4 wildDraw4
        4 wild
        8 skips (2 in each color)
        8 reverse (2 in each color)
        8 draw2 (2 in each color)

        1-9 red x 2
        1-9 yellow x 2
        1-9 blue x 2
        1-9 green x 2

        red 0, yellow 0, blue 0, green 0
        */

        int wildCards = state.UsedRuleSet.WildCards; // 1
        int specialCards = state.UsedRuleSet.SpecialCards; // 2
        int numberCards = state.UsedRuleSet.NumberCards; // 20

        int colorCount = Enum.GetValues<CardColor>().Length - 1; // count - 1, because last one is wild and not a color
        for (int i = 0; i < colorCount; i++) { // for each color
            CardColor cardColor = Enum.GetValues<CardColor>()[i];
            for (int j = 0; j < wildCards; j++) {
                AddWildCards(deck); // wild cards
            }
            for (int k = 0; k < specialCards; k++) {
                AddSpecialCards(deck, cardColor); // special cards
            }
            for (int l = 1; l < numberCards; l++) { // number cards
                deck.Cards.Add(new Card()
                {
                    Type = CardType.Number,
                    Color = cardColor,
                    Number = l % 10
                });
            }
        }
        deck.Cards = Shuffle(deck.Cards);
    }

    private static void AddWildCards(Deck deck) {
        deck.Cards.Add(new Card()
        {
            Type = CardType.Blank,
            Color = CardColor.Wild,
            Number =  -1
        });
        deck.Cards.Add(new Card()
        {
            Type = CardType.Draw4,
            Color = CardColor.Wild,
            Number =  -1
        });
        deck.Cards.Add(new Card()
        {
            Type = CardType.Wild,
            Color = CardColor.Wild,
            Number =  -1
        });
    }

    private static void AddSpecialCards(Deck deck, CardColor cardColor) {
        deck.Cards.Add(new Card()
        {
            Type = CardType.Skip,
            Color = cardColor,
            Number =  -1
        });
        deck.Cards.Add(new Card()
        {
            Type = CardType.Reverse,
            Color = cardColor,
            Number =  -1
        });
        deck.Cards.Add(new Card()
        {
            Type = CardType.Draw2,
            Color = cardColor,
            Number =  -1
        });
    }
    
    private IGameRepository GetContext() {
        if (serviceProvider == null) {throw new Exception("Repository null");}
        var gameRepository = serviceProvider.GetService<IGameRepository>();
        if (gameRepository == null) {throw new Exception("Repository null");} 
        return gameRepository;
    }
    
    public void Draw(Deck deck, Player player) {
            List<Card> cards = deck.Cards;
            if (cards.Count == 0) { // deck empty
                // separate top card of discard pile
                Card? topCard = GetLast(state.DiscardPile);
                if (topCard != null) {
                    state.DiscardPile.RemoveAt(state.DiscardPile.Count -1);
                }
                // shuffle discard pile
                state.DiscardPile = Shuffle(state.DiscardPile);
                // put discard pile in deck
                state.GameDeck.Cards.AddRange(state.DiscardPile);
                state.DiscardPile.Clear();
                // put separated card back on top of discard pile
                if (topCard != null) {
                    state.DiscardPile.Add(topCard);
                }
            }

            if (cards.Count == 0) return;
            Card card = cards[0];
            cards.RemoveAt(0);
            player.Hand.Add(card);
        }

        public void PlayCard(Player player, Card card)
        {
            if (!Playable(card)) return; // Play action is ignored because card can not be played
            if (state.Players.Count < 1) { // Playing a card is not possible if there are no players
                return;
            }
            if (card.Color == CardColor.Wild) {
                card.Color = ChooseWildCardColor();
            }
            state.DiscardPile.Add(card);
            RemoveCard(player, card);
            if (card.Type == CardType.Blank && state.Players.Count > 1) { // blank is regarded as swap hands card for simplicity
                List<Player> otherPlayers = state.Players.Where(eachPlayer => eachPlayer != state.CurrentPlayer).ToList();
                List<string> choiceList = otherPlayers.Select(eachPlayer => eachPlayer.ToString()).ToList();
                int choice;
                if (state.CurrentPlayer.IsAi) {
                    var random = new Random();
                    choice = random.Next(otherPlayers.Count);
                } else {
                    if (!runningInWeb)
                    {
                        choice = ConsoleUI.SelectionPrompt(state, choiceList, null, "\nChoose player to swap hands with:\n", true);
                    }
                    else
                    {
                        choice = player.PrefSwap;
                    }
                    
                }
                if (choice == -1) {return;} // ignore
                SwapHands(player, otherPlayers[choice]);
            }
            CheckIfGameOver();
            CardEffectCheck(card); // special effect check
            state.CurrentPlayerDrew = false;
        }

        private void CheckIfGameOver()
        {
            bool playerRemoved = false;
            foreach (Player player in state.Players.ToList()) {
                if (player.Hand.Count == 0)
                {
                    state.Winners.Add(player);
                    state.Players.Remove(player);
                    playerRemoved = true;
                }
            }
            if (state.Players.Count == 0) {GameOver();}
            if (playerRemoved) {
                TurnChange(false, -1); // go back by one player, because current player was removed before turn ended.
            }
        }

        private void SwapHands(Player player, Player otherPlayer) {
            (otherPlayer.Hand, player.Hand) = (player.Hand, otherPlayer.Hand);
        }

        void CardEffectCheck(Card card)
        {
            Card? lastPlayedCard = GetLast(state.DiscardPile);
            if (state.Players.Count < 1) {return;} // effects are void if no players exist
            switch (card.Type) {
                case CardType.Blank: // switch hands
                    // blank card effect
                    TurnChange(true, 1);
                    break;
                case CardType.Draw4:
                case CardType.Draw2 when (lastPlayedCard == null || card.Type == lastPlayedCard.Type):
                    CardDrawEffect(card);
                    break;
                case CardType.Skip:
                    TurnChange(false, 1); // change turns two times
                    if (!runningInWeb)
                    {
                        ConsoleUI.AnyKeyMessage(GetCurrentPlayerName() + " was skipped. Press any key to continue.");
                    }
                    TurnChange(true, 1);
                    break;
                case CardType.Reverse:
                    state.IsReversed = !state.IsReversed; // set reverse bool
                    TurnChange(true, state.Players.Count == 2 ? 0 : 1); // if two players, same player goes again
                    break;
                default: // no effect, just change turns
                    TurnChange(true, 1);
                    break;
            }
            SaveGame();
        }

        private void CardDrawEffect(Card card) {
            bool stacking = false;
            Player nextPlayer = GetNextPlayer();
            // if rules allow stacking and passing, check next players hand and choice.
            bool hasStackable = false;
            foreach (var c in nextPlayer.Hand)
            {
                if (c.Type is CardType.Draw4 or CardType.Draw2)
                {
                    hasStackable = true;
                    break;
                }
            }
            if (state.UsedRuleSet.CanPassPlus2And4 && hasStackable) {
                if (nextPlayer.IsAi) {
                    stacking = true; // AI ALWAYS STACKS CARDS
                } else {
                    if (!runningInWeb)
                    {
                        stacking = ConsoleUI.StackingPrompt(nextPlayer, card.Type); // Prompt for stacking in console
                    }
                    else
                    {
                        stacking = nextPlayer.Stacking; // Use stacking preference value for web app
                    }
                }
            }
            if (stacking) {
                state.StackCounter++;
                TurnChange(false, 1); // move to next player
                foreach (var c in state.CurrentPlayer.Hand)
                {
                    if (state.DiscardPile.Count != 0 && c.Type == GetLast(state.DiscardPile)!.Type &&
                        c.Type is CardType.Draw2 or CardType.Draw4)
                    {
                        PlayCard(state.CurrentPlayer, c); // player's card is stacked
                        break;
                    }
                }
                //Console.WriteLine(game.GetCurrentPlayerName() + " stacked " + card);
            } else { // player won't stack, just make them pick up cards.
                TurnChange(false, 1); // change turn to drawing player
                int cardsToDraw = state.StackCounter;
                if (card.Type == CardType.Draw4) {
                    cardsToDraw *= 4;
                } else {
                    cardsToDraw *= 2;
                }
                for (int i = 0; i < cardsToDraw; i++) { // make player draw
                    AttemptCardDraw(state.CurrentPlayer);
                }
                state.StackCounter = 1; // reset stacks
                if (!runningInWeb)
                {
                    ConsoleUI.AnyKeyMessage(GetCurrentPlayerName() + " drew " + cardsToDraw + " cards and was skipped. Press any key to continue.");
                }
                
                TurnChange(true, 1); // end turn of drawing player.
            }
        }
        

        public void StartTurn()
        {
            state.CurrentPlayerDrew = false;
            if (state.CurrentPlayer.IsAi) {
                AIChooseCard();
            } else {
                if (!runningInWeb)
                {
                    ConsoleUI.ChooseCardPrompt(this);
                }
            }
        }

        void RemoveCard(Player player, Card targetCard) {
            int i = 0;
            foreach (Card card in player.Hand) {
                if (card.Equals(targetCard)) {
                    player.Hand.RemoveAt(i);
                    return;
                }
                i++;
            }
            Console.WriteLine("Error! Removable card not found!");
        }

        public bool HasPlayableCards(Player player) {
            return player.Hand.Any(Playable);
        }

        private static bool HasCardOfTypeAndColor(Player player, Card targetCard) {
            return player.Hand.Any(card => card.Equals(targetCard));
        }
        
        private static IGameRepository? GetNewContext() {
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
            return gameRepository;
        }
        
        public static Guid GuidFromGameName(string input) {
            Guid result;
            using (MD5 md5 = MD5.Create()) {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                result = new Guid(hash);
            }
            return result;
        }

        private void AIChooseCard() {
            state.CurrentPlayerCalledUno = true; // AI always calls uno at start of turn
            List<Card> hand = state.CurrentPlayer.Hand;
            List<Card> playableCards = new List<Card>();
            while (true) { // check if playable cards, if not, draw according to rules.
                foreach (var card in hand) {
                    if (Playable(card)) {
                        playableCards.Add(card);
                    }
                }
                if (playableCards.Count == 0) {
                    state.CurrentPlayerDrew = true;
                    Draw(state.GameDeck, state.CurrentPlayer);
                } else {
                    break;
                }
                if (state.UsedRuleSet.DrawLimitIsOne) {
                    break;
                }
            }
            if (playableCards.Count == 0) {
                if (!runningInWeb)
                {
                    ConsoleUI.AnyKeyMessage(state.CurrentPlayer.Name + " drew a card and ended turn.");
                }
                state.CurrentPlayerDrew = false;
                TurnChange(true, 1); // pass if no cards to play
            } else {
                var random = new Random();
                int index = random.Next(playableCards.Count);
                Card ChosenCard = playableCards[index];
                if (!runningInWeb)
                {
                    ConsoleUI.AnyKeyMessage(state.CurrentPlayer.Name + " played " + ChosenCard);
                }
                PlayCard(state.CurrentPlayer, ChosenCard);
            }
        }

        private CardColor AIChooseColor() { // Choose color most common in hand
            int red = 0;
            int yellow = 0;
            int blue = 0;
            int green = 0;
            List<Card> hand = state.CurrentPlayer.Hand;
            foreach (var card in hand) {
                if (card.Color == CardColor.Red) {
                    red++;
                } else if (card.Color == CardColor.Yellow) {
                    yellow++;
                } else if (card.Color == CardColor.Blue) {
                    blue++;
                } else if (card.Color == CardColor.Green) {
                    green++;
                }
            }
            if (red > yellow && red > blue && red > green) {
                return CardColor.Red;
            }
            if (yellow > red && yellow > blue && yellow > green) {
                return CardColor.Yellow;
            }
            if (blue > red && blue > yellow && blue > green) {
                return CardColor.Blue;
            }
            if (green > red && green > yellow && green > blue) {
                return CardColor.Green;
            }
            var random = new Random();
            int i = random.Next(hand.Count);
            if (hand[i].Color == CardColor.Wild) {return CardColor.Red;} // if random card happens to be wild, choose red color
            // this logic should be improved as it causes AI to choose red more often than others.
            return hand[i].Color;
        }
}

