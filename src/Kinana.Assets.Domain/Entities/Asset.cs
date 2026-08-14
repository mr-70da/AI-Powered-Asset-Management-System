using System;
using System.Collections.Generic;

namespace Kinana.AssetManagement.Domain.Entities;

public partial class Asset
{
    public int Id { get; set; }

    public string AssetCode { get; set; } = null!;

    public string AssetName { get; set; } = null!;

    public string? Description { get; set; }

    public int CategoryId { get; set; }

    public int AssetTypeId { get; set; }

    public string Manufacturer { get; set; } = null!;

    public string Model { get; set; } = null!;

    public string? SerialNumber { get; set; }

    public DateOnly? PurchaseDate { get; set; }

    public decimal? PurchaseCost { get; set; }

    public DateOnly? WarrantyExpiryDate { get; set; }

    public string Status { get; set; } = null!;

    public int? DepartmentId { get; set; }

    public int? AssignedEmployeeId { get; set; }

    public int? LocationId { get; set; }

    public int? CreatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public int? ModifiedByUserId { get; set; }

    public DateTime ModifiedAtUtc { get; set; }

    public byte[]? RowVersion { get; set; }

    public virtual ICollection<AssetTransfer> AssetTransfers { get; set; } = new List<AssetTransfer>();

    public virtual AssetType AssetType { get; set; } = null!;

    public virtual Employee? AssignedEmployee { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual AppUser? CreatedByUser { get; set; }

    public virtual Department? Department { get; set; }

    public virtual Location? Location { get; set; }

    public virtual AppUser? ModifiedByUser { get; set; }
}
