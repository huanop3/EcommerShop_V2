using System.ComponentModel.DataAnnotations;

namespace MainEcommerceService.Models.ViewModel
{
    /// <summary>
    /// View model dùng để hiển thị thông tin địa chỉ
    /// </summary>
    public class PaymentVM
    {
    public int PaymentId { get; set; }

    public int OrderId { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string? TransactionId { get; set; }

    public DateTime PaymentDate { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }
    }
}
