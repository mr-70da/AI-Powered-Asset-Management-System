using System;
using System.Collections.Generic;

namespace Kinana.AssetManagement.Domain.Entities;

public partial class AppUser
{
    public int Id { get; set; }

    public string UserName { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public int RoleId { get; set; }

    public bool IsDisabled { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public virtual ICollection<Asset> AssetCreatedByUsers { get; set; } = new List<Asset>();

    public virtual ICollection<Asset> AssetModifiedByUsers { get; set; } = new List<Asset>();

    public virtual ICollection<AssetTransfer> AssetTransfers { get; set; } = new List<AssetTransfer>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual Role Role { get; set; } = null!;
}
