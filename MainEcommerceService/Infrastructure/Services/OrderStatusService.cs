using Microsoft.AspNetCore.SignalR;
using MainEcommerceService.Hubs;
using MainEcommerceService.Helper;
using MainEcommerceService.Models.dbMainEcommer;
using MainEcommerceService.Models.ViewModel;
using Microsoft.EntityFrameworkCore;
using MainEcommerceService.Kafka;

public interface IOrderStatusService
{
    Task<HTTPResponseClient<IEnumerable<OrderStatusVM>>> GetAllOrderStatuses();
    Task<HTTPResponseClient<OrderStatusVM>> GetOrderStatusById(int statusId);
    Task<HTTPResponseClient<bool>> CreateOrderStatus(OrderStatusVM orderStatusVM);
    Task<HTTPResponseClient<bool>> UpdateOrderStatus(OrderStatusVM orderStatusVM);
    Task<HTTPResponseClient<bool>> DeleteOrderStatus(int statusId);
    Task<HTTPResponseClient<OrderStatusVM>> GetOrderStatusByName(string statusName);
}

public class OrderStatusService : IOrderStatusService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly RedisHelper _cacheService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public OrderStatusService(
        IUnitOfWork unitOfWork,
        RedisHelper cacheService,
        IHubContext<NotificationHub> hubContext)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _hubContext = hubContext;
    }

    public async Task<HTTPResponseClient<IEnumerable<OrderStatusVM>>> GetAllOrderStatuses()
    {
        var response = new HTTPResponseClient<IEnumerable<OrderStatusVM>>();
        try
        {
            const string cacheKey = "AllOrderStatuses";
            var cachedStatuses = await _cacheService.GetAsync<IEnumerable<OrderStatusVM>>(cacheKey);
            if (cachedStatuses != null)
            {
                response.Data = cachedStatuses;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy danh sách trạng thái đơn hàng từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var orderStatuses = await _unitOfWork._orderStatusRepository.Query()
                .Where(os => os.IsDeleted == false)
                .ToListAsync();

            if (orderStatuses == null || !orderStatuses.Any())
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy trạng thái đơn hàng nào";
                return response;
            }

            var orderStatusVMs = orderStatuses.Select(os => new OrderStatusVM
            {
                StatusId = os.StatusId,
                StatusName = os.StatusName,
                Description = os.Description,
                CreatedAt = os.CreatedAt,
                UpdatedAt = os.UpdatedAt,
                IsDeleted = os.IsDeleted
            }).ToList();

            await _cacheService.SetAsync(cacheKey, orderStatusVMs, TimeSpan.FromHours(2));

            response.Data = orderStatusVMs;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy danh sách trạng thái đơn hàng thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy danh sách trạng thái đơn hàng: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<OrderStatusVM>> GetOrderStatusById(int statusId)
    {
        var response = new HTTPResponseClient<OrderStatusVM>();
        try
        {
            string cacheKey = $"OrderStatus_{statusId}";
            var cachedStatus = await _cacheService.GetAsync<OrderStatusVM>(cacheKey);
            if (cachedStatus != null)
            {
                response.Data = cachedStatus;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy thông tin trạng thái đơn hàng từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var orderStatus = await _unitOfWork._orderStatusRepository.Query()
                .FirstOrDefaultAsync(os => os.StatusId == statusId && os.IsDeleted == false);

            if (orderStatus == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy trạng thái đơn hàng";
                return response;
            }

            var orderStatusVM = new OrderStatusVM
            {
                StatusId = orderStatus.StatusId,
                StatusName = orderStatus.StatusName,
                Description = orderStatus.Description,
                CreatedAt = orderStatus.CreatedAt,
                UpdatedAt = orderStatus.UpdatedAt,
                IsDeleted = orderStatus.IsDeleted
            };

            await _cacheService.SetAsync(cacheKey, orderStatusVM, TimeSpan.FromHours(2));

            response.Data = orderStatusVM;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy thông tin trạng thái đơn hàng thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy thông tin trạng thái đơn hàng: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<bool>> CreateOrderStatus(OrderStatusVM orderStatusVM)
    {
        var response = new HTTPResponseClient<bool>();
        try
        {
            await _unitOfWork.BeginTransaction();

            // Kiểm tra tên trạng thái đã tồn tại chưa
            var existingStatus = await _unitOfWork._orderStatusRepository.Query()
                .FirstOrDefaultAsync(os => os.StatusName == orderStatusVM.StatusName && os.IsDeleted == false);

            if (existingStatus != null)
            {
                response.Success = false;
                response.StatusCode = 400;
                response.Message = "Tên trạng thái đơn hàng đã tồn tại";
                return response;
            }

            var orderStatus = new OrderStatus
            {
                StatusName = orderStatusVM.StatusName,
                Description = orderStatusVM.Description,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsDeleted = false
            };

            await _unitOfWork._orderStatusRepository.AddAsync(orderStatus);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            await InvalidateAllOrderStatusCaches(orderStatus.StatusId);

            await _hubContext.Clients.All.SendAsync("OrderStatusCreated", orderStatus.StatusId, orderStatus.StatusName);

            response.Success = true;
            response.StatusCode = 201;
            response.Message = "Tạo trạng thái đơn hàng thành công";
            response.Data = true;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi tạo trạng thái đơn hàng: {ex.Message}";
        }
        return response;
    }

    public async Task<HTTPResponseClient<bool>> UpdateOrderStatus(OrderStatusVM orderStatusVM)
    {
        var response = new HTTPResponseClient<bool>();
        try
        {
            await _unitOfWork.BeginTransaction();

            var orderStatus = await _unitOfWork._orderStatusRepository.GetByIdAsync(orderStatusVM.StatusId);
            if (orderStatus == null || orderStatus.IsDeleted == true)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy trạng thái đơn hàng";
                return response;
            }

            // Kiểm tra tên trạng thái có bị trùng không (ngoại trừ chính nó)
            var existingStatus = await _unitOfWork._orderStatusRepository.Query()
                .FirstOrDefaultAsync(os => os.StatusName == orderStatusVM.StatusName &&
                                          os.StatusId != orderStatusVM.StatusId &&
                                          os.IsDeleted == false);

            if (existingStatus != null)
            {
                response.Success = false;
                response.StatusCode = 400;
                response.Message = "Tên trạng thái đơn hàng đã tồn tại";
                return response;
            }

            orderStatus.StatusName = orderStatusVM.StatusName;
            orderStatus.Description = orderStatusVM.Description;
            orderStatus.UpdatedAt = DateTime.Now;

            _unitOfWork._orderStatusRepository.Update(orderStatus);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            await InvalidateAllOrderStatusCaches(orderStatusVM.StatusId);

            await _hubContext.Clients.All.SendAsync("OrderStatusUpdated", orderStatus.StatusId, orderStatus.StatusName);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Cập nhật trạng thái đơn hàng thành công";
            response.Data = true;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi cập nhật trạng thái đơn hàng: {ex.Message}";
        }
        return response;
    }

    public async Task<HTTPResponseClient<bool>> DeleteOrderStatus(int statusId)
    {
        var response = new HTTPResponseClient<bool>();
        try
        {
            await _unitOfWork.BeginTransaction();

            var orderStatus = await _unitOfWork._orderStatusRepository.GetByIdAsync(statusId);
            if (orderStatus == null || orderStatus.IsDeleted == true)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy trạng thái đơn hàng";
                return response;
            }

            // Kiểm tra có đơn hàng nào đang sử dụng trạng thái này không
            var ordersUsingStatus = await _unitOfWork._orderRepository.Query()
                .AnyAsync(o => o.OrderStatusId == statusId && o.IsDeleted == false);

            if (ordersUsingStatus)
            {
                response.Success = false;
                response.StatusCode = 400;
                response.Message = "Không thể xóa trạng thái đơn hàng vì đang được sử dụng";
                return response;
            }

            orderStatus.IsDeleted = true;
            orderStatus.UpdatedAt = DateTime.Now;
            _unitOfWork._orderStatusRepository.Update(orderStatus);
            
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            await InvalidateAllOrderStatusCaches(statusId);

            await _hubContext.Clients.All.SendAsync("OrderStatusDeleted", statusId);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Xóa trạng thái đơn hàng thành công";
            response.Data = true;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi xóa trạng thái đơn hàng: {ex.Message}";
        }
        return response;
    }

    public async Task<HTTPResponseClient<OrderStatusVM>> GetOrderStatusByName(string statusName)
    {
        var response = new HTTPResponseClient<OrderStatusVM>();
        try
        {
            string cacheKey = $"OrderStatusByName_{statusName}";
            var cachedStatus = await _cacheService.GetAsync<OrderStatusVM>(cacheKey);
            if (cachedStatus != null)
            {
                response.Data = cachedStatus;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy thông tin trạng thái đơn hàng theo tên từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var orderStatus = await _unitOfWork._orderStatusRepository.Query()
                .FirstOrDefaultAsync(os => os.StatusName == statusName && os.IsDeleted == false);

            if (orderStatus == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy trạng thái đơn hàng";
                return response;
            }

            var orderStatusVM = new OrderStatusVM
            {
                StatusId = orderStatus.StatusId,
                StatusName = orderStatus.StatusName,
                Description = orderStatus.Description,
                CreatedAt = orderStatus.CreatedAt,
                UpdatedAt = orderStatus.UpdatedAt,
                IsDeleted = orderStatus.IsDeleted
            };

            await _cacheService.SetAsync(cacheKey, orderStatusVM, TimeSpan.FromHours(2));

            response.Data = orderStatusVM;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy thông tin trạng thái đơn hàng theo tên thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy thông tin trạng thái đơn hàng theo tên: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    private async Task InvalidateAllOrderStatusCaches(int statusId)
    {
        var cacheKeys = new[]
        {
            "AllOrderStatuses",
            $"OrderStatus_{statusId}"
        };

        var tasks = cacheKeys.Select(key => _cacheService.DeleteByPatternAsync(key));
        await Task.WhenAll(tasks);
    }
}