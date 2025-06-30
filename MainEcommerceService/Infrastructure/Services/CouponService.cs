using Microsoft.AspNetCore.SignalR;
using MainEcommerceService.Hubs;
using System.Linq.Expressions;
using MainEcommerceService.Helper;
using MainEcommerceService.Models.dbMainEcommer;
using MainEcommerceService.Models.ViewModel;
using Microsoft.EntityFrameworkCore;

public interface ICouponService
{
    Task<HTTPResponseClient<IEnumerable<CouponVM>>> GetAllCoupons();
    Task<HTTPResponseClient<CouponVM>> GetCouponById(int couponId);
    Task<HTTPResponseClient<CouponVM>> GetCouponByCode(string couponCode);
    Task<HTTPResponseClient<string>> CreateCoupon(CouponVM couponVM);
    Task<HTTPResponseClient<string>> UpdateCoupon(CouponVM couponVM);
    Task<HTTPResponseClient<string>> DeleteCoupon(int couponId);
    Task<HTTPResponseClient<string>> ActivateCoupon(int couponId);
    Task<HTTPResponseClient<string>> DeactivateCoupon(int couponId);
    Task<HTTPResponseClient<IEnumerable<CouponVM>>> GetActiveCoupons();
    Task<HTTPResponseClient<bool>> ValidateCoupon(string couponCode);
    Task<HTTPResponseClient<bool>> UpdateCouponUsageCount(int couponId);
}

