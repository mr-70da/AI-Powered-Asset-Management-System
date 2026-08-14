using Kinana.AssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kinana.AssetManagement.Infrastructure.Data;

public partial class AssetManagementDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Asset>(entity =>
        {
            entity.Property(a => a.RowVersion).IsRowVersion();
        });
    }
}
