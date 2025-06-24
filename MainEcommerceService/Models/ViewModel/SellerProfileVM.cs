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
public class SellerRequestMessage
{
    public string Action { get; set; } // "GET_SELLER_BY_USER_ID"
    public int UserId { get; set; }
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
}

public class SellerResponseMessage
{
    public string RequestId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public SellerProfileVM? Data { get; set; } // 🔥 FIX: Đây là property chính
    public string? ErrorMessage { get; set; }
    
    // 🔥 ADD: Helper properties để backward compatibility (optional)
    public int SellerId => Data?.SellerId ?? 0;
    public string StoreName => Data?.StoreName ?? string.Empty;
}