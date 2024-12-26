namespace Domain.DB;

public abstract class BaseEntity { // base class for DB classes, provides primary key
    public Guid Id { get; set; }
}