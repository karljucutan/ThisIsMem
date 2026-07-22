using API.Domain;
using API.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace API.Infrastructure.Persistence;

public sealed class MemDbContext : DbContext
{
    public const string ConnectionStringName = "MemDatabase";

    public MemDbContext(DbContextOptions<MemDbContext> options)
        : base(options)
    {
    }

    public DbSet<RagDocument> Documents => Set<RagDocument>();

    public DbSet<RagChunk> Chunks => Set<RagChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.ApplyConfiguration(new RagDocumentConfiguration());
        modelBuilder.ApplyConfiguration(new RagChunkConfiguration());
    }
}