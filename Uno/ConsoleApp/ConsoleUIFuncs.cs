namespace ConsoleApp;
using Domain;
public static class ConsoleUI {

    public static bool StackingPrompt(Player promptedPlayer, CardType cardType) {
        Console.Clear();
        Console.WriteLine(promptedPlayer.Name + ", do you wish to stack your " + cardType.ToString() + " card? (Y/N)\n");
        while (true) {
            string? userInput = Console.ReadLine();
            if (string.IsNullOrEmpty(userInput)) {
                Console.WriteLine("Empty input, type 'y' or 'n'.");
            } else {
                userInput = userInput.ToLower();
                if (userInput is "y" or "yes") {
                    return true;
                }
                if (userInput is "n" or "no") {
                    return false;
                }
                Console.WriteLine("Invalid input, type 'y' or 'n'.");
            }
        }
    }

    private static void PrintTurnText(Game internalGame, bool withCards) {
        Clear();
        Console.WriteLine(internalGame.CurrentPlayer.Name + ", now is your turn.");
        if (internalGame.DiscardPile.Count > 0) {
            Card? lastCard = internalGame.DiscardPile.Count == 0 ? null : internalGame.DiscardPile.Last();
            if (lastCard != null) {
                List<string> cardString = new List<string> {lastCard.ToString()};
                List<Card> card = new List<Card> {lastCard};
                DrawChoices(cardString, -1, GetCardColors(card), "Last card was: ");
                Console.Write(" "); // spacer
            }
        }
        if (withCards) {
            List<string> cardStrings = GetCardStrings(internalGame);
            List<ConsoleColor> colors = GetCardColors(internalGame.CurrentPlayer.Hand);
            DrawChoices(cardStrings, -1, colors, "\nYour hand is:\n");
        }
    }

    private static List<string> GetCardStrings(Game game) {
        return game.CurrentPlayer.Hand.Select(card => card.ToString()).ToList();
    }

    private static List<ConsoleColor> GetCardColors(List<Card> cards) {
        List<ConsoleColor> colors = new List<ConsoleColor>();
        foreach(Card card in cards) {
            switch (card.Color)
            {
                case CardColor.Red:
                    colors.Add(ConsoleColor.Red);
                    break;
                case CardColor.Yellow:
                    colors.Add(ConsoleColor.Yellow);
                    break;
                case CardColor.Blue:
                    colors.Add(ConsoleColor.Blue);
                    break;
                case CardColor.Green:
                    colors.Add(ConsoleColor.Green);
                    break;
                default:
                    colors.Add(ConsoleColor.White);
                    break;
            }
        }
        return colors;
    }

    public static void ChooseCardPrompt(GameContainer gameContainer) {
        bool choiceMade = false; // if choice is made, prompt will end, otherwise retries
        bool canDraw = true; // used to determine if player is shown draw card button
        bool canEndTurn = false; // used to determine if player is shown end turn button
        bool canOnlyPlayLastDrawn = false; // player cant play other cards when drawing
        while (!choiceMade) {
            Console.Clear(); // full refresh
            List<string> cardStrings = GetCardStrings(gameContainer.state);
            var colors = GetCardColors(gameContainer.state.CurrentPlayer.Hand);
            if (gameContainer.state.CurrentPlayer.Hand.Count == 2 && gameContainer.state.CurrentPlayerCalledUno == false) {
                cardStrings.Add("[CALL UNO!]");
                colors.Add(ConsoleColor.DarkGray);
            }
            if (canDraw) {
                cardStrings.Add("[DRAW A CARD]");
                colors.Add(ConsoleColor.DarkGray);
            }
            if (canEndTurn) {
                cardStrings.Add("[END TURN]");
                colors.Add(ConsoleColor.DarkGray);
            }
            int choice = SelectionPrompt(gameContainer.state, cardStrings, colors, "\nChoose your action:\n", false);
            if (choice == -1) {return;} // ignore
            if (cardStrings[choice] == "[DRAW A CARD]")
            {
                gameContainer.state.CurrentPlayerDrew = true;
                canOnlyPlayLastDrawn = true;
                if (gameContainer.state.UsedRuleSet.DrawLimitIsOne) { // player can always draw 0-1 cards when playable card in hand, if no playable cards, draws one, if no limit, draws until playable found.
                    gameContainer.AttemptCardDraw(gameContainer.state.CurrentPlayer);
                    List<Card> tmpCard = new List<Card> {gameContainer.state.CurrentPlayer.Hand.Last()};
                    colors.Add(GetCardColors(tmpCard)[0]);
                    canDraw = false;
                    canEndTurn = true;
                } else {
                    gameContainer.AttemptCardDraw(gameContainer.state.CurrentPlayer);
                    if (gameContainer.HasPlayableCards(gameContainer.state.CurrentPlayer)) { // if player can play a card
                        List<Card> tmpCard = new List<Card> {gameContainer.state.CurrentPlayer.Hand.Last()};
                        colors.Add(GetCardColors(tmpCard)[0]);
                        canDraw = false;
                        canEndTurn = true;
                    } else {
                        List<Card> tmpCard = new List<Card> {gameContainer.state.CurrentPlayer.Hand.Last()};
                        colors.Add(GetCardColors(tmpCard)[0]);
                    }
                }
            } else if (cardStrings[choice] == "[CALL UNO!]") {
                gameContainer.state.CurrentPlayerCalledUno = true;
            } else if (cardStrings[choice] == "[END TURN]") {
                choiceMade = true;
                gameContainer.state.CurrentPlayerDrew = false; // reset drawing
                gameContainer.TurnChange(true, 1);
            } else { // play card
                Card chosenCard = gameContainer.state.CurrentPlayer.Hand[choice];
                if (gameContainer.Playable(chosenCard)) {
                    if (canOnlyPlayLastDrawn && choice + 1 <= gameContainer.state.CurrentPlayer.Hand.Count - 1) { // if player chooses card from hand after drawing, but it's not drawn card
                        // ignore, this is not allowed
                    } else {
                        if (gameContainer.state.CurrentPlayer.Hand.Count == 2 && !gameContainer.state.CurrentPlayerCalledUno) {
                            gameContainer.PlayCard(gameContainer.state.CurrentPlayer, chosenCard);
                            gameContainer.AttemptCardDraw(gameContainer.state.CurrentPlayer);
                            gameContainer.AttemptCardDraw(gameContainer.state.CurrentPlayer);
                            AnyKeyMessage("You did not call Uno! and draw 2 cards. Press any key to continue.");
                            gameContainer.TurnChange(true, 1);
                        } else {
                            gameContainer.PlayCard(gameContainer.state.CurrentPlayer, chosenCard);
                        }
                        choiceMade = true;
                    }
                }
            }
        }
    }

