using Microsoft.AspNetCore.SignalR;
using MainEcommerceService.Hubs;
using System.Linq.Expressions;
using MainEcommerceService.Helper;
using MainEcommerceService.Models.dbMainEcommer;
using MainEcommerceService.Models.ViewModel;
using Microsoft.EntityFrameworkCore;

public interface IUserService
{
    Task<HTTPResponseClient<IEnumerable<UserVM>>> GetAllUser();
    Task<HTTPResponseClient<IEnumerable<UserVM>>> GetUserByPage(int pageIndex, int pageSize);
    Task<HTTPResponseClient<IEnumerable<RoleVM>>> GetAllRole();
    Task<HTTPResponseClient<ProfileVM>> GetProfileById(int userId);
    Task<HTTPResponseClient<string>> UpdateProfile(ProfileVM profileVM);
    Task<HTTPResponseClient<string>> UpdateUser(UserListVM userlistVM);
    Task<HTTPResponseClient<string>> DeleteUser(int userId);
}

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly JwtAuthService _jwtAuthService;
    private readonly RedisHelper _cacheService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public UserService(
        IUnitOfWork unitOfWork,
        JwtAuthService jwtAuthService,
        RedisHelper cacheService,
        IHubContext<NotificationHub> hubContext)
    {
        _unitOfWork = unitOfWork;
        _jwtAuthService = jwtAuthService;
        _cacheService = cacheService;
        _hubContext = hubContext;
    }
    public async Task<HTTPResponseClient<IEnumerable<UserVM>>> GetAllUser()
    {
        var response = new HTTPResponseClient<IEnumerable<UserVM>>();
        try
        {
            const string cacheKey = "AllUsers";

            // Kiểm tra cache trước
            var cachedUsers = await _cacheService.GetAsync<IEnumerable<UserVM>>(cacheKey);
            if (cachedUsers != null)
            {
                response.Data = cachedUsers;
                response.Success = true;
                response.StatusCode = 200; // OK
                response.Message = "Lấy danh sách người dùng từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            // Nếu không có trong cache, lấy từ database
            var users = await _unitOfWork._userRepository.GetAllAsync();

            if (users == null || !users.Any())
            {
                response.Success = false;
                response.StatusCode = 404; // Not Found
                response.Message = "Không tìm thấy người dùng nào";
                return response;
            }

            var userIds = users.Select(u => u.UserId).ToList();
            var userRoles = await _unitOfWork._userRoleRepository.Query()
                .Where(ur => userIds.Contains(ur.UserId))
                .Include(ur => ur.Role)
                .ToListAsync();

            var userVMs = new List<UserVM>();
            foreach (var user in users)
            {
                var role = userRoles
                    .Where(ur => ur.UserId == user.UserId)
                    .Select(ur => ur.Role?.RoleName)
                    .FirstOrDefault() ?? "Chưa có vai trò";

                userVMs.Add(new UserVM
                {
                    Id = user.UserId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    UserName = user.Username,
                    Email = user.Email,
                    Role = role,
                    JoinedDate = user.CreatedAt,
                    IsActive = user.IsActive,
                    IsDeleted = user.IsDeleted
                });
            }

            // Lưu vào cache
            await _cacheService.SetAsync(cacheKey, userVMs, TimeSpan.FromDays(1));

            response.Data = userVMs;
            response.Success = true;
            response.StatusCode = 200; // OK
            response.Message = "Lấy danh sách người dùng thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500; // Internal Server Error
            response.Message = $"Lỗi khi lấy danh sách người dùng: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }
    /// <summary>
    /// Lấy danh sách người dùng theo phân trang
    /// </summary>
    public async Task<HTTPResponseClient<IEnumerable<UserVM>>> GetUserByPage(int pageIndex, int pageSize)
    {
        var response = new HTTPResponseClient<IEnumerable<UserVM>>();
        try
        {
            // Kiểm tra cache trước
            const string cacheKey = "PagedUsers";
            var cachedUsers = await _cacheService.GetAsync<IEnumerable<UserVM>>(cacheKey);
            if (cachedUsers != null)
            {
                response.Data = cachedUsers;
                response.Success = true;
                response.StatusCode = 200; // OK
                response.Message = "Lấy danh sách người dùng từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }
            var users = await _unitOfWork._userRepository.GetByPageAsync(pageIndex, pageSize);
            if (users == null || !users.Any())
            {
                response.Success = false;
                response.StatusCode = 404; // Not Found
                response.Message = "Không tìm thấy người dùng nào";
                return response;
            }
            var userIds = users.Select(u => u.UserId).ToList();
            var userRoles = await _unitOfWork._userRoleRepository.Query()
                .Where(ur => userIds.Contains(ur.UserId))
                .Include(ur => ur.Role)
                .ToListAsync();

            var userVMs = new List<UserVM>();
            foreach (var user in users)
            {
                var role = userRoles
                    .Where(ur => ur.UserId == user.UserId)
                    .Select(ur => ur.Role?.RoleName)
                    .FirstOrDefault() ?? "Chưa có vai trò";

                userVMs.Add(new UserVM
                {
                    Id = user.UserId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    UserName = user.Username,
                    Email = user.Email,
                    Role = role,
                    JoinedDate = user.CreatedAt,
                    IsActive = user.IsActive,
                    IsDeleted = user.IsDeleted
                });
            }
            // Lưu vào cache
            await _cacheService.SetAsync(cacheKey, userVMs, TimeSpan.FromDays(1));
            // Trả về danh sách người dùng
            response.Data = userVMs;
            response.Success = true;
            response.StatusCode = 200; // OK
            response.Message = "Lấy danh sách người dùng theo phân trang thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500; // Internal Server Error
            response.Message = $"Lỗi khi lấy danh sách người dùng theo phân trang: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }
    public async Task<HTTPResponseClient<IEnumerable<RoleVM>>> GetAllRole()
    {
        var response = new HTTPResponseClient<IEnumerable<RoleVM>>();
        try
        {
            var roles = await _unitOfWork._roleRepository.GetAllAsync();
            if (roles == null || !roles.Any())
            {
                response.Success = false;
                response.StatusCode = 404; // Not Found
                response.Message = "Không tìm thấy vai trò nào";
                return response;
            }
            var roleVMs = roles.Select(role => new RoleVM
            {
                RoleName = role.RoleName
            }).ToList();

            response.Data = roleVMs;
            response.Success = true;
            response.StatusCode = 200; // OK
            response.Message = "Lấy danh sách vai trò thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500; // Internal Server Error
            response.Message = $"Lỗi khi lấy danh sách vai trò: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }
    /// <summary>
    /// Lấy thông tin người dùng hiện tại chưa bị xóa
    /// </summary>

    public async Task<HTTPResponseClient<string>> UpdateUser(UserListVM userlistVM)
    {
        var response = new HTTPResponseClient<string>();
        try
        {
            // Bắt đầu transaction
            await _unitOfWork.BeginTransaction();

            var user = await _unitOfWork._userRepository.GetByIdAsync(userlistVM.Id);
            if (user == null)
            {
                response.Success = false;
                response.StatusCode = 404; // Not Found
                response.Message = "Không tìm thấy người dùng";
                return response;
            }
            user.FirstName = userlistVM.FirstName;
            user.LastName = userlistVM.LastName;    
            user.Email = userlistVM.Email;
            user.IsActive = userlistVM.IsActive;

            //Lay roleid trong bang role
            var role = await _unitOfWork._roleRepository.Query()
                .FirstOrDefaultAsync(r => r.RoleName == userlistVM.Role);
            if (role == null)
            {
                response.Success = false;
                response.StatusCode = 404; // Not Found
                response.Message = "Không tìm thấy vai trò";
                return response;
            }
            //Kiểm tra nếu trong sellerProfile mà bị xóa thì không cho cập nhật role Seller
            if (role.RoleName == "Seller")
            {
                var sellerProfile = await _unitOfWork._sellerProfileRepository.Query()
                    .FirstOrDefaultAsync(sp => sp.UserId == userlistVM.Id && sp.IsDeleted == true);
                if (sellerProfile != null)
                {
                    response.Success = false;
                    response.StatusCode = 400; // Bad Request
                    response.Message = "Không thể cập nhật vai trò Seller vì người dùng đã bị xóa trong Seller Profile";
                    return response;
                }
            }
            //Kiểm tra nếu trong ShipperProfile mà bị xóa thì không cho cập nhật role Shipper
            if (role.RoleName == "Shipper")
            {
                var shipperProfile = await _unitOfWork._shipperProfileRepository.Query()
                    .FirstOrDefaultAsync(sp => sp.UserId == userlistVM.Id && sp.IsDeleted == true);
                if (shipperProfile != null)
                {
                    response.Success = false;
                    response.StatusCode = 400; // Bad Request
                    response.Message = "Không thể cập nhật vai trò Shipper vì người dùng đã bị xóa trong Shipper Profile";
                    return response;
                }
            }
                //Cap nhat role
                var userRole = await _unitOfWork._userRoleRepository.Query()
                .FirstOrDefaultAsync(ur => ur.UserId == userlistVM.Id);

            if (userRole != null)
            {
                userRole.RoleId = role.RoleId;
            }
            else
            {
                userRole = new UserRole
                {
                    UserId = userlistVM.Id,
                    RoleId = role.RoleId
                };
                await _unitOfWork._userRoleRepository.AddAsync(userRole);
            }

            _unitOfWork._userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();

            // Commit transaction khi tất cả thành công
            await _unitOfWork.CommitTransaction();

            // Xóa cache để đảm bảo dữ liệu mới nhất
            await _cacheService.DeleteByPatternAsync("AllUsers");
            await _cacheService.DeleteByPatternAsync("PagedUsers");

            // Gửi thông báo realtime qua SignalR
            await _hubContext.Clients.All.SendAsync("UserUpdated", userlistVM.Id, userlistVM.LastName);

            // Gửi thông báo trạng thái nếu có thay đổi
            if (user.IsActive == true)
            {
                await _hubContext.Clients.All.SendAsync("UserStatusChanged", userlistVM.Id, "Active");
            }
            else
            {
                await _hubContext.Clients.All.SendAsync("UserStatusChanged", userlistVM.Id, "Inactive");
            }

            response.Success = true;
            response.StatusCode = 200; // OK
            response.Message = "Cập nhật thông tin người dùng thành công";
            response.Data = "Cập nhật thành công";
        }
        catch (Exception ex)
        {
            // Rollback transaction nếu có lỗi
            await _unitOfWork.RollbackTransaction();

            response.Success = false;
            response.StatusCode = 500; // Internal Server Error
            response.Message = $"Lỗi khi cập nhật thông tin người dùng: {ex.Message}";
        }
        return response;
    }
    public async Task<HTTPResponseClient<string>> DeleteUser(int userId)
    {
        var response = new HTTPResponseClient<string>();
        try
        {
            // Bắt đầu transaction
            await _unitOfWork.BeginTransaction();

            var user = await _unitOfWork._userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                response.Success = false;
                response.StatusCode = 404; // Not Found
                response.Message = "Không tìm thấy người dùng";
                return response;
            }

            // Xóa user role trước
            var userRole = await _unitOfWork._userRoleRepository.Query()
                .FirstOrDefaultAsync(ur => ur.UserId == userId);

            if (userRole != null)
            {
                //Đặt trạng thái xóa cho user role
                userRole.IsDeleted = true;
                _unitOfWork._userRoleRepository.Update(userRole);
            }
            //Xóa sellerProfile nếu có
            var sellerProfile = await _unitOfWork._sellerProfileRepository.Query()
                .FirstOrDefaultAsync(sp => sp.UserId == userId);
            if (sellerProfile != null)
            {
                // Đặt trạng thái xóa cho sellerProfile
                sellerProfile.IsDeleted = true;
                _unitOfWork._sellerProfileRepository.Update(sellerProfile);
            }
            // Đặt trạng thái xóa cho user
                user.IsDeleted = true;
            _unitOfWork._userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();

            // Commit transaction khi tất cả thành công
            await _unitOfWork.CommitTransaction();

            // Xóa cache
            await _cacheService.DeleteByPatternAsync("AllUsers");
            await _cacheService.DeleteByPatternAsync("PagedUsers");

            // Gửi thông báo realtime qua SignalR
            await _hubContext.Clients.All.SendAsync("UserDeleted", userId);

            response.Success = true;
            response.StatusCode = 200; // OK
            response.Message = "Xóa người dùng thành công";
            response.Data = "Xóa thành công";
        }
        catch (Exception ex)
        {
            // Rollback transaction nếu có lỗi
            await _unitOfWork.RollbackTransaction();

            response.Success = false;
            response.StatusCode = 500; // Internal Server Error
            response.Message = $"Lỗi khi xóa người dùng: {ex.Message}";
        }
        return response;
    }
    public async Task<HTTPResponseClient<ProfileVM>> GetProfileById(int userId)
    {
        var response = new HTTPResponseClient<ProfileVM>();
        try
        {
            var user = await _unitOfWork._userRepository.Query()
                .FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                response.Success = false;
                response.StatusCode = 404; // Not Found
                response.Message = "Không tìm thấy người dùng";
                return response;
            }
            var profileVM = new ProfileVM
            {
                Id = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.Username,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };
            response.Data = profileVM;
            response.Success = true;
            response.StatusCode = 200; // OK
            response.Message = "Lấy thông tin người dùng thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500; // Internal Server Error
            response.Message = $"Lỗi khi lấy thông tin người dùng: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }
    public async Task<HTTPResponseClient<string>> UpdateProfile(ProfileVM profileVM)
    {
        var response = new HTTPResponseClient<string>();
        try
        {
            // Bắt đầu transaction
            await _unitOfWork.BeginTransaction();

            var user = await _unitOfWork._userRepository.Query()
                .FirstOrDefaultAsync(u => u.Username == profileVM.UserName && u.IsDeleted == false);
            if (user == null)
            {
                response.Success = false;
                response.StatusCode = 404; // Not Found
                response.Message = "Không tìm thấy người dùng";
                return response;
            }

            user.FirstName = profileVM.FirstName;
            user.LastName = profileVM.LastName;
            user.Email = profileVM.Email;
            user.PhoneNumber = profileVM.PhoneNumber;

            _unitOfWork._userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();

            // Commit transaction khi tất cả thành công
            await _unitOfWork.CommitTransaction();

            // Xóa cache để đảm bảo dữ liệu mới nhất
            await _cacheService.DeleteByPatternAsync("AllUsers");
            await _cacheService.DeleteByPatternAsync("PagedUsers");

            // Gửi thông báo realtime qua SignalR
            await _hubContext.Clients.All.SendAsync("ProfileUpdated", user.Username);

            response.Success = true;
            response.StatusCode = 200; // OK
            response.Message = "Cập nhật thông tin người dùng thành công";
            response.Data = "Cập nhật thành công";
        }
        catch (Exception ex)
        {
            // Rollback transaction nếu có lỗi
            await _unitOfWork.RollbackTransaction();

            response.Success = false;
            response.StatusCode = 500; // Internal Server Error
            response.Message = $"Lỗi khi cập nhật thông tin người dùng: {ex.Message}";
        }
        return response;
    }
}