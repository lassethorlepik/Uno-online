namespace Domain;

public class RuleSet {
    public bool DrawLimitIsOne { get; set; } = true; // If true, player has to draw until a playable card is found. If false, when player draws a card, his turn is forfeit.
    public bool CanPassPlus2And4 { get; set; } = false; // Can player stack plus 2 cards with eachother and plus 4 cards with eachother, passing them to next player.
    public int WildCards { get; set; } = 1;
    public int SpecialCards { get; set; } = 2;
    public int NumberCards { get; set; } = 20;
}