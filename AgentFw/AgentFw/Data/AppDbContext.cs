using Microsoft.EntityFrameworkCore;

namespace AgentFw.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<TodoItem> Todos => Set<TodoItem>();

        public DbSet<AiProvider> AiProviders => Set<AiProvider>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TodoItem>().ToTable("todos");

            modelBuilder.Entity<AiProvider>(entity =>
            {
                entity.ToTable("ai_providers");

                entity.Property(p => p.ProviderType).HasConversion<string>().HasMaxLength(32);
                entity.Property(p => p.AuthMode).HasConversion<string>().HasMaxLength(32);

                entity.HasIndex(p => p.Name).IsUnique();

                // At most one provider can be active at a time.
                entity.HasIndex(p => p.IsActive)
                      .IsUnique()
                      .HasFilter("is_active");
            });
        }
    }
}
