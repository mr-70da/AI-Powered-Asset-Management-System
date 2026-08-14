using System;
using System.Collections.Generic;

namespace Kinana.AssetManagement.Infrastructure.Data;

public partial class RefreshToken
{
    public int Id { get; set; }

    public int AppUserId { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public virtual AppUser AppUser { get; set; } = null!;
}
