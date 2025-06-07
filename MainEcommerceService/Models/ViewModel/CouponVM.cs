using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MainEcommerceService.Models.ViewModel
{
    /// <summary>
    /// View model dùng để hiển thị thông tin mã giảm giá
    /// </summary>
    public class CouponVM
    {
    public int CouponId { get; set; }

    [Required(ErrorMessage = "Mã giảm giá là bắt buộc")]
    [StringLength(50, ErrorMessage = "Mã giảm giá không được vượt quá 50 ký tự")]
    public string CouponCode { get; set; } = null!;

    [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
    public string? Description { get; set; }

    [Range(0, 100, ErrorMessage = "Phần trăm giảm giá phải từ 0 đến 100")]
    public decimal? DiscountPercent { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Số tiền giảm giá phải lớn hơn 0")]
    public decimal? DiscountAmount { get; set; }

    [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
    public DateTime EndDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsDeleted { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Giới hạn sử dụng phải lớn hơn 0")]
    public int? UsageLimit { get; set; }

    public int? UsageCount { get; set; }
    }

}