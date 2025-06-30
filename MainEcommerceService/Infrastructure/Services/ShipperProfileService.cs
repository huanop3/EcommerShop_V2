using Microsoft.AspNetCore.SignalR;
using MainEcommerceService.Hubs;
using System.Linq.Expressions;
using MainEcommerceService.Helper;
using MainEcommerceService.Models.dbMainEcommer;
using MainEcommerceService.Models.ViewModel;
using Microsoft.EntityFrameworkCore;
using MainEcommerceService.Kafka;

public interface IShipperProfileService
{
    Task<HTTPResponseClient<IEnumerable<ShipperProfileVM>>> GetAllShipperProfiles();
    Task<HTTPResponseClient<ShipperProfileVM>> GetShipperProfileById(int shipperId);
    Task<HTTPResponseClient<ShipperProfileVM>> GetShipperProfileByUserId(int userId);
    Task<HTTPResponseClient<bool>> CreateShipperProfile(ShipperProfileVM shipperProfileVM);
    Task<HTTPResponseClient<bool>> UpdateShipperProfile(ShipperProfileVM shipperProfileVM);
    Task<HTTPResponseClient<bool>> DeleteShipperProfile(int shipperId);
    Task<HTTPResponseClient<bool>> ActivateShipperProfile(int shipperId);
    Task<HTTPResponseClient<bool>> DeactivateShipperProfile(int shipperId);
    Task<HTTPResponseClient<IEnumerable<ShipperProfileVM>>> GetActiveShipperProfiles();
    Task<HTTPResponseClient<bool>> CheckUserHasShipperProfile(int userId);
    Task<HTTPResponseClient<IEnumerable<ShipperProfileVM>>> GetInactiveShipperProfiles();
}

