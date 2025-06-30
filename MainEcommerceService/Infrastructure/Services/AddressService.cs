using Microsoft.AspNetCore.SignalR;
using MainEcommerceService.Hubs;
using System.Linq.Expressions;
using MainEcommerceService.Helper;
using MainEcommerceService.Models.dbMainEcommer;
using MainEcommerceService.Models.ViewModel;
using Microsoft.EntityFrameworkCore;

public interface IAddressService
{
    Task<HTTPResponseClient<IEnumerable<AddressVM>>> GetAllAddresses();
    Task<HTTPResponseClient<IEnumerable<AddressVM>>> GetAddressesByUserId(int userId);
    Task<HTTPResponseClient<AddressVM>> GetAddressById(int addressId);
    Task<HTTPResponseClient<string>> CreateAddress(AddressVM addressVM);
    Task<HTTPResponseClient<string>> UpdateAddress(AddressVM addressVM);
    Task<HTTPResponseClient<string>> DeleteAddress(int addressId);
    Task<HTTPResponseClient<string>> SetDefaultAddress(int addressId, int userId);
}

public class AddressService : IAddressService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly RedisHelper _cacheService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public AddressService(
        IUnitOfWork unitOfWork,
        RedisHelper cacheService,
        IHubContext<NotificationHub> hubContext)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _hubContext = hubContext;
    }

    public async Task<HTTPResponseClient<IEnumerable<AddressVM>>> GetAllAddresses()
    {
        var response = new HTTPResponseClient<IEnumerable<AddressVM>>();
        try
        {
            const string cacheKey = "AllAddresses";

            // Kiểm tra cache trước
            var cachedAddresses = await _cacheService.GetAsync<IEnumerable<AddressVM>>(cacheKey);
            if (cachedAddresses != null)
            {
                response.Data = cachedAddresses;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy danh sách địa chỉ từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            // Nếu không có trong cache, lấy từ database
            var addresses = await _unitOfWork._addressRepository.Query()
                .Where(a => a.IsDeleted != true)
                .ToListAsync();

            if (addresses == null || !addresses.Any())
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy địa chỉ nào";
                return response;
            }

            var addressVMs = addresses.Select(address => new AddressVM
            {
                AddressId = address.AddressId,
                UserId = address.UserId,
                AddressLine1 = address.AddressLine1,
                AddressLine2 = address.AddressLine2,
                City = address.City,
                State = address.State,
                PostalCode = address.PostalCode,
                Country = address.Country,
                IsDefault = address.IsDefault,
                CreatedAt = address.CreatedAt,
                UpdatedAt = address.UpdatedAt,
                IsDeleted = address.IsDeleted
            }).ToList();

            // Lưu vào cache
            await _cacheService.SetAsync(cacheKey, addressVMs, TimeSpan.FromDays(1));

            response.Data = addressVMs;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy danh sách địa chỉ thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy danh sách địa chỉ: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }
    public async Task<HTTPResponseClient<IEnumerable<AddressVM>>> GetAddressesByUserId(int userId)
    {
        try
        {
            string cacheKey = $"UserAddresses_{userId}";
            
            // Check cache first
            var cachedAddresses = await _cacheService.GetAsync<IEnumerable<AddressVM>>(cacheKey);
            if (cachedAddresses != null)
            {
                return CreateSuccessResponse(cachedAddresses, "Retrieved addresses from cache successfully");
            }

            // Validate user exists
            if (!await UserExistsAsync(userId))
            {
                return CreateErrorResponse<IEnumerable<AddressVM>>(404, "User not found");
            }

            var addresses = await _unitOfWork._addressRepository.Query()
                .Where(a => a.UserId == userId && a.IsDeleted != true)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            var addressVMs = addresses.Select(MapToAddressVM).ToList();

            // Cache the result (even if empty)
            await _cacheService.SetAsync(cacheKey, addressVMs, TimeSpan.FromDays(1));

            var message = addressVMs.Any() ? "Retrieved user addresses successfully" : "User has no addresses yet";
            return CreateSuccessResponse<IEnumerable<AddressVM>>(addressVMs, message);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<IEnumerable<AddressVM>>(500, $"Error retrieving user addresses: {ex.Message}");
        }
    }

    public async Task<HTTPResponseClient<AddressVM>> GetAddressById(int addressId)
    {
        var response = new HTTPResponseClient<AddressVM>();
        try
        {
            string cacheKey = $"Address_{addressId}";
            var cachedAddress = await _cacheService.GetAsync<AddressVM>(cacheKey);
            if (cachedAddress != null)
            {
                response.Data = cachedAddress;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy thông tin địa chỉ từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var address = await _unitOfWork._addressRepository.Query()
                .FirstOrDefaultAsync(a => a.AddressId == addressId && a.IsDeleted != true);

            if (address == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy địa chỉ";
                return response;
            }

            var addressVM = new AddressVM
            {
                AddressId = address.AddressId,
                UserId = address.UserId,
                AddressLine1 = address.AddressLine1,
                AddressLine2 = address.AddressLine2,
                City = address.City,
                State = address.State,
                PostalCode = address.PostalCode,
                Country = address.Country,
                IsDefault = address.IsDefault,
                CreatedAt = address.CreatedAt,
                UpdatedAt = address.UpdatedAt,
                IsDeleted = address.IsDeleted
            };

            // Lưu vào cache
            await _cacheService.SetAsync(cacheKey, addressVM, TimeSpan.FromDays(1));

            response.Data = addressVM;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy thông tin địa chỉ thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy thông tin địa chỉ: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<string>> CreateAddress(AddressVM addressVM)
    {
        try
        {
            await _unitOfWork.BeginTransaction();

            if (!await UserExistsAsync(addressVM.UserId))
            {
                return CreateErrorResponse<string>(404, "User not found");
            }

            var existingAddresses = await GetUserAddressesAsync(addressVM.UserId);
            
            // Auto-set as default if first address
            if (!existingAddresses.Any())
            {
                addressVM.IsDefault = true;
            }
            
            // Update other addresses if this is set as default
            if (addressVM.IsDefault == true)
            {
                await UpdateDefaultAddresses(existingAddresses, false);
            }

            var newAddress = MapToAddress(addressVM);
            newAddress.CreatedAt = DateTime.Now;
            newAddress.IsDeleted = false;

            await _unitOfWork._addressRepository.AddAsync(newAddress);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            await ClearAddressCache(addressVM.UserId);
            await _hubContext.Clients.All.SendAsync("AddressCreated", newAddress.AddressId, addressVM.UserId);

            var message = existingAddresses.Any() ? "Address created successfully" : "First address created and set as default successfully";
            return CreateSuccessResponse("Address created successfully", message, 201);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            return CreateErrorResponse<string>(500, $"Error creating address: {ex.Message}");
        }
    }

    public async Task<HTTPResponseClient<string>> UpdateAddress(AddressVM addressVM)
    {
        var response = new HTTPResponseClient<string>();
        try
        {
            // Bắt đầu transaction
            await _unitOfWork.BeginTransaction();

            var address = await _unitOfWork._addressRepository.Query()
                .FirstOrDefaultAsync(a => a.AddressId == addressVM.AddressId && a.IsDeleted != true);

            if (address == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy địa chỉ";
                return response;
            }

            // Nếu đây là địa chỉ mặc định, cập nhật các địa chỉ khác của user
            if (addressVM.IsDefault == true && address.IsDefault != true)
            {
                var existingAddresses = await _unitOfWork._addressRepository.Query()
                    .Where(a => a.UserId == address.UserId && a.AddressId != addressVM.AddressId && a.IsDeleted != true)
                    .ToListAsync();

                foreach (var addr in existingAddresses)
                {
                    addr.IsDefault = false;
                    _unitOfWork._addressRepository.Update(addr);
                }
            }

            address.AddressLine1 = addressVM.AddressLine1;
            address.AddressLine2 = addressVM.AddressLine2;
            address.City = addressVM.City;
            address.State = addressVM.State;
            address.PostalCode = addressVM.PostalCode;
            address.Country = addressVM.Country;
            address.IsDefault = addressVM.IsDefault;
            address.UpdatedAt = DateTime.Now;

            _unitOfWork._addressRepository.Update(address);
            await _unitOfWork.SaveChangesAsync();

            // Commit transaction khi tất cả thành công
            await _unitOfWork.CommitTransaction();

            // Xóa cache
            await ClearAddressCache(address.UserId);

            // Gửi thông báo realtime qua SignalR
            await _hubContext.Clients.All.SendAsync("AddressUpdated", addressVM.AddressId, address.UserId);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Cập nhật địa chỉ thành công";
            response.Data = "Cập nhật thành công";
        }
        catch (Exception ex)
        {
            // Rollback transaction nếu có lỗi
            await _unitOfWork.RollbackTransaction();

            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi cập nhật địa chỉ: {ex.Message}";
        }
        return response;
    }

    public async Task<HTTPResponseClient<string>> DeleteAddress(int addressId)
    {
        var response = new HTTPResponseClient<string>();
        try
        {
            // Bắt đầu transaction
            await _unitOfWork.BeginTransaction();

            var address = await _unitOfWork._addressRepository.Query()
                .FirstOrDefaultAsync(a => a.AddressId == addressId && a.IsDeleted != true);

            if (address == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy địa chỉ";
                return response;
            }

            // Đặt trạng thái xóa cho địa chỉ
            address.IsDeleted = true;
            address.UpdatedAt = DateTime.Now;
            _unitOfWork._addressRepository.Update(address);

            await _unitOfWork.SaveChangesAsync();

            // Commit transaction khi tất cả thành công
            await _unitOfWork.CommitTransaction();

            // Xóa cache
            await ClearAddressCache(address.UserId);

            // Gửi thông báo realtime qua SignalR
            await _hubContext.Clients.All.SendAsync("AddressDeleted", addressId, address.UserId);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Xóa địa chỉ thành công";
            response.Data = "Xóa thành công";
        }
        catch (Exception ex)
        {
            // Rollback transaction nếu có lỗi
            await _unitOfWork.RollbackTransaction();

            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi xóa địa chỉ: {ex.Message}";
        }
        return response;
    }

    public async Task<HTTPResponseClient<string>> SetDefaultAddress(int addressId, int userId)
    {
        var response = new HTTPResponseClient<string>();
        try
        {
            // Bắt đầu transaction
            await _unitOfWork.BeginTransaction();

            var address = await _unitOfWork._addressRepository.Query()
                .FirstOrDefaultAsync(a => a.AddressId == addressId && a.UserId == userId && a.IsDeleted != true);

            if (address == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy địa chỉ";
                return response;
            }

            // Cập nhật tất cả địa chỉ của user về không mặc định
            var allUserAddresses = await _unitOfWork._addressRepository.Query()
                .Where(a => a.UserId == userId && a.IsDeleted != true)
                .ToListAsync();

            foreach (var addr in allUserAddresses)
            {
                addr.IsDefault = addr.AddressId == addressId;
                addr.UpdatedAt = DateTime.Now;
                _unitOfWork._addressRepository.Update(addr);
            }

            await _unitOfWork.SaveChangesAsync();

            // Commit transaction khi tất cả thành công
            await _unitOfWork.CommitTransaction();

            // Xóa cache
            await ClearAddressCache(userId);

            // Gửi thông báo realtime qua SignalR
            await _hubContext.Clients.All.SendAsync("DefaultAddressChanged", addressId, userId);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Đặt địa chỉ mặc định thành công";
            response.Data = "Cập nhật thành công";
        }
        catch (Exception ex)
        {
            // Rollback transaction nếu có lỗi
            await _unitOfWork.RollbackTransaction();

            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi đặt địa chỉ mặc định: {ex.Message}";
        }
        return response;
    }

    private async Task ClearAddressCache(int userId)
    {
        await _cacheService.DeleteByPatternAsync("AllAddresses");
        await _cacheService.DeleteByPatternAsync($"UserAddresses_{userId}");
        
        // Xóa cache phân trang (có thể có nhiều trang)
        for (int i = 1; i <= 10; i++)
        {
            await _cacheService.DeleteByPatternAsync($"PagedAddresses_{i}_10");
            await _cacheService.DeleteByPatternAsync($"PagedAddresses_{i}_20");
            await _cacheService.DeleteByPatternAsync($"PagedAddresses_{i}_50");
        }
    }

    // Helper methods
    private async Task<bool> UserExistsAsync(int userId)
    {
        return await _unitOfWork._userRepository.Query()
            .AnyAsync(u => u.UserId == userId && u.IsDeleted != true);
    }

    private async Task<List<Address>> GetUserAddressesAsync(int userId)
    {
        return await _unitOfWork._addressRepository.Query()
            .Where(a => a.UserId == userId && a.IsDeleted != true)
            .ToListAsync();
    }

    private async Task UpdateDefaultAddresses(List<Address> addresses, bool isDefault)
    {
        foreach (var addr in addresses)
        {
            addr.IsDefault = isDefault;
            _unitOfWork._addressRepository.Update(addr);
        }
    }

    private static AddressVM MapToAddressVM(Address address)
    {
        return new AddressVM
        {
            AddressId = address.AddressId,
            UserId = address.UserId,
            AddressLine1 = address.AddressLine1,
            AddressLine2 = address.AddressLine2,
            City = address.City,
            State = address.State,
            PostalCode = address.PostalCode,
            Country = address.Country,
            IsDefault = address.IsDefault,
            CreatedAt = address.CreatedAt,
            UpdatedAt = address.UpdatedAt,
            IsDeleted = address.IsDeleted
        };
    }

    private static Address MapToAddress(AddressVM addressVM)
    {
        return new Address
        {
            AddressId = addressVM.AddressId,
            UserId = addressVM.UserId,
            AddressLine1 = addressVM.AddressLine1?.Trim(),
            AddressLine2 = addressVM.AddressLine2?.Trim(),
            City = addressVM.City?.Trim(),
            State = addressVM.State?.Trim(),
            PostalCode = addressVM.PostalCode?.Trim(),
            Country = addressVM.Country?.Trim(),
            IsDefault = addressVM.IsDefault
        };
    }

    private static HTTPResponseClient<T> CreateSuccessResponse<T>(T data, string message, int statusCode = 200)
    {
        return new HTTPResponseClient<T>
        {
            Data = data,
            Success = true,
            StatusCode = statusCode,
            Message = message,
            DateTime = DateTime.Now
        };
    }

    private static HTTPResponseClient<T> CreateErrorResponse<T>(int statusCode, string message)
    {
        return new HTTPResponseClient<T>
        {
            Success = false,
            StatusCode = statusCode,
            Message = message,
            DateTime = DateTime.Now
        };
    }
}
