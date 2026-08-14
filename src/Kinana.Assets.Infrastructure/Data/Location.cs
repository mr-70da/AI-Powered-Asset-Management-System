using System;
using System.Collections.Generic;

namespace Kinana.AssetManagement.Infrastructure.Data;

public partial class Location
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<AssetTransfer> AssetTransferFromLocations { get; set; } = new List<AssetTransfer>();

    public virtual ICollection<AssetTransfer> AssetTransferToLocations { get; set; } = new List<AssetTransfer>();

    public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
}
