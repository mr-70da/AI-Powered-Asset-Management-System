using System;
using System.Collections.Generic;

namespace Kinana.AssetManagement.Infrastructure.Data;

public partial class AssetTransfer
{
    public int Id { get; set; }

    public int AssetId { get; set; }

    public int? FromEmployeeId { get; set; }

    public int? ToEmployeeId { get; set; }

    public int? FromDepartmentId { get; set; }

    public int? ToDepartmentId { get; set; }

    public int? FromLocationId { get; set; }

    public int? ToLocationId { get; set; }

    public DateTime TransferDateUtc { get; set; }

    public string? Reason { get; set; }

    public int TransferredByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public virtual Asset Asset { get; set; } = null!;

    public virtual Department? FromDepartment { get; set; }

    public virtual Employee? FromEmployee { get; set; }

    public virtual Location? FromLocation { get; set; }

    public virtual Department? ToDepartment { get; set; }

    public virtual Employee? ToEmployee { get; set; }

    public virtual Location? ToLocation { get; set; }

    public virtual AppUser TransferredByUser { get; set; } = null!;
}
