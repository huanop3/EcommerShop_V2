using Microsoft.AspNetCore.SignalR;
using MainEcommerceService.Hubs;
using MainEcommerceService.Helper;
using MainEcommerceService.Models.dbMainEcommer;
using MainEcommerceService.Models.ViewModel;
using Microsoft.EntityFrameworkCore;
using MainEcommerceService.Kafka;

public interface IOrderItemService
{
    Task<HTTPResponseClient<IEnumerable<OrderItemVM>>> GetAllOrderItems();
    Task<HTTPResponseClient<OrderItemVM>> GetOrderItemById(int orderItemId);
    Task<HTTPResponseClient<IEnumerable<OrderItemVM>>> GetOrderItemsByOrderId(int orderId);
    Task<HTTPResponseClient<IEnumerable<OrderItemVM>>> GetOrderItemsByProductId(int productId);
    Task<HTTPResponseClient<bool>> CreateOrderItem(OrderItemVM orderItemVM);
    Task<HTTPResponseClient<bool>> UpdateOrderItem(OrderItemVM orderItemVM);
    Task<HTTPResponseClient<bool>> DeleteOrderItem(int orderItemId);
    Task<HTTPResponseClient<decimal>> GetTotalAmountByOrderId(int orderId);
}

public class OrderItemService : IOrderItemService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly RedisHelper _cacheService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public OrderItemService(
        IUnitOfWork unitOfWork,
        RedisHelper cacheService,
        IHubContext<NotificationHub> hubContext
    )
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _hubContext = hubContext;
    }

    public async Task<HTTPResponseClient<IEnumerable<OrderItemVM>>> GetAllOrderItems()
    {
        var response = new HTTPResponseClient<IEnumerable<OrderItemVM>>();
        try
        {
            const string cacheKey = "AllOrderItems";
            var cachedOrderItems = await _cacheService.GetAsync<IEnumerable<OrderItemVM>>(cacheKey);
            if (cachedOrderItems != null)
            {
                response.Data = cachedOrderItems;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy danh sách chi tiết đơn hàng từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var orderItems = await _unitOfWork._orderItemRepository.Query()
                .Where(oi => oi.IsDeleted == false)
                .ToListAsync();

            if (orderItems == null || !orderItems.Any())
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy chi tiết đơn hàng nào";
                return response;
            }

            var orderItemVMs = orderItems.Select(oi => new OrderItemVM
            {
                OrderItemId = oi.OrderItemId,
                OrderId = oi.OrderId,
                ProductId = oi.ProductId,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                TotalPrice = oi.TotalPrice,
                CreatedAt = oi.CreatedAt,
                UpdatedAt = oi.UpdatedAt,
                IsDeleted = oi.IsDeleted
            }).ToList();

            await _cacheService.SetAsync(cacheKey, orderItemVMs, TimeSpan.FromDays(1));

            response.Data = orderItemVMs;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy danh sách chi tiết đơn hàng thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy danh sách chi tiết đơn hàng: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<OrderItemVM>> GetOrderItemById(int orderItemId)
    {
        var response = new HTTPResponseClient<OrderItemVM>();
        try
        {
            string cacheKey = $"OrderItem_{orderItemId}";
            var cachedOrderItem = await _cacheService.GetAsync<OrderItemVM>(cacheKey);
            if (cachedOrderItem != null)
            {
                response.Data = cachedOrderItem;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy thông tin chi tiết đơn hàng từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var orderItem = await _unitOfWork._orderItemRepository.Query()
                .FirstOrDefaultAsync(oi => oi.OrderItemId == orderItemId && oi.IsDeleted == false);

            if (orderItem == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy chi tiết đơn hàng";
                return response;
            }

            var orderItemVM = new OrderItemVM
            {
                OrderItemId = orderItem.OrderItemId,
                OrderId = orderItem.OrderId,
                ProductId = orderItem.ProductId,
                Quantity = orderItem.Quantity,
                UnitPrice = orderItem.UnitPrice,
                TotalPrice = orderItem.TotalPrice,
                CreatedAt = orderItem.CreatedAt,
                UpdatedAt = orderItem.UpdatedAt,
                IsDeleted = orderItem.IsDeleted
            };

            await _cacheService.SetAsync(cacheKey, orderItemVM, TimeSpan.FromDays(1));

            response.Data = orderItemVM;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy thông tin chi tiết đơn hàng thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy thông tin chi tiết đơn hàng: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<IEnumerable<OrderItemVM>>> GetOrderItemsByOrderId(int orderId)
    {
        var response = new HTTPResponseClient<IEnumerable<OrderItemVM>>();
        try
        {
            string cacheKey = $"OrderItemsByOrder_{orderId}";
            var cachedOrderItems = await _cacheService.GetAsync<IEnumerable<OrderItemVM>>(cacheKey);
            if (cachedOrderItems != null)
            {
                response.Data = cachedOrderItems;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy danh sách chi tiết đơn hàng theo đơn hàng từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var orderItems = await _unitOfWork._orderItemRepository.Query()
                .Where(oi => oi.OrderId == orderId && oi.IsDeleted == false)
                .ToListAsync();

            var orderItemVMs = orderItems.Select(oi => new OrderItemVM
            {
                OrderItemId = oi.OrderItemId,
                OrderId = oi.OrderId,
                ProductId = oi.ProductId,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                TotalPrice = oi.TotalPrice,
                CreatedAt = oi.CreatedAt,
                UpdatedAt = oi.UpdatedAt,
                IsDeleted = oi.IsDeleted
            }).ToList();

            await _cacheService.SetAsync(cacheKey, orderItemVMs, TimeSpan.FromDays(1));

            response.Data = orderItemVMs;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy danh sách chi tiết đơn hàng theo đơn hàng thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy danh sách chi tiết đơn hàng theo đơn hàng: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<IEnumerable<OrderItemVM>>> GetOrderItemsByProductId(int productId)
    {
        var response = new HTTPResponseClient<IEnumerable<OrderItemVM>>();
        try
        {
            string cacheKey = $"OrderItemsByProduct_{productId}";
            var cachedOrderItems = await _cacheService.GetAsync<IEnumerable<OrderItemVM>>(cacheKey);
            if (cachedOrderItems != null)
            {
                response.Data = cachedOrderItems;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy danh sách chi tiết đơn hàng theo sản phẩm từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var orderItems = await _unitOfWork._orderItemRepository.Query()
                .Where(oi => oi.ProductId == productId && oi.IsDeleted == false)
                .ToListAsync();

            var orderItemVMs = orderItems.Select(oi => new OrderItemVM
            {
                OrderItemId = oi.OrderItemId,
                OrderId = oi.OrderId,
                ProductId = oi.ProductId,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                TotalPrice = oi.TotalPrice,
                CreatedAt = oi.CreatedAt,
                UpdatedAt = oi.UpdatedAt,
                IsDeleted = oi.IsDeleted
            }).ToList();

            await _cacheService.SetAsync(cacheKey, orderItemVMs, TimeSpan.FromDays(1));

            response.Data = orderItemVMs;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy danh sách chi tiết đơn hàng theo sản phẩm thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy danh sách chi tiết đơn hàng theo sản phẩm: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<bool>> CreateOrderItem(OrderItemVM orderItemVM)
    {
        var response = new HTTPResponseClient<bool>();
        try
        {
            await _unitOfWork.BeginTransaction();

            // Kiểm tra đơn hàng có tồn tại không
            var order = await _unitOfWork._orderRepository.GetByIdAsync(orderItemVM.OrderId);
            if (order == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy đơn hàng";
                return response;
            }
            var orderItem = new OrderItem
            {
                OrderId = orderItemVM.OrderId,
                ProductId = orderItemVM.ProductId,
                Quantity = orderItemVM.Quantity,
                UnitPrice = orderItemVM.UnitPrice,
                TotalPrice = orderItemVM.Quantity * orderItemVM.UnitPrice,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsDeleted = false
            };

            await _unitOfWork._orderItemRepository.AddAsync(orderItem);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            await InvalidateAllOrderItemCaches(orderItem.OrderItemId, orderItemVM.OrderId, orderItemVM.ProductId);

            await _hubContext.Clients.All.SendAsync("OrderItemCreated", orderItem.OrderItemId, orderItem.OrderId);

            response.Success = true;
            response.StatusCode = 201;
            response.Message = "Tạo chi tiết đơn hàng thành công";
            response.Data = true;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi tạo chi tiết đơn hàng: {ex.Message}";
        }
        return response;
    }

    public async Task<HTTPResponseClient<bool>> UpdateOrderItem(OrderItemVM orderItemVM)
    {
        var response = new HTTPResponseClient<bool>();
        try
        {
            await _unitOfWork.BeginTransaction();

            var orderItem = await _unitOfWork._orderItemRepository.GetByIdAsync(orderItemVM.OrderItemId);
            if (orderItem == null || orderItem.IsDeleted == true)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy chi tiết đơn hàng";
                return response;
            }
            orderItem.ProductId = orderItemVM.ProductId;
            orderItem.Quantity = orderItemVM.Quantity;
            orderItem.UnitPrice = orderItemVM.UnitPrice;
            orderItem.TotalPrice = orderItemVM.Quantity * orderItemVM.UnitPrice;
            orderItem.UpdatedAt = DateTime.Now;

            _unitOfWork._orderItemRepository.Update(orderItem);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            await InvalidateAllOrderItemCaches(orderItemVM.OrderItemId, orderItem.OrderId, orderItemVM.ProductId);

            await _hubContext.Clients.All.SendAsync("OrderItemUpdated", orderItem.OrderItemId, orderItem.OrderId);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Cập nhật chi tiết đơn hàng thành công";
            response.Data = true;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi cập nhật chi tiết đơn hàng: {ex.Message}";
        }
        return response;
    }

    public async Task<HTTPResponseClient<bool>> DeleteOrderItem(int orderItemId)
    {
        var response = new HTTPResponseClient<bool>();
        try
        {
            await _unitOfWork.BeginTransaction();

            var orderItem = await _unitOfWork._orderItemRepository.GetByIdAsync(orderItemId);
            if (orderItem == null || orderItem.IsDeleted == true)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy chi tiết đơn hàng";
                return response;
            }

            orderItem.IsDeleted = true;
            orderItem.UpdatedAt = DateTime.Now;
            _unitOfWork._orderItemRepository.Update(orderItem);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            await InvalidateAllOrderItemCaches(orderItemId, orderItem.OrderId, orderItem.ProductId);

            await _hubContext.Clients.All.SendAsync("OrderItemDeleted", orderItemId);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Xóa chi tiết đơn hàng thành công";
            response.Data = true;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi xóa chi tiết đơn hàng: {ex.Message}";
        }
        return response;
    }

    public async Task<HTTPResponseClient<decimal>> GetTotalAmountByOrderId(int orderId)
    {
        var response = new HTTPResponseClient<decimal>();
        try
        {
            string cacheKey = $"OrderItemsTotal_{orderId}";
            var cachedTotal = await _cacheService.GetAsync<decimal?>(cacheKey);
            if (cachedTotal.HasValue)
            {
                response.Data = cachedTotal.Value;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy tổng tiền đơn hàng từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var totalAmount = await _unitOfWork._orderItemRepository.Query()
                .Where(oi => oi.OrderId == orderId && oi.IsDeleted == false)
                .SumAsync(oi => oi.TotalPrice);

            await _cacheService.SetAsync(cacheKey, totalAmount, TimeSpan.FromDays(1));

            response.Data = totalAmount;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Tính tổng tiền đơn hàng thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi tính tổng tiền đơn hàng: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    private async Task InvalidateAllOrderItemCaches(int orderItemId, int orderId, int productId)
    {
        var cacheKeys = new[]
        {
            "AllOrderItems",
            $"OrderItem_{orderItemId}",
            $"OrderItemsByOrder_{orderId}",
            $"OrderItemsByProduct_{productId}",
            $"OrderItemsTotal_{orderId}"
        };

        var tasks = cacheKeys.Select(key => _cacheService.DeleteByPatternAsync(key));
        await Task.WhenAll(tasks);
    }
}