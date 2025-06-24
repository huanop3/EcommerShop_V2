using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MainEcommerceService.Models.dbMainEcommer;

public class SellerProfileVM
{
    public int SellerId { get; set; }

    public int UserId { get; set; }

    [Required(ErrorMessage = "Store name is required")]
    [StringLength(100, ErrorMessage = "Store name cannot exceed 100 characters")]
    public string StoreName { get; set; } = null!;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsVerified { get; set; }

    public bool? IsDeleted { get; set; }
}
