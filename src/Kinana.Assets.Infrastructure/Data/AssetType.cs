using System;
using System.Collections.Generic;

namespace Kinana.AssetManagement.Infrastructure.Data;

public partial class AssetType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
}
