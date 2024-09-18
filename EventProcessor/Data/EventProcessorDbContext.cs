using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventProcessor.Data;

public class EventProcessorDbContext : DbContext
{
    public DbSet<Event> Events { get; set; }
    public DbSet<Incident> Incidents { get; set; }

    public EventProcessorDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Time);
            entity.Property(e => e.Type);
        });
        modelBuilder.Entity<Incident>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Time);
            entity.Property(e => e.Type);
            entity.HasMany(e => e.Events)
                .WithOne();
            entity.HasIndex(e => e.Time);
            entity.HasIndex(e => e.Type);
        });
    }
}