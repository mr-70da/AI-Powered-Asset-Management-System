using System;
using System.Collections.Generic;

namespace Kinana.AssetManagement.Domain.Entities;

public partial class Employee
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Email { get; set; }

    public int? DepartmentId { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<AssetTransfer> AssetTransferFromEmployees { get; set; } = new List<AssetTransfer>();

    public virtual ICollection<AssetTransfer> AssetTransferToEmployees { get; set; } = new List<AssetTransfer>();

    public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();

    public virtual Department? Department { get; set; }
}
