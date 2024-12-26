using Domain.DB;

namespace DAL;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public DbSet<Game> Games { get; set; } = default!;
    
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {
        // nothing to add rn
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder) { // every item is assigned primary key
    }
}
