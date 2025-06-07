using Microsoft.AspNetCore.SignalR;
using MainEcommerceService.Hubs;
using MainEcommerceService.Helper;
using MainEcommerceService.Models.dbMainEcommer;
using MainEcommerceService.Models.ViewModel;
using Microsoft.EntityFrameworkCore;
using MainEcommerceService.Kafka;
using MainEcommerceService.Models.Kafka;

public interface IOrderService
{
    Task<HTTPResponseClient<IEnumerable<OrderVM>>> GetAllOrders();
    Task<HTTPResponseClient<OrderVM>> GetOrderById(int orderId);
    Task<HTTPResponseClient<IEnumerable<OrderVM>>> GetOrdersByUserId(int userId);
    Task<HTTPResponseClient<string>> CreateOrder(OrderVM orderVM);
    Task<HTTPResponseClient<string>> UpdateOrder(OrderVM orderVM);
    Task<HTTPResponseClient<string>> DeleteOrder(int orderId);
    Task<HTTPResponseClient<string>> UpdateOrderStatus(int orderId, int statusId);
    Task<HTTPResponseClient<IEnumerable<OrderVM>>> GetOrdersByStatus(int statusId);
    Task<HTTPResponseClient<IEnumerable<OrderVM>>> GetOrdersByDateRange(DateTime startDate, DateTime endDate);
    Task<HTTPResponseClient<string>> CreateOrderWithItems(OrderVM orderVM, List<OrderItemVM> orderItems);
    Task<HTTPResponseClient<string>> UpdateOrderStatusByName(int orderId, string statusName);
}

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly RedisHelper _cacheService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IKafkaProducerService _kafkaProducer;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IUnitOfWork unitOfWork,
        RedisHelper cacheService,
        IHubContext<NotificationHub> hubContext,
        IKafkaProducerService kafkaProducer,
        ILogger<OrderService> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _hubContext = hubContext;
        _kafkaProducer = kafkaProducer;
        _logger = logger;
    }

    public async Task<HTTPResponseClient<IEnumerable<OrderVM>>> GetAllOrders()
    {
        var response = new HTTPResponseClient<IEnumerable<OrderVM>>();
        try
        {
            const string cacheKey = "AllOrders";
            var cachedOrders = await _cacheService.GetAsync<IEnumerable<OrderVM>>(cacheKey);
            if (cachedOrders != null)
            {
                response.Data = cachedOrders;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy danh sách đơn hàng từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var orders = await _unitOfWork._orderRepository.Query()
                .Where(o => o.IsDeleted == false)
                .Include(o => o.OrderStatus)
                .Include(o => o.User)
                .ToListAsync();

            if (orders == null || !orders.Any())
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy đơn hàng nào";
                response.DateTime = DateTime.Now;
                return response;
            }

            var orderVMs = orders.Select(o => new OrderVM
            {
                OrderId = o.OrderId,
                UserId = o.UserId,
                OrderStatusId = o.OrderStatusId,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                ShippingAddressId = o.ShippingAddressId,
                CouponId = o.CouponId,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt,
                IsDeleted = o.IsDeleted
            }).ToList();

            await _cacheService.SetAsync(cacheKey, orderVMs, TimeSpan.FromMinutes(30));

            response.Data = orderVMs;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy danh sách đơn hàng thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy danh sách đơn hàng: {ex.Message}";
            response.DateTime = DateTime.Now;
            _logger.LogError(ex, "Error getting all orders");
        }
        return response;
    }

    public async Task<HTTPResponseClient<OrderVM>> GetOrderById(int orderId)
    {
        var response = new HTTPResponseClient<OrderVM>();
        try
        {
            string cacheKey = $"Order_{orderId}";
            var cachedOrder = await _cacheService.GetAsync<OrderVM>(cacheKey);
            if (cachedOrder != null)
            {
                response.Data = cachedOrder;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy thông tin đơn hàng từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var order = await _unitOfWork._orderRepository.Query()
                .Include(o => o.OrderStatus)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.IsDeleted == false);

            if (order == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy đơn hàng";
                response.DateTime = DateTime.Now;
                return response;
            }

            var orderVM = new OrderVM
            {
                OrderId = order.OrderId,
                UserId = order.UserId,
                OrderStatusId = order.OrderStatusId,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                ShippingAddressId = order.ShippingAddressId,
                CouponId = order.CouponId,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                IsDeleted = order.IsDeleted
            };

            await _cacheService.SetAsync(cacheKey, orderVM, TimeSpan.FromMinutes(30));

            response.Data = orderVM;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy thông tin đơn hàng thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy thông tin đơn hàng: {ex.Message}";
            response.DateTime = DateTime.Now;
            _logger.LogError(ex, "Error getting order by id {OrderId}", orderId);
        }
        return response;
    }

    public async Task<HTTPResponseClient<IEnumerable<OrderVM>>> GetOrdersByUserId(int userId)
    {
        var response = new HTTPResponseClient<IEnumerable<OrderVM>>();
        try
        {
            string cacheKey = $"OrdersByUser_{userId}";
            var cachedOrders = await _cacheService.GetAsync<IEnumerable<OrderVM>>(cacheKey);
            if (cachedOrders != null)
            {
                response.Data = cachedOrders;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy danh sách đơn hàng theo người dùng từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var orders = await _unitOfWork._orderRepository.Query()
                .Where(o => o.UserId == userId && o.IsDeleted == false)
                .Include(o => o.OrderStatus)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var orderVMs = orders.Select(o => new OrderVM
            {
                OrderId = o.OrderId,
                UserId = o.UserId,
                OrderStatusId = o.OrderStatusId,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                ShippingAddressId = o.ShippingAddressId,
                CouponId = o.CouponId,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt,
                IsDeleted = o.IsDeleted
            }).ToList();

            await _cacheService.SetAsync(cacheKey, orderVMs, TimeSpan.FromMinutes(30));

            response.Data = orderVMs;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy danh sách đơn hàng theo người dùng thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy danh sách đơn hàng theo người dùng: {ex.Message}";
            response.DateTime = DateTime.Now;
            _logger.LogError(ex, "Error getting orders by user id {UserId}", userId);
        }
        return response;
    }

    public async Task<HTTPResponseClient<string>> CreateOrder(OrderVM orderVM)
    {
        var response = new HTTPResponseClient<string>();
        try
        {
            await _unitOfWork.BeginTransaction();

            // Kiểm tra user có tồn tại không
            var user = await _unitOfWork._userRepository.GetByIdAsync(orderVM.UserId);
            if (user == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy người dùng";
                response.Data = "USER_NOT_FOUND";
                response.DateTime = DateTime.Now;
                return response;
            }

            // Tạo đơn hàng với trạng thái Pending
            var pendingStatus = await _unitOfWork._orderStatusRepository.Query()
                .FirstOrDefaultAsync(os => os.StatusName == "Pending" && os.IsDeleted == false);
            
            if (pendingStatus == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy trạng thái Pending";
                response.Data = "PENDING_STATUS_NOT_FOUND";
                response.DateTime = DateTime.Now;
                return response;
            }

            var order = new Order
            {
                UserId = orderVM.UserId,
                OrderStatusId = pendingStatus.StatusId,
                OrderDate = DateTime.Now,
                TotalAmount = orderVM.TotalAmount,
                ShippingAddressId = orderVM.ShippingAddressId,
                CouponId = orderVM.CouponId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsDeleted = false
            };

            await _unitOfWork._orderRepository.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            // Gửi message tới Kafka để xử lý trừ sản phẩm
            try
            {
                var orderCreatedMessage = new OrderCreatedMessage
                {
                    RequestId = Guid.NewGuid().ToString(),
                    OrderId = order.OrderId,
                    UserId = order.UserId,
                    OrderItems = orderVM.OrderItems?.Select(oi => new OrderItemData
                    {
                        ProductId = oi.ProductId,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice
                    }).ToList() ?? new List<OrderItemData>(),
                    CreatedAt = order.CreatedAt.Value
                };
                await _kafkaProducer.SendMessageAsync(
                    "order-created",
                    order.OrderId.ToString(),
                    orderCreatedMessage);
                _logger.LogInformation("📤 Sent order created message to Kafka for order {OrderId}", order.OrderId);
            }
            catch (Exception kafkaEx)
            {
                _logger.LogError(kafkaEx, "❌ Failed to send order created message to Kafka for order {OrderId}", order.OrderId);
                // Không rollback vì đã commit, để consumer xử lý retry
            }

            await InvalidateAllOrderCaches(order.OrderId, orderVM.UserId, pendingStatus.StatusId);
            await _hubContext.Clients.All.SendAsync("OrderCreated", order.OrderId, order.UserId, order.TotalAmount);

            response.Success = true;
            response.StatusCode = 201;
            response.Message = "Tạo đơn hàng thành công, đang xử lý";
            response.Data = $"ORDER_CREATED_SUCCESS_{order.OrderId}";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi tạo đơn hàng: {ex.Message}";
            response.Data = "ORDER_CREATION_FAILED";
            response.DateTime = DateTime.Now;
            _logger.LogError(ex, "❌ Error creating order for user {UserId}", orderVM.UserId);
        }
        return response;
    }

    public async Task<HTTPResponseClient<string>> UpdateOrder(OrderVM orderVM)
    {
        var response = new HTTPResponseClient<string>();
        try
        {
            await _unitOfWork.BeginTransaction();

            var order = await _unitOfWork._orderRepository.GetByIdAsync(orderVM.OrderId);
            if (order == null || order.IsDeleted == true)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy đơn hàng";
                response.Data = "ORDER_NOT_FOUND";
                response.DateTime = DateTime.Now;
                return response;
            }

            // Kiểm tra trạng thái đơn hàng có tồn tại không
            var orderStatus = await _unitOfWork._orderStatusRepository.GetByIdAsync(orderVM.OrderStatusId);
            if (orderStatus == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy trạng thái đơn hàng";
                response.Data = "ORDER_STATUS_NOT_FOUND";
                response.DateTime = DateTime.Now;
                return response;
            }

            order.OrderStatusId = orderVM.OrderStatusId;
            order.TotalAmount = orderVM.TotalAmount;
            order.ShippingAddressId = orderVM.ShippingAddressId;
            order.CouponId = orderVM.CouponId;
            order.UpdatedAt = DateTime.Now;

            _unitOfWork._orderRepository.Update(order);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            await InvalidateAllOrderCaches(orderVM.OrderId, order.UserId, orderVM.OrderStatusId);
            await _hubContext.Clients.All.SendAsync("OrderUpdated", order.OrderId, order.UserId);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Cập nhật đơn hàng thành công";
            response.Data = $"ORDER_UPDATED_SUCCESS_{order.OrderId}";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi cập nhật đơn hàng: {ex.Message}";
            response.Data = "ORDER_UPDATE_FAILED";
            response.DateTime = DateTime.Now;
            _logger.LogError(ex, "❌ Error updating order {OrderId}", orderVM.OrderId);
        }
        return response;
    }

    public async Task<HTTPResponseClient<string>> DeleteOrder(int orderId)
    {
        var response = new HTTPResponseClient<string>();
        try
        {
            await _unitOfWork.BeginTransaction();

            var order = await _unitOfWork._orderRepository.GetByIdAsync(orderId);
            if (order == null || order.IsDeleted == true)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy đơn hàng";
                response.Data = "ORDER_NOT_FOUND";
                response.DateTime = DateTime.Now;
                return response;
            }

            order.IsDeleted = true;
            order.UpdatedAt = DateTime.Now;
            _unitOfWork._orderRepository.Update(order);
            
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            await InvalidateAllOrderCaches(orderId, order.UserId, order.OrderStatusId);
            await _hubContext.Clients.All.SendAsync("OrderDeleted", orderId);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Xóa đơn hàng thành công";
            response.Data = $"ORDER_DELETED_SUCCESS_{orderId}";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi xóa đơn hàng: {ex.Message}";
            response.Data = "ORDER_DELETE_FAILED";
            response.DateTime = DateTime.Now;
            _logger.LogError(ex, "❌ Error deleting order {OrderId}", orderId);
        }
        return response;
    }

    public async Task<HTTPResponseClient<string>> UpdateOrderStatus(int orderId, int statusId)
    {
        var response = new HTTPResponseClient<string>();
        try
        {
            await _unitOfWork.BeginTransaction();

            var order = await _unitOfWork._orderRepository.GetByIdAsync(orderId);
            if (order == null || order.IsDeleted == true)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy đơn hàng";
                response.Data = "ORDER_NOT_FOUND";
                response.DateTime = DateTime.Now;
                return response;
            }

            var orderStatus = await _unitOfWork._orderStatusRepository.GetByIdAsync(statusId);
            if (orderStatus == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy trạng thái đơn hàng";
                response.Data = "ORDER_STATUS_NOT_FOUND";
                response.DateTime = DateTime.Now;
                return response;
            }

            var oldStatusId = order.OrderStatusId;
            order.OrderStatusId = statusId;
            order.UpdatedAt = DateTime.Now;
            _unitOfWork._orderRepository.Update(order);
            
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            await InvalidateAllOrderCaches(orderId, order.UserId, statusId);
            await _hubContext.Clients.All.SendAsync("OrderStatusUpdated", orderId, statusId);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = $"Cập nhật trạng thái đơn hàng thành công từ {oldStatusId} sang {statusId}";
            response.Data = $"ORDER_STATUS_UPDATED_SUCCESS_{orderId}_{statusId}";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi cập nhật trạng thái đơn hàng: {ex.Message}";
            response.Data = "ORDER_STATUS_UPDATE_FAILED";
            response.DateTime = DateTime.Now;
            _logger.LogError(ex, "❌ Error updating order status {OrderId}", orderId);
        }
        return response;
    }

    public async Task<HTTPResponseClient<IEnumerable<OrderVM>>> GetOrdersByStatus(int statusId)
    {
        var response = new HTTPResponseClient<IEnumerable<OrderVM>>();
        try
        {
            string cacheKey = $"OrdersByStatus_{statusId}";
            var cachedOrders = await _cacheService.GetAsync<IEnumerable<OrderVM>>(cacheKey);
            if (cachedOrders != null)
            {
                response.Data = cachedOrders;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy danh sách đơn hàng theo trạng thái từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var orders = await _unitOfWork._orderRepository.Query()
                .Where(o => o.OrderStatusId == statusId && o.IsDeleted == false)
                .Include(o => o.OrderStatus)
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var orderVMs = orders.Select(o => new OrderVM
            {
                OrderId = o.OrderId,
                UserId = o.UserId,
                OrderStatusId = o.OrderStatusId,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                ShippingAddressId = o.ShippingAddressId,
                CouponId = o.CouponId,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt,
                IsDeleted = o.IsDeleted
            }).ToList();

            await _cacheService.SetAsync(cacheKey, orderVMs, TimeSpan.FromMinutes(15));

            response.Data = orderVMs;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy danh sách đơn hàng theo trạng thái thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy danh sách đơn hàng theo trạng thái: {ex.Message}";
            response.DateTime = DateTime.Now;
            _logger.LogError(ex, "❌ Error getting orders by status {StatusId}", statusId);
        }
        return response;
    }

    public async Task<HTTPResponseClient<IEnumerable<OrderVM>>> GetOrdersByDateRange(DateTime startDate, DateTime endDate)
    {
        var response = new HTTPResponseClient<IEnumerable<OrderVM>>();
        try
        {
            string cacheKey = $"OrdersByDateRange_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}";
            var cachedOrders = await _cacheService.GetAsync<IEnumerable<OrderVM>>(cacheKey);
            if (cachedOrders != null)
            {
                response.Data = cachedOrders;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy danh sách đơn hàng theo thời gian từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var orders = await _unitOfWork._orderRepository.Query()
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate && o.IsDeleted == false)
                .Include(o => o.OrderStatus)
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var orderVMs = orders.Select(o => new OrderVM
            {
                OrderId = o.OrderId,
                UserId = o.UserId,
                OrderStatusId = o.OrderStatusId,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                ShippingAddressId = o.ShippingAddressId,
                CouponId = o.CouponId,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt,
                IsDeleted = o.IsDeleted
            }).ToList();

            await _cacheService.SetAsync(cacheKey, orderVMs, TimeSpan.FromMinutes(30));

            response.Data = orderVMs;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy danh sách đơn hàng theo thời gian thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy danh sách đơn hàng theo thời gian: {ex.Message}";
            response.DateTime = DateTime.Now;
            _logger.LogError(ex, "❌ Error getting orders by date range {StartDate} - {EndDate}", startDate, endDate);
        }
        return response;
    }

    public async Task<HTTPResponseClient<string>> CreateOrderWithItems(OrderVM orderVM, List<OrderItemVM> orderItems)
    {
        var response = new HTTPResponseClient<string>();
        try
        {
            await _unitOfWork.BeginTransaction();

            // Kiểm tra user có tồn tại không
            var user = await _unitOfWork._userRepository.GetByIdAsync(orderVM.UserId);
            if (user == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy người dùng";
                response.Data = "USER_NOT_FOUND";
                response.DateTime = DateTime.Now;
                return response;
            }

            // Tạo đơn hàng với trạng thái Pending
            var pendingStatus = await _unitOfWork._orderStatusRepository.Query()
                .FirstOrDefaultAsync(os => os.StatusName == "Pending" && os.IsDeleted == false);
            
            if (pendingStatus == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy trạng thái Pending";
                response.Data = "PENDING_STATUS_NOT_FOUND";
                response.DateTime = DateTime.Now;
                return response;
            }

            // Tính tổng tiền từ order items
            var totalAmount = orderItems.Sum(oi => oi.Quantity * oi.UnitPrice);

            // 1. Tạo Order
            var order = new Order
            {
                UserId = orderVM.UserId,
                OrderStatusId = pendingStatus.StatusId,
                OrderDate = DateTime.Now,
                TotalAmount = totalAmount,
                ShippingAddressId = orderVM.ShippingAddressId,
                CouponId = orderVM.CouponId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsDeleted = false
            };
            await _unitOfWork._orderRepository.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();

            // 2. Tạo OrderItems
            foreach (var item in orderItems)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.Quantity * item.UnitPrice,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsDeleted = false
                };
                await _unitOfWork._orderItemRepository.AddAsync(orderItem);
            }
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            // 3. 🔥 QUAN TRỌNG: Gửi message tới Kafka
            try
            {
                var orderCreatedMessage = new OrderCreatedMessage
                {
                    RequestId = Guid.NewGuid().ToString(),
                    OrderId = order.OrderId,
                    UserId = order.UserId,
                    OrderItems = orderItems.Select(oi => new OrderItemData
                    {
                        ProductId = oi.ProductId,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice
                    }).ToList(),
                    CreatedAt = order.CreatedAt.Value
                };

                await _kafkaProducer.SendMessageAsync(
                    "order-created",
                    order.OrderId.ToString(),
                    orderCreatedMessage);
                
                _logger.LogInformation("✅ MainService: Successfully sent order created message to Kafka for order {OrderId}", order.OrderId);
            }
            catch (Exception kafkaEx)
            {
                _logger.LogError(kafkaEx, "❌ MainService: Failed to send order created message to Kafka for order {OrderId}", order.OrderId);
                // Không rollback vì đã commit
            }

            // 4. Clear cache và gửi SignalR notifications
            await InvalidateAllOrderCaches(order.OrderId, orderVM.UserId, pendingStatus.StatusId);
            await _hubContext.Clients.All.SendAsync("OrderCreated", order.OrderId, order.UserId, order.TotalAmount);

            response.Success = true;
            response.StatusCode = 201;
            response.Message = "Tạo đơn hàng với chi tiết thành công, đang xử lý sản phẩm";
            response.Data = $"ORDER_WITH_ITEMS_CREATED_SUCCESS_{order.OrderId}";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi tạo đơn hàng: {ex.Message}";
            response.Data = "ORDER_WITH_ITEMS_CREATION_FAILED";
            response.DateTime = DateTime.Now;
            _logger.LogError(ex, "❌ MainService: Error creating order with items for user {UserId}", orderVM.UserId);
        }
        return response;
    }

    public async Task<HTTPResponseClient<string>> UpdateOrderStatusByName(int orderId, string statusName)
    {
        var response = new HTTPResponseClient<string>();
        try
        {
            await _unitOfWork.BeginTransaction();

            var order = await _unitOfWork._orderRepository.GetByIdAsync(orderId);
            if (order == null || order.IsDeleted == true)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy đơn hàng";
                response.Data = "ORDER_NOT_FOUND";
                response.DateTime = DateTime.Now;
                return response;
            }

            var newStatus = await _unitOfWork._orderStatusRepository.Query()
                .FirstOrDefaultAsync(os => os.StatusName == statusName && os.IsDeleted == false);

            if (newStatus == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = $"Không tìm thấy trạng thái {statusName}";
                response.Data = $"STATUS_NOT_FOUND_{statusName}";
                response.DateTime = DateTime.Now;
                return response;
            }

            var oldStatusId = order.OrderStatusId;
            order.OrderStatusId = newStatus.StatusId;
            order.UpdatedAt = DateTime.Now;

            _unitOfWork._orderRepository.Update(order);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            // Clear cache
            await InvalidateAllOrderCaches(orderId, order.UserId, newStatus.StatusId);

            // Send SignalR notification
            await _hubContext.Clients.All.SendAsync("OrderStatusChanged", orderId, order.UserId, newStatus.StatusId, statusName);

            // Send notification to specific user
            await _hubContext.Clients.Group($"User_{order.UserId}")
                .SendAsync("YourOrderStatusChanged", orderId, statusName);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = $"Cập nhật trạng thái đơn hàng thành {statusName} thành công";
            response.Data = $"ORDER_STATUS_UPDATED_SUCCESS_{orderId}_{statusName}";
            response.DateTime = DateTime.Now;

            _logger.LogInformation("✅ Updated order {OrderId} status from {OldStatus} to {NewStatus}", 
                orderId, oldStatusId, newStatus.StatusId);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi cập nhật trạng thái đơn hàng: {ex.Message}";
            response.Data = "ORDER_STATUS_UPDATE_BY_NAME_FAILED";
            response.DateTime = DateTime.Now;
            _logger.LogError(ex, "❌ Error updating order {OrderId} status to {StatusName}", orderId, statusName);
        }
        return response;
    }

    private async Task InvalidateAllOrderCaches(int orderId, int userId, int statusId)
    {
        var cacheKeys = new[]
        {
            "AllOrders",
            $"Order_{orderId}",
            $"OrdersByUser_{userId}",
            $"OrdersByStatus_{statusId}",
            $"OrderItemsByOrder_{orderId}"
        };

        var tasks = cacheKeys.Select(key => _cacheService.DeleteAsync(key));
        await Task.WhenAll(tasks);
    }
}