using System;
using System.Collections.Generic;

namespace Kinana.AssetManagement.Domain.Entities;

public partial class Organisation
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Department> Departments { get; set; } = new List<Department>();
}
