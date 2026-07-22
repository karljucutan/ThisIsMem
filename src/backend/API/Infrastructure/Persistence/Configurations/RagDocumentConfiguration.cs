using API.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Infrastructure.Persistence.Configurations;

public sealed class RagDocumentConfiguration : IEntityTypeConfiguration<RagDocument>
{
    public void Configure(EntityTypeBuilder<RagDocument> builder)
    {
        builder.ToTable("rag_document");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.DocumentKey)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.SourcePath)
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(x => x.ContentHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.MetadataJson)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}")
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.DocumentKey)
            .IsUnique();

        builder.HasMany(x => x.Chunks)
            .WithOne()
            .HasForeignKey(x => x.RagDocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}