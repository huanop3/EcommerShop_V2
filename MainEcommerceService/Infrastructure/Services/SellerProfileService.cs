using Microsoft.AspNetCore.SignalR;
using MainEcommerceService.Hubs;
using System.Linq.Expressions;
using MainEcommerceService.Helper;
using MainEcommerceService.Models.dbMainEcommer;
using MainEcommerceService.Models.ViewModel;
using Microsoft.EntityFrameworkCore;
using MainEcommerceService.Kafka;

public interface ISellerProfileService
{
    Task<HTTPResponseClient<IEnumerable<SellerProfileVM>>> GetAllSellerProfiles();
    Task<HTTPResponseClient<SellerProfileVM>> GetSellerProfileById(int sellerId);
    Task<HTTPResponseClient<SellerProfileVM>> GetSellerProfileByUserId(int userId);
    Task<HTTPResponseClient<bool>> CreateSellerProfile(SellerProfileVM sellerProfileVM);
    Task<HTTPResponseClient<bool>> UpdateSellerProfile(SellerProfileVM sellerProfileVM);
    Task<HTTPResponseClient<bool>> DeleteSellerProfile(int sellerId);
    Task<HTTPResponseClient<bool>> VerifySellerProfile(int sellerId);
    Task<HTTPResponseClient<bool>> UnverifySellerProfile(int sellerId);
    Task<HTTPResponseClient<IEnumerable<SellerProfileVM>>> GetVerifiedSellerProfiles();
    Task<HTTPResponseClient<bool>> CheckUserHasSellerProfile(int userId);
    Task<HTTPResponseClient<IEnumerable<SellerProfileVM>>> GetPendingVerificationProfiles();

}

