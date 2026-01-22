using FindActivity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FindActivity.Infrastructure.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(80).IsRequired();

        builder.HasData(
            new Category { Id = 1, Name = "Sports" },
            new Category { Id = 2, Name = "Outdoors" },
            new Category { Id = 3, Name = "Music" },
            new Category { Id = 4, Name = "Food" },
            new Category { Id = 5, Name = "Arts" },
            new Category { Id = 6, Name = "Tech" }
        );
    }
}
