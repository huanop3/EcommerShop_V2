using System;
using System.Collections.Generic;

namespace MainEcommerceService.Models.dbMainEcommer;

public partial class OrderStatus
{
    public int StatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