public class SellerProfileService : ISellerProfileService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly RedisHelper _cacheService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IKafkaProducerService _kafkaProducer;

    public SellerProfileService(
        IUnitOfWork unitOfWork,
        RedisHelper cacheService,
        IHubContext<NotificationHub> hubContext,
        IKafkaProducerService kafkaProducer)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _hubContext = hubContext;
        _kafkaProducer = kafkaProducer;
    }

    public async Task<HTTPResponseClient<IEnumerable<SellerProfileVM>>> GetAllSellerProfiles()
    {
        var response = new HTTPResponseClient<IEnumerable<SellerProfileVM>>();
        try
        {
            const string cacheKey = "AllSellerProfiles";

            // Kiểm tra cache trước
            var cachedProfiles = await _cacheService.GetAsync<IEnumerable<SellerProfileVM>>(cacheKey);
            if (cachedProfiles != null)
            {
                response.Data = cachedProfiles;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy danh sách seller profile từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            // Nếu không có trong cache, lấy từ database
            var sellerProfiles = await _unitOfWork._sellerProfileRepository.Query()
                .Where(s => s.IsDeleted == false)
                .Include(s => s.User) // Include User để lấy thông tin user
                .ToListAsync();

            if (sellerProfiles == null || !sellerProfiles.Any())
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy seller profile nào";
                return response;
            }

            var sellerProfileVMs = sellerProfiles.Select(s => new SellerProfileVM
            {
                SellerId = s.SellerId,
                UserId = s.UserId,
                StoreName = s.StoreName,
                Description = s.Description,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                IsVerified = s.IsVerified,
                IsDeleted = s.IsDeleted
            }).ToList();

            // Lưu vào cache
            await _cacheService.SetAsync(cacheKey, sellerProfileVMs, TimeSpan.FromDays(1));

            response.Data = sellerProfileVMs;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy danh sách seller profile thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy danh sách seller profile: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<SellerProfileVM>> GetSellerProfileById(int sellerId)
    {
        var response = new HTTPResponseClient<SellerProfileVM>();
        try
        {
            string cacheKey = $"SellerProfile_{sellerId}";
            var cachedProfile = await _cacheService.GetAsync<SellerProfileVM>(cacheKey);
            if (cachedProfile != null)
            {
                response.Data = cachedProfile;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy thông tin seller profile từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var sellerProfile = await _unitOfWork._sellerProfileRepository.Query()
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.SellerId == sellerId && s.IsDeleted == false);

            if (sellerProfile == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy seller profile";
                return response;
            }

            var sellerProfileVM = new SellerProfileVM
            {
                SellerId = sellerProfile.SellerId,
                UserId = sellerProfile.UserId,
                StoreName = sellerProfile.StoreName,
                Description = sellerProfile.Description,
                CreatedAt = sellerProfile.CreatedAt,
                UpdatedAt = sellerProfile.UpdatedAt,
                IsVerified = sellerProfile.IsVerified,
                IsDeleted = sellerProfile.IsDeleted
            };

            // Lưu vào cache
            await _cacheService.SetAsync(cacheKey, sellerProfileVM, TimeSpan.FromDays(1));

            response.Data = sellerProfileVM;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy thông tin seller profile thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy thông tin seller profile: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<SellerProfileVM>> GetSellerProfileByUserId(int userId)
    {
        var response = new HTTPResponseClient<SellerProfileVM>();
        try
        {
            string cacheKey = $"SellerProfileByUser_{userId}";
            var cachedProfile = await _cacheService.GetAsync<SellerProfileVM>(cacheKey);
            if (cachedProfile != null)
            {
                response.Data = cachedProfile;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy thông tin seller profile từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var sellerProfile = await _unitOfWork._sellerProfileRepository.Query()
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.IsDeleted == false);

            if (sellerProfile == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Người dùng chưa có seller profile";
                return response;
            }

            var sellerProfileVM = new SellerProfileVM
            {
                SellerId = sellerProfile.SellerId,
                UserId = sellerProfile.UserId,
                StoreName = sellerProfile.StoreName,
                Description = sellerProfile.Description,
                CreatedAt = sellerProfile.CreatedAt,
                UpdatedAt = sellerProfile.UpdatedAt,
                IsVerified = sellerProfile.IsVerified,
                IsDeleted = sellerProfile.IsDeleted
            };

            // Lưu vào cache
            await _cacheService.SetAsync(cacheKey, sellerProfileVM, TimeSpan.FromDays(1));

            response.Data = sellerProfileVM;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy thông tin seller profile thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy thông tin seller profile: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<bool>> CreateSellerProfile(SellerProfileVM sellerProfileVM)
    {
        var response = new HTTPResponseClient<bool>();
        try
        {
            // Bắt đầu transaction
            await _unitOfWork.BeginTransaction();

            // Kiểm tra user đã có seller profile chưa
            var existingProfile = await _unitOfWork._sellerProfileRepository.Query()
                .FirstOrDefaultAsync(s => s.UserId == sellerProfileVM.UserId && s.IsDeleted == false);

            if (existingProfile != null)
            {
                response.Success = false;
                response.StatusCode = 400;
                response.Message = "Người dùng đã có seller profile";
                return response;
            }
            // Kiểm tra user có seller profile bị xóa không và không cho tao tạo mới nếu đã bị xóa
            // Nếu có seller profile đã bị xóa, không cho tạo mới
            var deletedProfile = await _unitOfWork._sellerProfileRepository.Query()
                .FirstOrDefaultAsync(s => s.UserId == sellerProfileVM.UserId && s.IsDeleted == true);
            if (deletedProfile != null)
            {
                response.Success = false;
                response.StatusCode = 400;
                response.Message = "Người dùng đã có seller profile bị xóa, không thể tạo mới";
                return response;
            }
            // Kiểm tra user có tồn tại không
            var user = await _unitOfWork._userRepository.GetByIdAsync(sellerProfileVM.UserId);
            if (user == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy người dùng";
                return response;
            }

            // Kiểm tra tên store đã tồn tại chưa
            var existingStore = await _unitOfWork._sellerProfileRepository.Query()
                .FirstOrDefaultAsync(s => s.StoreName == sellerProfileVM.StoreName);

            if (existingStore != null)
            {
                response.Success = false;
                response.StatusCode = 400;
                response.Message = "Tên cửa hàng đã tồn tại";
                return response;
            }

            // Tạo mới seller profile entity
            var sellerProfile = new SellerProfile
            {
                UserId = sellerProfileVM.UserId,
                StoreName = sellerProfileVM.StoreName,
                Description = sellerProfileVM.Description,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsVerified = false, // Mặc định chưa được xác minh
                IsDeleted = false
            };

            await _unitOfWork._sellerProfileRepository.AddAsync(sellerProfile);
            await _unitOfWork.SaveChangesAsync();

            // Commit transaction khi tất cả thành công
            await _unitOfWork.CommitTransaction();

            // Xóa cache đầy đủ
            await InvalidateAllSellerProfileCaches(sellerProfile.SellerId, sellerProfileVM.UserId);

            // Gửi thông báo realtime qua SignalR
            await _hubContext.Clients.All.SendAsync("SellerProfileCreated", sellerProfile.SellerId, sellerProfile.StoreName);

            response.Success = true;
            response.StatusCode = 201;
            response.Message = "Tạo seller profile thành công";
            response.Data = true;
        }
        catch (Exception ex)
        {
            // Rollback transaction nếu có lỗi
            await _unitOfWork.RollbackTransaction();

            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi tạo seller profile: {ex.Message}";
        }
        return response;
    }

    public async Task<HTTPResponseClient<bool>> UpdateSellerProfile(SellerProfileVM sellerProfileVM)
    {
        var response = new HTTPResponseClient<bool>();
        try
        {
            // Bắt đầu transaction
            await _unitOfWork.BeginTransaction();

            var sellerProfile = await _unitOfWork._sellerProfileRepository.GetByIdAsync(sellerProfileVM.SellerId);
            if (sellerProfile == null || sellerProfile.IsDeleted == true)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy seller profile";
                return response;
            }

            // Kiểm tra tên store có bị trùng không (ngoại trừ chính nó)
            var existingStore = await _unitOfWork._sellerProfileRepository.Query()
                .FirstOrDefaultAsync(s => s.StoreName == sellerProfileVM.StoreName &&
                                         s.SellerId != sellerProfileVM.SellerId &&
                                         s.IsDeleted == false);

            if (existingStore != null)
            {
                response.Success = false;
                response.StatusCode = 400;
                response.Message = "Tên cửa hàng đã tồn tại";
                return response;
            }

            // Cập nhật thông tin
            sellerProfile.StoreName = sellerProfileVM.StoreName;
            sellerProfile.Description = sellerProfileVM.Description;
            sellerProfile.UpdatedAt = DateTime.Now;

            _unitOfWork._sellerProfileRepository.Update(sellerProfile);
            await _unitOfWork.SaveChangesAsync();

            // Commit transaction khi tất cả thành công
            await _unitOfWork.CommitTransaction();

            // Xóa cache đầy đủ
            await InvalidateAllSellerProfileCaches(sellerProfileVM.SellerId, sellerProfile.UserId);

            // Gửi thông báo realtime qua SignalR
            await _hubContext.Clients.All.SendAsync("SellerProfileUpdated", sellerProfile.SellerId, sellerProfile.StoreName);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Cập nhật seller profile thành công";
            response.Data = true;
        }
        catch (Exception ex)
        {
            // Rollback transaction nếu có lỗi
            await _unitOfWork.RollbackTransaction();

            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi cập nhật seller profile: {ex.Message}";
        }
        return response;
    }

    public async Task<HTTPResponseClient<bool>> DeleteSellerProfile(int sellerId)
    {
        var response = new HTTPResponseClient<bool>();
        try
        {
            // Bắt đầu transaction
            await _unitOfWork.BeginTransaction();

            var sellerProfile = await _unitOfWork._sellerProfileRepository.GetByIdAsync(sellerId);
            if (sellerProfile == null || sellerProfile.IsDeleted == true)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy seller profile";
                return response;
            }

            // Đặt trạng thái xóa (soft delete)
            sellerProfile.IsDeleted = true;
            sellerProfile.UpdatedAt = DateTime.Now;
            _unitOfWork._sellerProfileRepository.Update(sellerProfile);
            
            await _unitOfWork.SaveChangesAsync();

            // Commit transaction trước khi gửi Kafka message
            await _unitOfWork.CommitTransaction();

            // Gửi message tới Kafka để xóa các sản phẩm liên quan
            try
            {
                var sellerDeletedMessage = new SellerProfileVM
                {
                    SellerId = sellerProfile.SellerId,
                    UserId = sellerProfile.UserId,
                    StoreName = sellerProfile.StoreName,
                };

                await _kafkaProducer.SendMessageAsync(
                    "seller-events", 
                    $"seller-{sellerProfile.SellerId}", 
                    sellerDeletedMessage);

            }
            catch (Exception kafkaEx)
            {
                // Log lỗi Kafka nhưng không rollback transaction
                // Có thể implement retry mechanism hoặc dead letter queue ở đây
            }

            // Xóa cache đầy đủ
            await InvalidateAllSellerProfileCaches(sellerId, sellerProfile.UserId);

            // Gửi thông báo realtime qua SignalR
            await _hubContext.Clients.All.SendAsync("SellerProfileDeleted", sellerId);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Xóa seller profile thành công";
            response.Data = true;
        }
        catch (Exception ex)
        {
            // Rollback transaction nếu có lỗi
            await _unitOfWork.RollbackTransaction();

            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi xóa seller profile: {ex.Message}";
        }
        return response;
    }

    public async Task<HTTPResponseClient<bool>> VerifySellerProfile(int sellerId)
    {
        var response = new HTTPResponseClient<bool>();
        try
        {
            await _unitOfWork.BeginTransaction();

            var sellerProfile = await _unitOfWork._sellerProfileRepository.GetByIdAsync(sellerId);
            if (sellerProfile == null || sellerProfile.IsDeleted == true)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy seller profile";
                return response;
            }
            if (sellerProfile.IsVerified == true)
            {
                response.Success = false;
                response.StatusCode = 400;
                response.Message = "Seller profile đã được xác minh trước đó";
                return response;
            }

            // ❌ THIẾU CODE CẬP NHẬT DỮ LIỆU!
            sellerProfile.IsVerified = true;
            sellerProfile.UpdatedAt = DateTime.Now;
            _unitOfWork._sellerProfileRepository.Update(sellerProfile);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            // Xóa cache đầy đủ
            await InvalidateAllSellerProfileCaches(sellerId, sellerProfile.UserId);

            await _hubContext.Clients.All.SendAsync("SellerProfileVerified", sellerId);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Xác minh seller profile thành công";
            response.Data = true;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi xác minh seller profile: {ex.Message}";
        }
        return response;
    }

    public async Task<HTTPResponseClient<bool>> UnverifySellerProfile(int sellerId)
    {
        var response = new HTTPResponseClient<bool>();
        try
        {
            await _unitOfWork.BeginTransaction();

            var sellerProfile = await _unitOfWork._sellerProfileRepository.GetByIdAsync(sellerId);
            if (sellerProfile == null || sellerProfile.IsDeleted == true)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy seller profile";
                return response;
            }
            sellerProfile.IsVerified = false;
            sellerProfile.UpdatedAt = DateTime.Now;
            _unitOfWork._sellerProfileRepository.Update(sellerProfile);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            // Xóa cache đầy đủ
            await InvalidateAllSellerProfileCaches(sellerId, sellerProfile.UserId);

            await _hubContext.Clients.All.SendAsync("SellerProfileUnverified", sellerId);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Hủy xác minh seller profile thành công";
            response.Data = true;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi hủy xác minh seller profile: {ex.Message}";
        }
        return response;
    }

    public async Task<HTTPResponseClient<IEnumerable<SellerProfileVM>>> GetVerifiedSellerProfiles()
    {
        var response = new HTTPResponseClient<IEnumerable<SellerProfileVM>>();
        try
        {
            const string cacheKey = "VerifiedSellerProfiles";
            var cachedProfiles = await _cacheService.GetAsync<IEnumerable<SellerProfileVM>>(cacheKey);
            if (cachedProfiles != null)
            {
                response.Data = cachedProfiles;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy danh sách seller profile đã xác minh từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var sellerProfiles = await _unitOfWork._sellerProfileRepository.Query()
                .Where(s => s.IsDeleted == false && s.IsVerified == true)
                .Include(s => s.User)
                .ToListAsync();

            var sellerProfileVMs = sellerProfiles.Select(s => new SellerProfileVM
            {
                SellerId = s.SellerId,
                UserId = s.UserId,
                StoreName = s.StoreName,
                Description = s.Description,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                IsVerified = s.IsVerified,
                IsDeleted = s.IsDeleted
            }).ToList();

            await _cacheService.SetAsync(cacheKey, sellerProfileVMs, TimeSpan.FromDays(1));

            response.Data = sellerProfileVMs;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy danh sách seller profile đã xác minh thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy danh sách seller profile đã xác minh: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<bool>> CheckUserHasSellerProfile(int userId)
    {
        var response = new HTTPResponseClient<bool>();
        try
        {
            var hasProfile = await _unitOfWork._sellerProfileRepository.Query()
                .AnyAsync(s => s.UserId == userId && s.IsDeleted == false);

            response.Data = hasProfile;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = hasProfile ? "Người dùng đã có seller profile" : "Người dùng chưa có seller profile";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi kiểm tra seller profile: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<IEnumerable<SellerProfileVM>>> GetPendingVerificationProfiles()
    {
        var response = new HTTPResponseClient<IEnumerable<SellerProfileVM>>();
        try
        {
            const string cacheKey = "PendingVerificationProfiles";
            var cachedProfiles = await _cacheService.GetAsync<IEnumerable<SellerProfileVM>>(cacheKey);
            if (cachedProfiles != null)
            {
                response.Data = cachedProfiles;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy danh sách seller profile chờ xác minh từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var sellerProfiles = await _unitOfWork._sellerProfileRepository.Query()
                .Where(s => s.IsDeleted == false && s.IsVerified == false)
                .Include(s => s.User)
                .ToListAsync();

            var sellerProfileVMs = sellerProfiles.Select(s => new SellerProfileVM
            {
                SellerId = s.SellerId,
                UserId = s.UserId,
                StoreName = s.StoreName,
                Description = s.Description,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                IsVerified = s.IsVerified,
                IsDeleted = s.IsDeleted
            }).ToList();

            await _cacheService.SetAsync(cacheKey, sellerProfileVMs, TimeSpan.FromDays(1));

            response.Data = sellerProfileVMs;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy danh sách seller profile chờ xác minh thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy danh sách seller profile chờ xác minh: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    private async Task InvalidateAllSellerProfileCaches(int sellerId, int userId)
    {
        var cacheKeys = new[]
        {
            "AllSellerProfiles",
            "VerifiedSellerProfiles",
            "PendingVerificationProfiles",
            $"SellerProfile_{sellerId}",
            $"SellerProfileByUser_{userId}"
        };

        var tasks = cacheKeys.Select(key => _cacheService.DeleteByPatternAsync(key));
        await Task.WhenAll(tasks);
    }
}