using System;
using System.Collections.Generic;

namespace MainEcommerceService.Models.dbMainEcommer;

public partial class Address
{
    public int AddressId { get; set; }

    public int UserId { get; set; }

    public string AddressLine1 { get; set; } = null!;

    public string? AddressLine2 { get; set; }

    public string City { get; set; } = null!;

    public string? State { get; set; }

    public string? PostalCode { get; set; }

    public string Country { get; set; } = null!;

    public bool? IsDefault { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual User User { get; set; } = null!;
}
