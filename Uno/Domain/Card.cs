namespace Domain;

public class Card {
    public CardType Type { get; set; }
    public CardColor Color { get; set; }
    public int Number { get; set; }

    public override string ToString()
    {
        if (Number == -1) {
            return "[" + Type + "]";
        } else {
            return "[" + Number + "]";
        }
            
    }
    public bool Equals(Card card) {
        return Type == card.Type && Color == card.Color && Number == card.Number;
    }
}