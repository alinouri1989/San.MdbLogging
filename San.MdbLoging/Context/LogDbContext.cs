using Microsoft.EntityFrameworkCore;
using San.MdbLogging.Models;
namespace San.SqlLogging;

public class LogDbContext<T> : DbContext where T : BaseSqlModel
{
    public LogDbContext(DbContextOptions<LogDbContext<T>> options)
        : base(options)
    {
    }

    public DbSet<T> Logs { get; set; } // Renamed for clarity  

    // Override OnModelCreating if needed  
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<T>().ToTable(typeof(T).Name); // Ensure the table name matches the entity name  
        modelBuilder.Entity<T>().Property(e => e.TraceCode); // Example configuration  
    }
}