using DatabaseSimulation.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DatabaseSimulation.Data;

public class SimulationDbContext(DbContextOptions<SimulationDbContext> options) : DbContext(options) {
    public DbSet<SimulationRun> SimulationRuns => Set<SimulationRun>();
    public DbSet<PhilosopherStateLog> PhilosopherStateLogs => Set<PhilosopherStateLog>();
    public DbSet<ForkStateLog> ForkStateLogs => Set<ForkStateLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<SimulationRun>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StartTime).IsRequired();
            entity.Property(e => e.StrategyName).HasMaxLength(100);
        });
        
        modelBuilder.Entity<PhilosopherStateLog>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PhilosopherName).HasMaxLength(100);
            entity.Property(e => e.State).HasConversion<string>();
            entity.Property(e => e.Action).HasConversion<string>();
            
            // TODO: this index may not be necessary at all
            entity.HasIndex(e => new { e.SimulationRunId, e.TimestampMs });
            
            entity.HasIndex(e => new { e.SimulationRunId, e.PhilosopherId, e.TimestampMs });
            
            entity.HasOne(e => e.SimulationRun)
                .WithMany(sr => sr.PhilosopherStateLogs)
                .HasForeignKey(e => e.SimulationRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<ForkStateLog>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UsedByPhilosopherName).HasMaxLength(100);
            entity.Property(e => e.State).HasConversion<string>();
            
            // TODO: this index may not be necessary at all
            entity.HasIndex(e => new { e.SimulationRunId, e.TimestampMs });
            
            entity.HasIndex(e => new { e.SimulationRunId, e.ForkId, e.TimestampMs });
            
            entity.HasOne(e => e.SimulationRun)
                .WithMany(sr => sr.ForkStateLogs)
                .HasForeignKey(e => e.SimulationRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