public class CouponService : ICouponService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly RedisHelper _cacheService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public CouponService(
        IUnitOfWork unitOfWork,
        RedisHelper cacheService,
        IHubContext<NotificationHub> hubContext)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _hubContext = hubContext;
    }

    public async Task<HTTPResponseClient<IEnumerable<CouponVM>>> GetAllCoupons()
    {
        var response = new HTTPResponseClient<IEnumerable<CouponVM>>();
        try
        {
            const string cacheKey = "AllCoupons";

            // Kiểm tra cache trước
            var cachedCoupons = await _cacheService.GetAsync<IEnumerable<CouponVM>>(cacheKey);
            if (cachedCoupons != null)
            {
                response.Data = cachedCoupons;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy danh sách mã giảm giá từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            // Nếu không có trong cache, lấy từ database
            var coupons = await _unitOfWork._couponRepository.Query()
                .Where(c => c.IsDeleted == false)
                .ToListAsync();

            if (coupons == null || !coupons.Any())
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy mã giảm giá nào";
                return response;
            }

            var couponVMs = coupons.Select(c => new CouponVM
            {
                CouponId = c.CouponId,
                CouponCode = c.CouponCode,
                Description = c.Description,
                DiscountPercent = c.DiscountPercent,
                DiscountAmount = c.DiscountAmount,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                IsActive = c.IsActive,
                IsDeleted = c.IsDeleted,
                UsageLimit = c.UsageLimit,
                UsageCount = c.UsageCount
            }).ToList();

            // Lưu vào cache
            await _cacheService.SetAsync(cacheKey, couponVMs, TimeSpan.FromDays(1));

            response.Data = couponVMs;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy danh sách mã giảm giá thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy danh sách mã giảm giá: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<CouponVM>> GetCouponById(int couponId)
    {
        var response = new HTTPResponseClient<CouponVM>();
        try
        {
            string cacheKey = $"Coupon_{couponId}";
            var cachedCoupon = await _cacheService.GetAsync<CouponVM>(cacheKey);
            if (cachedCoupon != null)
            {
                response.Data = cachedCoupon;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy thông tin mã giảm giá từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var coupon = await _unitOfWork._couponRepository.Query()
                .FirstOrDefaultAsync(c => c.CouponId == couponId && c.IsDeleted == false);

            if (coupon == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy mã giảm giá";
                return response;
            }

            var couponVM = new CouponVM
            {
                CouponId = coupon.CouponId,
                CouponCode = coupon.CouponCode,
                Description = coupon.Description,
                DiscountPercent = coupon.DiscountPercent,
                DiscountAmount = coupon.DiscountAmount,
                StartDate = coupon.StartDate,
                EndDate = coupon.EndDate,
                CreatedAt = coupon.CreatedAt,
                UpdatedAt = coupon.UpdatedAt,
                IsActive = coupon.IsActive,
                IsDeleted = coupon.IsDeleted,
                UsageLimit = coupon.UsageLimit,
                UsageCount = coupon.UsageCount
            };

            // Lưu vào cache
            await _cacheService.SetAsync(cacheKey, couponVM, TimeSpan.FromDays(1));

            response.Data = couponVM;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy thông tin mã giảm giá thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy thông tin mã giảm giá: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<CouponVM>> GetCouponByCode(string couponCode)
    {
        var response = new HTTPResponseClient<CouponVM>();
        try
        {
            string cacheKey = $"CouponCode_{couponCode}";
            var cachedCoupon = await _cacheService.GetAsync<CouponVM>(cacheKey);
            if (cachedCoupon != null)
            {
                response.Data = cachedCoupon;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy thông tin mã giảm giá từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var coupon = await _unitOfWork._couponRepository.Query()
                .FirstOrDefaultAsync(c => c.CouponCode == couponCode && c.IsDeleted == false);

            if (coupon == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy mã giảm giá";
                return response;
            }

            var couponVM = new CouponVM
            {
                CouponId = coupon.CouponId,
                CouponCode = coupon.CouponCode,
                Description = coupon.Description,
                DiscountPercent = coupon.DiscountPercent,
                DiscountAmount = coupon.DiscountAmount,
                StartDate = coupon.StartDate,
                EndDate = coupon.EndDate,
                CreatedAt = coupon.CreatedAt,
                UpdatedAt = coupon.UpdatedAt,
                IsActive = coupon.IsActive,
                IsDeleted = coupon.IsDeleted,
                UsageLimit = coupon.UsageLimit,
                UsageCount = coupon.UsageCount
            };

            // Lưu vào cache
            await _cacheService.SetAsync(cacheKey, couponVM, TimeSpan.FromDays(1));

            response.Data = couponVM;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy thông tin mã giảm giá thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy thông tin mã giảm giá: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<string>> CreateCoupon(CouponVM couponVM)
    {
        var response = new HTTPResponseClient<string>();
        try
        {
            // Bắt đầu transaction
            await _unitOfWork.BeginTransaction();

            // Kiểm tra mã giảm giá đã tồn tại chưa
            var existingCoupon = await _unitOfWork._couponRepository.Query()
                .FirstOrDefaultAsync(c => c.CouponCode == couponVM.CouponCode && c.IsDeleted == false);

            if (existingCoupon != null)
            {
                response.Success = false;
                response.StatusCode = 400;
                response.Message = "Mã giảm giá đã tồn tại";
                return response;
            }

            // Tạo mới coupon entity
            var coupon = new Coupon
            {
                CouponCode = couponVM.CouponCode,
                Description = couponVM.Description,
                DiscountPercent = couponVM.DiscountPercent,
                DiscountAmount = couponVM.DiscountAmount,
                StartDate = couponVM.StartDate,
                EndDate = couponVM.EndDate,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsActive = couponVM.IsActive ?? true,
                IsDeleted = false,
                UsageLimit = couponVM.UsageLimit,
                UsageCount = 0
            };

            await _unitOfWork._couponRepository.AddAsync(coupon);
            await _unitOfWork.SaveChangesAsync();

            // Commit transaction khi tất cả thành công
            await _unitOfWork.CommitTransaction();

            // Xóa cache để đảm bảo dữ liệu mới nhất
            await _cacheService.DeleteByPatternAsync("AllCoupons");
            await _cacheService.DeleteByPatternAsync("PagedCoupons_*");

            // Gửi thông báo realtime qua SignalR
            await _hubContext.Clients.All.SendAsync("CouponCreated", coupon.CouponId, coupon.CouponCode);

            response.Success = true;
            response.StatusCode = 201;
            response.Message = "Tạo mã giảm giá thành công";
            response.Data = "Tạo thành công";
        }
        catch (Exception ex)
        {
            // Rollback transaction nếu có lỗi
            await _unitOfWork.RollbackTransaction();

            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi tạo mã giảm giá: {ex.Message}";
        }
        return response;
    }

    public async Task<HTTPResponseClient<string>> UpdateCoupon(CouponVM couponVM)
    {
        var response = new HTTPResponseClient<string>();
        try
        {
            // Bắt đầu transaction
            await _unitOfWork.BeginTransaction();

            var coupon = await _unitOfWork._couponRepository.GetByIdAsync(couponVM.CouponId);
            if (coupon == null || coupon.IsDeleted == true)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy mã giảm giá";
                return response;
            }

            // Kiểm tra mã giảm giá có bị trùng không (ngoại trừ chính nó)
            var existingCoupon = await _unitOfWork._couponRepository.Query()
                .FirstOrDefaultAsync(c => c.CouponCode == couponVM.CouponCode &&
                                         c.CouponId != couponVM.CouponId &&
                                         c.IsDeleted == false);

            if (existingCoupon != null)
            {
                response.Success = false;
                response.StatusCode = 400;
                response.Message = "Mã giảm giá đã tồn tại";
                return response;
            }

            // Cập nhật thông tin
            coupon.CouponCode = couponVM.CouponCode;
            coupon.Description = couponVM.Description;
            coupon.DiscountPercent = couponVM.DiscountPercent;
            coupon.DiscountAmount = couponVM.DiscountAmount;
            coupon.StartDate = couponVM.StartDate;
            coupon.EndDate = couponVM.EndDate;
            coupon.UpdatedAt = DateTime.Now;
            coupon.IsActive = couponVM.IsActive;
            coupon.UsageLimit = couponVM.UsageLimit;

            _unitOfWork._couponRepository.Update(coupon);
            await _unitOfWork.SaveChangesAsync();

            // Commit transaction khi tất cả thành công
            await _unitOfWork.CommitTransaction();

            // Xóa cache để đảm bảo dữ liệu mới nhất
            await _cacheService.DeleteByPatternAsync("AllCoupons");
            await _cacheService.DeleteByPatternAsync("PagedCoupons_*");
            await _cacheService.DeleteByPatternAsync($"Coupon_{couponVM.CouponId}");
            await _cacheService.DeleteByPatternAsync($"CouponCode_{couponVM.CouponCode}");

            // Gửi thông báo realtime qua SignalR
            await _hubContext.Clients.All.SendAsync("CouponUpdated", coupon.CouponId, coupon.CouponCode);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Cập nhật mã giảm giá thành công";
            response.Data = "Cập nhật thành công";
        }
        catch (Exception ex)
        {
            // Rollback transaction nếu có lỗi
            await _unitOfWork.RollbackTransaction();

            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi cập nhật mã giảm giá: {ex.Message}";
        }
        return response;
    }

    public async Task<HTTPResponseClient<string>> DeleteCoupon(int couponId)
    {
        var response = new HTTPResponseClient<string>();
        try
        {
            // Bắt đầu transaction
            await _unitOfWork.BeginTransaction();

            var coupon = await _unitOfWork._couponRepository.GetByIdAsync(couponId);
            if (coupon == null || coupon.IsDeleted == true)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy mã giảm giá";
                return response;
            }

            // Đặt trạng thái xóa
            coupon.IsDeleted = true;
            coupon.UpdatedAt = DateTime.Now;
            _unitOfWork._couponRepository.Update(coupon);

            await _unitOfWork.SaveChangesAsync();

            // Commit transaction khi tất cả thành công
            await _unitOfWork.CommitTransaction();

            // Xóa cache
            await _cacheService.DeleteByPatternAsync("AllCoupons");
            await _cacheService.DeleteByPatternAsync("PagedCoupons_*");
            await _cacheService.DeleteByPatternAsync($"Coupon_{couponId}");
            await _cacheService.DeleteByPatternAsync($"CouponCode_{coupon.CouponCode}");

            // Gửi thông báo realtime qua SignalR
            await _hubContext.Clients.All.SendAsync("CouponDeleted", couponId);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Xóa mã giảm giá thành công";
            response.Data = "Xóa thành công";
        }
        catch (Exception ex)
        {
            // Rollback transaction nếu có lỗi
            await _unitOfWork.RollbackTransaction();

            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi xóa mã giảm giá: {ex.Message}";
        }
        return response;
    }

    public async Task<HTTPResponseClient<string>> ActivateCoupon(int couponId)
    {
        var response = new HTTPResponseClient<string>();
        try
        {
            await _unitOfWork.BeginTransaction();

            var coupon = await _unitOfWork._couponRepository.GetByIdAsync(couponId);
            if (coupon == null || coupon.IsDeleted == true)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy mã giảm giá";
                return response;
            }

            coupon.IsActive = true;
            coupon.UpdatedAt = DateTime.Now;
            _unitOfWork._couponRepository.Update(coupon);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            // Xóa cache
            await _cacheService.DeleteByPatternAsync("AllCoupons");
            await _cacheService.DeleteByPatternAsync("PagedCoupons_*");
            await _cacheService.DeleteByPatternAsync($"Coupon_{couponId}");

            await _hubContext.Clients.All.SendAsync("CouponActivated", couponId);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Kích hoạt mã giảm giá thành công";
            response.Data = "Kích hoạt thành công";
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi kích hoạt mã giảm giá: {ex.Message}";
        }
        return response;
    }

    public async Task<HTTPResponseClient<string>> DeactivateCoupon(int couponId)
    {
        var response = new HTTPResponseClient<string>();
        try
        {
            await _unitOfWork.BeginTransaction();

            var coupon = await _unitOfWork._couponRepository.GetByIdAsync(couponId);
            if (coupon == null || coupon.IsDeleted == true)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy mã giảm giá";
                return response;
            }

            coupon.IsActive = false;
            coupon.UpdatedAt = DateTime.Now;
            _unitOfWork._couponRepository.Update(coupon);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            // Xóa cache
            await _cacheService.DeleteByPatternAsync("AllCoupons");
            await _cacheService.DeleteByPatternAsync("PagedCoupons_*");
            await _cacheService.DeleteByPatternAsync($"Coupon_{couponId}");

            await _hubContext.Clients.All.SendAsync("CouponDeactivated", couponId);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Vô hiệu hóa mã giảm giá thành công";
            response.Data = "Vô hiệu hóa thành công";
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi vô hiệu hóa mã giảm giá: {ex.Message}";
        }
        return response;
    }

    public async Task<HTTPResponseClient<IEnumerable<CouponVM>>> GetActiveCoupons()
    {
        var response = new HTTPResponseClient<IEnumerable<CouponVM>>();
        try
        {
            const string cacheKey = "ActiveCoupons";
            var cachedCoupons = await _cacheService.GetAsync<IEnumerable<CouponVM>>(cacheKey);
            if (cachedCoupons != null)
            {
                response.Data = cachedCoupons;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy danh sách mã giảm giá hoạt động từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var currentDate = DateTime.Now;
            var coupons = await _unitOfWork._couponRepository.Query()
                .Where(c => c.IsDeleted == false &&
                           c.IsActive == true &&
                           c.StartDate <= currentDate &&
                           c.EndDate >= currentDate)
                .ToListAsync();

            var couponVMs = coupons.Select(c => new CouponVM
            {
                CouponId = c.CouponId,
                CouponCode = c.CouponCode,
                Description = c.Description,
                DiscountPercent = c.DiscountPercent,
                DiscountAmount = c.DiscountAmount,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                IsActive = c.IsActive,
                IsDeleted = c.IsDeleted,
                UsageLimit = c.UsageLimit,
                UsageCount = c.UsageCount
            }).ToList();

            await _cacheService.SetAsync(cacheKey, couponVMs, TimeSpan.FromDays(1));

            response.Data = couponVMs;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy danh sách mã giảm giá hoạt động thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy danh sách mã giảm giá hoạt động: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<bool>> ValidateCoupon(string couponCode)
    {
        var response = new HTTPResponseClient<bool>();
        try
        {
            var coupon = await _unitOfWork._couponRepository.Query()
                .FirstOrDefaultAsync(c => c.CouponCode == couponCode && c.IsDeleted == false);

            if (coupon == null)
            {
                response.Data = false;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Mã giảm giá không tồn tại";
                return response;
            }

            var currentDate = DateTime.Now;
            bool isValid = coupon.IsActive == true &&
                          coupon.StartDate <= currentDate &&
                          coupon.EndDate >= currentDate &&
                          (coupon.UsageLimit == null || coupon.UsageCount < coupon.UsageLimit);

            response.Data = isValid;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = isValid ? "Mã giảm giá hợp lệ" : "Mã giảm giá không hợp lệ hoặc đã hết hạn";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi kiểm tra mã giảm giá: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }
    public async Task<HTTPResponseClient<bool>> UpdateCouponUsageCount(int couponId)
    {
        var response = new HTTPResponseClient<bool>();
        try
        {
            await _unitOfWork.BeginTransaction();
            var coupon = await _unitOfWork._couponRepository.GetByIdAsync(couponId);
            if (coupon == null || coupon.IsDeleted == true)
            {
                response.Data = false;
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy mã giảm giá";
                return response;
            }

            coupon.UsageCount++;
            _unitOfWork._couponRepository.Update(coupon);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();
            // Xóa cache để đảm bảo dữ liệu mới nhất
            await _cacheService.DeleteByPatternAsync("AllCoupons");
            await _cacheService.DeleteByPatternAsync("PagedCoupons_*");
            await _cacheService.DeleteByPatternAsync($"Coupon_{couponId}_*");

            response.Data = true;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Cập nhật số lần sử dụng mã giảm giá thành công";
        }
        catch (Exception ex)
        {
            response.Data = false;
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi cập nhật số lần sử dụng mã giảm giá: {ex.Message}";
        }
        return response;
    }
}