using System;
using System.Collections.Generic;

namespace Kinana.AssetManagement.Infrastructure.Data;

public partial class Department
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int OrganisationId { get; set; }

    public virtual ICollection<AssetTransfer> AssetTransferFromDepartments { get; set; } = new List<AssetTransfer>();

    public virtual ICollection<AssetTransfer> AssetTransferToDepartments { get; set; } = new List<AssetTransfer>();

    public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public virtual Organisation Organisation { get; set; } = null!;
}
