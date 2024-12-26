namespace Domain.DB;

public class Game : BaseEntity {
    public DateTime CreatedAtDt { get; set; } = DateTime.Now;
    public DateTime UpdatedAtDt { get; set; } = DateTime.Now;
    public string State { get; set; } = default!;
}