    public static void Clear() {
        Console.ForegroundColor = ConsoleColor.White;
        Console.BackgroundColor = ConsoleColor.Black;
        Console.SetCursorPosition(0, 0);
        for (int i = 0; i < 10; i++) {
            Console.WriteLine(new String(' ', 300));
        }
        Console.SetCursorPosition(0, 0);
    }

    public static int SelectionPrompt(Game game, List<string> choices, List<ConsoleColor>? colors, string promptText, bool withCards) {
        colors ??= DefaultColors(choices); // if null, set
        int selection = 0;
        int max = choices.Count - 1;
        PrintTurnText(game, withCards);
        DrawChoices(choices, selection, colors, promptText);
        bool prompting = true;
        while (prompting) {
            ConsoleKey key = Console.ReadKey().Key;
            switch (key) {
                case ConsoleKey.LeftArrow:
                    selection = Math.Clamp(selection - 1, 0, max);
                    PrintTurnText(game, withCards);
                    DrawChoices(choices, selection, colors, promptText);
                    break;
                case ConsoleKey.RightArrow:
                    selection = Math.Clamp(selection + 1, 0, max);
                    PrintTurnText(game, withCards);
                    DrawChoices(choices, selection, colors, promptText);
                    break;
                case ConsoleKey.Enter:
                    return selection;
                case ConsoleKey.Escape:
                    if (withCards == false) { // user can only escape during card choice
                        prompting = false;
                        Clear();
                        Menu.PromptMenu();
                    }
                    break;
            }
        }
        return -1; // does not matter, game is abandoned
    }

    private static void DrawChoices(List<string> choices, int selection, List<ConsoleColor> colors, string promptText) {
        Console.Write(promptText);
        int i = 0;
        foreach (var choice in choices) {
            if (i == selection) {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = colors[i];
                Console.Write(" " + choice + " ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Black;
            } else {
                Console.ForegroundColor = colors[i];
                Console.Write(" " + choice + " ");
                Console.ForegroundColor = ConsoleColor.White;
            }
            i++;
        }
    }

    private static List<ConsoleColor> DefaultColors(List<string> choiceList) {
        List<ConsoleColor> colors = new List<ConsoleColor>();
        for (int i = 0; i < choiceList.Count; i++) {
            colors.Add(ConsoleColor.White);
        }
        return colors;
    }

    public static void ListSaveFiles(List<string> matchingFiles) {
        Console.WriteLine(Menu.currentPage.Parent);
        Menu.MenuPage? target = Menu.currentPage.menuItems[Menu.selectionIndex].targetMenuPage;
        if (target != null) {
            target.menuItems.Clear();
            foreach (string saveFile in matchingFiles) {
                target.AddMenuItem(new Menu.MenuItem(Path.GetFileName(saveFile), ChooseSaveFile));
            }
            Menu.UpdateMenu();
        }
    }

    public static void ChooseSaveFile() {
        Menu.selectedFilename = Menu.currentPage.menuItems[Menu.selectionIndex].displayName;
        Menu.StartLoading();
    }

    public static void AnyKeyMessage(string message) {
        Clear();
        Console.WriteLine(message);
        Console.ReadKey();
    }

    public static void NotifyTurnChange(string name) {
        AnyKeyMessage("Next turn is for player: " + name);
    }
}