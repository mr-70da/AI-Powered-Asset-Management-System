using System;
using System.Collections.Generic;
using Kinana.AssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kinana.AssetManagement.Infrastructure.Data;

public partial class AssetManagementDbContext : DbContext
{
    public AssetManagementDbContext(DbContextOptions<AssetManagementDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AppUser> AppUsers { get; set; }

    public virtual DbSet<Asset> Assets { get; set; }

    public virtual DbSet<AssetTransfer> AssetTransfers { get; set; }

    public virtual DbSet<AssetType> AssetTypes { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Location> Locations { get; set; }

    public virtual DbSet<Organisation> Organisations { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AppUsers__3214EC07BD1944F5");

            entity.HasIndex(e => e.Email, "UQ__AppUsers__A9D105348EA5E6C1").IsUnique();

            entity.HasIndex(e => e.UserName, "UQ__AppUsers__C9F28456D929077D").IsUnique();

            entity.Property(e => e.CreatedAtUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DisplayName).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.UserName).HasMaxLength(50);

            entity.HasOne(d => d.Role).WithMany(p => p.AppUsers)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AppUsers_Roles");
        });

        modelBuilder.Entity<Asset>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Assets__3214EC070707B4BD");

            entity.HasIndex(e => e.AssetTypeId, "IX_Assets_AssetTypeId");

            entity.HasIndex(e => e.AssignedEmployeeId, "IX_Assets_AssignedEmployeeId");

            entity.HasIndex(e => e.CategoryId, "IX_Assets_CategoryId");

            entity.HasIndex(e => e.DepartmentId, "IX_Assets_DepartmentId");

            entity.HasIndex(e => e.LocationId, "IX_Assets_LocationId");

            entity.HasIndex(e => e.Status, "IX_Assets_Status");

            entity.HasIndex(e => e.AssetCode, "UQ__Assets__2DDE5240373C4E27").IsUnique();

            entity.HasIndex(e => e.SerialNumber, "UX_Assets_SerialNumber").IsUnique();

            entity.Property(e => e.AssetCode).HasMaxLength(50);
            entity.Property(e => e.AssetName).HasMaxLength(150);
            entity.Property(e => e.CreatedAtUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Manufacturer).HasMaxLength(100);
            entity.Property(e => e.Model).HasMaxLength(100);
            entity.Property(e => e.ModifiedAtUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.PurchaseCost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SerialNumber).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(30);

            entity.HasOne(d => d.AssetType).WithMany(p => p.Assets)
                .HasForeignKey(d => d.AssetTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Assets_AssetTypes");

            entity.HasOne(d => d.AssignedEmployee).WithMany(p => p.Assets)
                .HasForeignKey(d => d.AssignedEmployeeId)
                .HasConstraintName("FK_Assets_Employees");

            entity.HasOne(d => d.Category).WithMany(p => p.Assets)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Assets_Categories");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.AssetCreatedByUsers)
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("FK_Assets_AppUsers_CreatedBy");

            entity.HasOne(d => d.Department).WithMany(p => p.Assets)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("FK_Assets_Departments");

            entity.HasOne(d => d.Location).WithMany(p => p.Assets)
                .HasForeignKey(d => d.LocationId)
                .HasConstraintName("FK_Assets_Locations");

            entity.HasOne(d => d.ModifiedByUser).WithMany(p => p.AssetModifiedByUsers)
                .HasForeignKey(d => d.ModifiedByUserId)
                .HasConstraintName("FK_Assets_AppUsers_ModifiedBy");
        });

        modelBuilder.Entity<AssetTransfer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AssetTra__3214EC0770783600");

            entity.HasIndex(e => new { e.AssetId, e.TransferDateUtc }, "IX_AssetTransfers_AssetId_TransferDate").IsDescending(false, true);

            entity.Property(e => e.CreatedAtUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.TransferDateUtc).HasPrecision(3);

            entity.HasOne(d => d.Asset).WithMany(p => p.AssetTransfers)
                .HasForeignKey(d => d.AssetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AssetTransfers_Assets");

            entity.HasOne(d => d.FromDepartment).WithMany(p => p.AssetTransferFromDepartments)
                .HasForeignKey(d => d.FromDepartmentId)
                .HasConstraintName("FK_AssetTransfers_Departments_From");

            entity.HasOne(d => d.FromEmployee).WithMany(p => p.AssetTransferFromEmployees)
                .HasForeignKey(d => d.FromEmployeeId)
                .HasConstraintName("FK_AssetTransfers_Employees_From");

            entity.HasOne(d => d.FromLocation).WithMany(p => p.AssetTransferFromLocations)
                .HasForeignKey(d => d.FromLocationId)
                .HasConstraintName("FK_AssetTransfers_Locations_From");

            entity.HasOne(d => d.ToDepartment).WithMany(p => p.AssetTransferToDepartments)
                .HasForeignKey(d => d.ToDepartmentId)
                .HasConstraintName("FK_AssetTransfers_Departments_To");

            entity.HasOne(d => d.ToEmployee).WithMany(p => p.AssetTransferToEmployees)
                .HasForeignKey(d => d.ToEmployeeId)
                .HasConstraintName("FK_AssetTransfers_Employees_To");

            entity.HasOne(d => d.ToLocation).WithMany(p => p.AssetTransferToLocations)
                .HasForeignKey(d => d.ToLocationId)
                .HasConstraintName("FK_AssetTransfers_Locations_To");

            entity.HasOne(d => d.TransferredByUser).WithMany(p => p.AssetTransfers)
                .HasForeignKey(d => d.TransferredByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AssetTransfers_AppUsers");
        });

        modelBuilder.Entity<AssetType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AssetTyp__3214EC07515E2AA4");

            entity.HasIndex(e => e.Name, "UQ__AssetTyp__737584F66A206FAE").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Categori__3214EC07D400277F");

            entity.HasIndex(e => e.Name, "UQ__Categori__737584F638CAC619").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Departme__3214EC079D4EFD26");

            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.Organisation).WithMany(p => p.Departments)
                .HasForeignKey(d => d.OrganisationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Departments_Organisations");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Employee__3214EC072E4E6EB7");

            entity.HasIndex(e => e.Email, "UQ__Employee__A9D1053426A0EEB0").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.Department).WithMany(p => p.Employees)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("FK_Employees_Departments");
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Location__3214EC07D8643B5C");

            entity.HasIndex(e => e.Name, "UQ__Location__737584F60E4970D5").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Organisation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Organisa__3214EC07C701E206");

            entity.HasIndex(e => e.Name, "UQ__Organisa__737584F6C03525C4").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(150);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RefreshT__3214EC07D7F7794B");

            entity.HasIndex(e => e.TokenHash, "UQ__RefreshT__BCB33F9285893743").IsUnique();

            entity.Property(e => e.CreatedAtUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ExpiresAtUtc).HasPrecision(3);
            entity.Property(e => e.RevokedAtUtc).HasPrecision(3);
            entity.Property(e => e.TokenHash).HasMaxLength(128);

            entity.HasOne(d => d.AppUser).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.AppUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RefreshTokens_AppUsers");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC07317ECF9C");

            entity.HasIndex(e => e.Name, "UQ__Roles__737584F64839AD91").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
