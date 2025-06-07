using System.ComponentModel.DataAnnotations;

namespace MainEcommerceService.Models.ViewModel
{
    /// <summary>
    /// View model dùng để hiển thị thông tin địa chỉ
    /// </summary>
    public class AddressVM
    {
        public int AddressId { get; set; }
        public int UserId { get; set; }

        [Required(ErrorMessage = "Địa chỉ dòng 1 là bắt buộc")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Địa chỉ dòng 1 phải từ 5 đến 200 ký tự")]
        public string AddressLine1 { get; set; } = null!;

        [StringLength(200, ErrorMessage = "Địa chỉ dòng 2 không được vượt quá 200 ký tự")]
        public string? AddressLine2 { get; set; }

        [Required(ErrorMessage = "Thành phố là bắt buộc")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên thành phố phải từ 2 đến 100 ký tự")]
        [RegularExpression(@"^[a-zA-ZÀ-ỹ\s]+$", ErrorMessage = "Tên thành phố chỉ được chứa chữ cái và khoảng trắng")]
        public string City { get; set; } = null!;

        [StringLength(100, ErrorMessage = "Tên tỉnh/bang không được vượt quá 100 ký tự")]
        [RegularExpression(@"^[a-zA-ZÀ-ỹ\s]*$", ErrorMessage = "Tên tỉnh/bang chỉ được chứa chữ cái và khoảng trắng")]
        public string? State { get; set; }

        [StringLength(20, ErrorMessage = "Mã bưu điện không được vượt quá 20 ký tự")]
        [RegularExpression(@"^[0-9\s\-]*$", ErrorMessage = "Mã bưu điện chỉ được chứa số, dấu gạch ngang và khoảng trắng")]
        public string? PostalCode { get; set; }

        [Required(ErrorMessage = "Quốc gia là bắt buộc")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên quốc gia phải từ 2 đến 100 ký tự")]
        [RegularExpression(@"^[a-zA-ZÀ-ỹ\s]+$", ErrorMessage = "Tên quốc gia chỉ được chứa chữ cái và khoảng trắng")]
        public string Country { get; set; } = null!;

        public bool? IsDefault { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool? IsDeleted { get; set; }
    }
}
