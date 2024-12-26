namespace Domain;

public class Player
{
    
    public string Name { get; set; } = default!;

    public bool IsAi { get; set; }

    public List<Card> Hand { get; set; } = new();
    
    public override string ToString() {
        return Name;
    }

    public bool Stacking { get; set; }
    public string PrefColor { get; set; } = "Red";
    public int PrefSwap { get; set; }
    public Guid Id { get; set; }
}