public class ShipperProfileService : IShipperProfileService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly RedisHelper _cacheService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public ShipperProfileService(
        IUnitOfWork unitOfWork,
        RedisHelper cacheService,
        IHubContext<NotificationHub> hubContext)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _hubContext = hubContext;
    }

    public async Task<HTTPResponseClient<IEnumerable<ShipperProfileVM>>> GetAllShipperProfiles()
    {
        var response = new HTTPResponseClient<IEnumerable<ShipperProfileVM>>();
        try
        {
            const string cacheKey = "AllShipperProfiles";

            // Kiểm tra cache trước
            var cachedProfiles = await _cacheService.GetAsync<IEnumerable<ShipperProfileVM>>(cacheKey);
            if (cachedProfiles != null)
            {
                response.Data = cachedProfiles;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy danh sách shipper profile từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            // Nếu không có trong cache, lấy từ database với join User
            var shipperProfiles = await _unitOfWork._shipperProfileRepository.Query()
                .Where(s => s.IsDeleted == false)
                .Include(s => s.User)
                .ToListAsync();

            if (shipperProfiles == null || !shipperProfiles.Any())
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy shipper profile nào";
                return response;
            }

            var shipperProfileVMs = shipperProfiles.Select(s => new ShipperProfileVM
            {
                ShipperId = s.ShipperId,
                UserId = s.UserId,
                FirstName = s.User?.FirstName,
                LastName = s.User?.LastName,
                Username = s.User?.Username,
                Email = s.User?.Email,
                PhoneNumber = s.User?.PhoneNumber,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                IsActive = s.IsActive,
                IsDeleted = s.IsDeleted
            }).ToList();

            // Lưu vào cache
            await _cacheService.SetAsync(cacheKey, shipperProfileVMs, TimeSpan.FromDays(1));

            response.Data = shipperProfileVMs;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy danh sách shipper profile thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy danh sách shipper profile: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<ShipperProfileVM>> GetShipperProfileById(int shipperId)
    {
        var response = new HTTPResponseClient<ShipperProfileVM>();
        try
        {
            string cacheKey = $"ShipperProfile_{shipperId}";
            var cachedProfile = await _cacheService.GetAsync<ShipperProfileVM>(cacheKey);
            if (cachedProfile != null)
            {
                response.Data = cachedProfile;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy thông tin shipper profile từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var shipperProfile = await _unitOfWork._shipperProfileRepository.Query()
                .Where(s => s.ShipperId == shipperId && s.IsDeleted == false)
                .Include(s => s.User)
                .FirstOrDefaultAsync();

            if (shipperProfile == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy shipper profile";
                return response;
            }

            var shipperProfileVM = new ShipperProfileVM
            {
                ShipperId = shipperProfile.ShipperId,
                UserId = shipperProfile.UserId,
                FirstName = shipperProfile.User?.FirstName,
                LastName = shipperProfile.User?.LastName,
                Username = shipperProfile.User?.Username,
                Email = shipperProfile.User?.Email,
                PhoneNumber = shipperProfile.User?.PhoneNumber,
                CreatedAt = shipperProfile.CreatedAt,
                UpdatedAt = shipperProfile.UpdatedAt,
                IsActive = shipperProfile.IsActive,
                IsDeleted = shipperProfile.IsDeleted
            };

            // Lưu vào cache
            await _cacheService.SetAsync(cacheKey, shipperProfileVM, TimeSpan.FromDays(1));

            response.Data = shipperProfileVM;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy thông tin shipper profile thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy thông tin shipper profile: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<ShipperProfileVM>> GetShipperProfileByUserId(int userId)
    {
        var response = new HTTPResponseClient<ShipperProfileVM>();
        try
        {
            string cacheKey = $"ShipperProfileByUser_{userId}";
            var cachedProfile = await _cacheService.GetAsync<ShipperProfileVM>(cacheKey);
            if (cachedProfile != null)
            {
                response.Data = cachedProfile;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy thông tin shipper profile từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var shipperProfile = await _unitOfWork._shipperProfileRepository.Query()
                .Where(s => s.UserId == userId && s.IsDeleted == false)
                .Include(s => s.User)
                .FirstOrDefaultAsync();

            if (shipperProfile == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy shipper profile";
                return response;
            }

            var shipperProfileVM = new ShipperProfileVM
            {
                ShipperId = shipperProfile.ShipperId,
                UserId = shipperProfile.UserId,
                FirstName = shipperProfile.User?.FirstName,
                LastName = shipperProfile.User?.LastName,
                Username = shipperProfile.User?.Username,
                Email = shipperProfile.User?.Email,
                PhoneNumber = shipperProfile.User?.PhoneNumber,
                CreatedAt = shipperProfile.CreatedAt,
                UpdatedAt = shipperProfile.UpdatedAt,
                IsActive = shipperProfile.IsActive,
                IsDeleted = shipperProfile.IsDeleted
            };

            // Lưu vào cache
            await _cacheService.SetAsync(cacheKey, shipperProfileVM, TimeSpan.FromDays(1));

            response.Data = shipperProfileVM;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy thông tin shipper profile thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy thông tin shipper profile: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<bool>> CreateShipperProfile(ShipperProfileVM shipperProfileVM)
    {
        var response = new HTTPResponseClient<bool>();
        try
        {
            // Bắt đầu transaction
            await _unitOfWork.BeginTransaction();

            // Kiểm tra user có tồn tại không
            var user = await _unitOfWork._userRepository.GetByIdAsync(shipperProfileVM.UserId);
            if (user == null || user.IsDeleted == true)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy người dùng";
                return response;
            }

            // Kiểm tra user đã có shipper profile chưa
            var existingProfile = await _unitOfWork._shipperProfileRepository.Query()
                .FirstOrDefaultAsync(s => s.UserId == shipperProfileVM.UserId && s.IsDeleted == false);

            if (existingProfile != null)
            {
                response.Success = false;
                response.StatusCode = 400;
                response.Message = "Người dùng đã có shipper profile";
                return response;
            }

                // Tạo mới shipper profile entity
                var shipperProfile = new ShipperProfile
                {
                    UserId = shipperProfileVM.UserId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsActive = false,
                    IsDeleted = false
                };

                await _unitOfWork._shipperProfileRepository.AddAsync(shipperProfile);


            await _unitOfWork.SaveChangesAsync();

            // Commit transaction khi tất cả thành công
            await _unitOfWork.CommitTransaction();

            // Xóa cache đầy đủ
            await InvalidateAllShipperProfileCaches(shipperProfile?.ShipperId ?? 0, shipperProfile?.UserId ?? 0);
            response.Success = true;
            response.StatusCode = 201;
            response.Message = "Tạo shipper profile thành công";
            response.Data = true;
        }
        catch (Exception ex)
        {
            // Rollback transaction nếu có lỗi
            await _unitOfWork.RollbackTransaction();

            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi tạo shipper profile: {ex.Message}";
        }
        return response;
    }

    public async Task<HTTPResponseClient<bool>> UpdateShipperProfile(ShipperProfileVM shipperProfileVM)
    {
        var response = new HTTPResponseClient<bool>();
        try
        {
            // Bắt đầu transaction
            await _unitOfWork.BeginTransaction();

            var shipperProfile = await _unitOfWork._shipperProfileRepository.GetByIdAsync(shipperProfileVM.ShipperId);
            if (shipperProfile == null || shipperProfile.IsDeleted == true)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy shipper profile";
                return response;
            }

            // Cập nhật thông tin
            shipperProfile.UpdatedAt = DateTime.Now;

            _unitOfWork._shipperProfileRepository.Update(shipperProfile);
            await _unitOfWork.SaveChangesAsync();

            // Commit transaction khi tất cả thành công
            await _unitOfWork.CommitTransaction();

            // Xóa cache đầy đủ
            await InvalidateAllShipperProfileCaches(shipperProfileVM.ShipperId, shipperProfile.UserId);

            // Gửi thông báo realtime qua SignalR
            await _hubContext.Clients.All.SendAsync("ShipperProfileUpdated", shipperProfile.ShipperId);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Cập nhật shipper profile thành công";
            response.Data = true;
        }
        catch (Exception ex)
        {
            // Rollback transaction nếu có lỗi
            await _unitOfWork.RollbackTransaction();

            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi cập nhật shipper profile: {ex.Message}";
        }
        return response;
    }

    public async Task<HTTPResponseClient<bool>> DeleteShipperProfile(int shipperId)
    {
        var response = new HTTPResponseClient<bool>();
        try
        {
            // Bắt đầu transaction
            await _unitOfWork.BeginTransaction();

            var shipperProfile = await _unitOfWork._shipperProfileRepository.GetByIdAsync(shipperId);
            if (shipperProfile == null || shipperProfile.IsDeleted == true)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy shipper profile";
                return response;
            }

            // Đặt trạng thái xóa (soft delete)
            shipperProfile.IsDeleted = true;
            shipperProfile.IsActive = false; // Deactivate khi xóa
            shipperProfile.UpdatedAt = DateTime.Now;
            _unitOfWork._shipperProfileRepository.Update(shipperProfile);
            
            await _unitOfWork.SaveChangesAsync();

            // Commit transaction trước khi gửi Kafka message
            await _unitOfWork.CommitTransaction();
            // Xóa cache đầy đủ
            await InvalidateAllShipperProfileCaches(shipperId, shipperProfile.UserId);

            // Gửi thông báo realtime qua SignalR
            await _hubContext.Clients.All.SendAsync("ShipperProfileDeleted", shipperId);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Xóa shipper profile thành công";
            response.Data = true;
        }
        catch (Exception ex)
        {
            // Rollback transaction nếu có lỗi
            await _unitOfWork.RollbackTransaction();

            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi xóa shipper profile: {ex.Message}";
        }
        return response;
    }

    public async Task<HTTPResponseClient<bool>> ActivateShipperProfile(int shipperId)
    {
        var response = new HTTPResponseClient<bool>();
        try
        {
            await _unitOfWork.BeginTransaction();

            var shipperProfile = await _unitOfWork._shipperProfileRepository.GetByIdAsync(shipperId);
            if (shipperProfile == null || shipperProfile.IsDeleted == true)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy shipper profile";
                return response;
            }

            if (shipperProfile.IsActive == true)
            {
                response.Success = false;
                response.StatusCode = 400;
                response.Message = "Shipper profile đã được kích hoạt trước đó";
                return response;
            }

            shipperProfile.IsActive = true;
            shipperProfile.UpdatedAt = DateTime.Now;
            _unitOfWork._shipperProfileRepository.Update(shipperProfile);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            // Xóa cache đầy đủ
            await InvalidateAllShipperProfileCaches(shipperId, shipperProfile.UserId);

            await _hubContext.Clients.All.SendAsync("ShipperProfileActivated", shipperId);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Kích hoạt shipper profile thành công";
            response.Data = true;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi kích hoạt shipper profile: {ex.Message}";
        }
        return response;
    }

    public async Task<HTTPResponseClient<bool>> DeactivateShipperProfile(int shipperId)
    {
        var response = new HTTPResponseClient<bool>();
        try
        {
            await _unitOfWork.BeginTransaction();

            var shipperProfile = await _unitOfWork._shipperProfileRepository.GetByIdAsync(shipperId);
            if (shipperProfile == null || shipperProfile.IsDeleted == true)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy shipper profile";
                return response;
            }

            shipperProfile.IsActive = false;
            shipperProfile.UpdatedAt = DateTime.Now;
            _unitOfWork._shipperProfileRepository.Update(shipperProfile);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            // Xóa cache đầy đủ
            await InvalidateAllShipperProfileCaches(shipperId, shipperProfile.UserId);

            await _hubContext.Clients.All.SendAsync("ShipperProfileDeactivated", shipperId);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Hủy kích hoạt shipper profile thành công";
            response.Data = true;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi hủy kích hoạt shipper profile: {ex.Message}";
        }
        return response;
    }

    public async Task<HTTPResponseClient<IEnumerable<ShipperProfileVM>>> GetActiveShipperProfiles()
    {
        var response = new HTTPResponseClient<IEnumerable<ShipperProfileVM>>();
        try
        {
            const string cacheKey = "ActiveShipperProfiles";
            var cachedProfiles = await _cacheService.GetAsync<IEnumerable<ShipperProfileVM>>(cacheKey);
            if (cachedProfiles != null)
            {
                response.Data = cachedProfiles;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy danh sách shipper profile đang hoạt động từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var shipperProfiles = await _unitOfWork._shipperProfileRepository.Query()
                .Where(s => s.IsDeleted == false && s.IsActive == true)
                .Include(s => s.User)
                .ToListAsync();

            var shipperProfileVMs = shipperProfiles.Select(s => new ShipperProfileVM
            {
                ShipperId = s.ShipperId,
                UserId = s.UserId,
                FirstName = s.User?.FirstName,
                LastName = s.User?.LastName,
                Username = s.User?.Username,
                Email = s.User?.Email,
                PhoneNumber = s.User?.PhoneNumber,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                IsActive = s.IsActive,
                IsDeleted = s.IsDeleted
            }).ToList();

            await _cacheService.SetAsync(cacheKey, shipperProfileVMs, TimeSpan.FromDays(1));

            response.Data = shipperProfileVMs;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy danh sách shipper profile đang hoạt động thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy danh sách shipper profile đang hoạt động: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<bool>> CheckUserHasShipperProfile(int userId)
    {
        var response = new HTTPResponseClient<bool>();
        try
        {
            var exists = await _unitOfWork._shipperProfileRepository.Query()
                .AnyAsync(s => s.UserId == userId && s.IsDeleted == false);

            response.Data = exists;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = exists ? "Người dùng đã có shipper profile" : "Người dùng chưa có shipper profile";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi kiểm tra shipper profile: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<IEnumerable<ShipperProfileVM>>> GetInactiveShipperProfiles()
    {
        var response = new HTTPResponseClient<IEnumerable<ShipperProfileVM>>();
        try
        {
            const string cacheKey = "InactiveShipperProfiles";
            var cachedProfiles = await _cacheService.GetAsync<IEnumerable<ShipperProfileVM>>(cacheKey);
            if (cachedProfiles != null)
            {
                response.Data = cachedProfiles;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy danh sách shipper profile không hoạt động từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var shipperProfiles = await _unitOfWork._shipperProfileRepository.Query()
                .Where(s => s.IsDeleted == false && s.IsActive == false)
                .Include(s => s.User)
                .ToListAsync();

            var shipperProfileVMs = shipperProfiles.Select(s => new ShipperProfileVM
            {
                ShipperId = s.ShipperId,
                UserId = s.UserId,
                FirstName = s.User?.FirstName,
                LastName = s.User?.LastName,
                Username = s.User?.Username,
                Email = s.User?.Email,
                PhoneNumber = s.User?.PhoneNumber,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                IsActive = s.IsActive,
                IsDeleted = s.IsDeleted
            }).ToList();

            await _cacheService.SetAsync(cacheKey, shipperProfileVMs, TimeSpan.FromDays(1));

            response.Data = shipperProfileVMs;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy danh sách shipper profile không hoạt động thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy danh sách shipper profile không hoạt động: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    private async Task InvalidateAllShipperProfileCaches(int shipperId, int userId)
    {
        var cacheKeys = new[]
        {
            "AllShipperProfiles",
            "ActiveShipperProfiles",
            "InactiveShipperProfiles",
            $"ShipperProfile_{shipperId}",
            $"ShipperProfileByUser_{userId}"
        };

        var tasks = cacheKeys.Select(key => _cacheService.DeleteByPatternAsync(key));
        await Task.WhenAll(tasks);
    }
}