using System;
using System.Collections.Generic;

namespace MainEcommerceService.Models.dbMainEcommer;

public partial class UserCoupon
{
    public int UserCouponId { get; set; }

    public int UserId { get; set; }

    public int CouponId { get; set; }

    public bool? IsUsed { get; set; }

    public DateTime? UsedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual Coupon Coupon { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
