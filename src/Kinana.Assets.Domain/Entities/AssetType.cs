using System;
using System.Collections.Generic;

namespace Kinana.AssetManagement.Domain.Entities;

public partial class AssetType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
}
