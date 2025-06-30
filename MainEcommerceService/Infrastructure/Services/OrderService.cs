using Microsoft.AspNetCore.SignalR;
using MainEcommerceService.Hubs;
using MainEcommerceService.Helper;
using MainEcommerceService.Models.dbMainEcommer;
using MainEcommerceService.Models.ViewModel;
using Microsoft.EntityFrameworkCore;
using MainEcommerceService.Kafka;
using MainEcommerceService.Models.Kafka;
using ProductService.Models.ViewModel;

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
    Task<HTTPResponseClient<string>> GetOrderStatusNameByOrderId(int orderId);
    Task<HTTPResponseClient<bool>> CancelOrder(int orderId);
    Task<HTTPResponseClient<IEnumerable<OrderWithDetailsVM>>> GetOrdersBySellerWithDetails(int sellerId);
    Task<HTTPResponseClient<AdminOrdersCompleteView>> GetAllOrdersWithCompleteDetails();
}

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly RedisHelper _cacheService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IKafkaProducerService _kafkaProducer;
    private readonly HttpClient _httpClient;

    public OrderService(
        IUnitOfWork unitOfWork,
        RedisHelper cacheService,
        IHubContext<NotificationHub> hubContext,
        IKafkaProducerService kafkaProducer,
        HttpClient httpClient)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _hubContext = hubContext;
        _kafkaProducer = kafkaProducer;
        _httpClient = httpClient;
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

            await _cacheService.SetAsync(cacheKey, orderVMs, TimeSpan.FromDays(1));

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

            await _cacheService.SetAsync(cacheKey, orderVM, TimeSpan.FromDays(1));

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

            await _cacheService.SetAsync(cacheKey, orderVMs, TimeSpan.FromDays(1));

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
            }
            catch (Exception kafkaEx)
            {
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

            await _cacheService.SetAsync(cacheKey, orderVMs, TimeSpan.FromDays(1));

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

            await _cacheService.SetAsync(cacheKey, orderVMs, TimeSpan.FromDays(1));

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

            }
            catch (Exception kafkaEx)
            {
                // Không rollback vì đã commit
            }

            // 4. Clear cache và gửi SignalR notifications
            await InvalidateAllOrderCaches(order.OrderId, orderVM.UserId, pendingStatus.StatusId);
            await _hubContext.Clients.All.SendAsync("OrderCreated", order.OrderId, order.UserId, order.TotalAmount);

            response.Success = true;
            response.StatusCode = 201;
            response.Message = "Tạo đơn hàng với chi tiết thành công, đang xử lý sản phẩm";
            response.Data = order.OrderId.ToString();
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

        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi cập nhật trạng thái đơn hàng: {ex.Message}";
            response.Data = "ORDER_STATUS_UPDATE_BY_NAME_FAILED";
            response.DateTime = DateTime.Now;
        }
        return response;
    }
    public async Task<HTTPResponseClient<string>> GetOrderStatusNameByOrderId(int orderId)
    {
        var response = new HTTPResponseClient<string>();
        try
        {
            string cacheKey = $"OrderStatus_{orderId}";
            var cachedStatus = await _cacheService.GetAsync<string>(cacheKey);
            if (cachedStatus != null)
            {
                response.Data = cachedStatus;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy trạng thái đơn hàng từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }
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

            var statusName = await _unitOfWork._orderStatusRepository.Query()
                .Where(os => os.StatusId == order.OrderStatusId && os.IsDeleted == false)
                .Select(os => os.StatusName)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(statusName))
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy trạng thái đơn hàng";
                response.Data = "ORDER_STATUS_NOT_FOUND";
                response.DateTime = DateTime.Now;
                return response;
            }

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy trạng thái đơn hàng thành công";
            response.Data = statusName;
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy trạng thái đơn hàng: {ex.Message}";
            response.Data = "ORDER_STATUS_RETRIEVAL_FAILED";
            response.DateTime = DateTime.Now;
        }
        return response;
    }
    public async Task<HTTPResponseClient<bool>> CancelOrder(int orderId)
    {
        var response = new HTTPResponseClient<bool>();
        try
        {
            await _unitOfWork.BeginTransaction();
            var order = await _unitOfWork._orderRepository.GetByIdAsync(orderId);
            if (order == null || order.IsDeleted == true)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy đơn hàng";
                response.Data = false;
                response.DateTime = DateTime.Now;
                return response;
            }

            // Get order items before sending Kafka message
            var orderItemsFromDb = await _unitOfWork._orderItemRepository.Query()
                .Where(oi => oi.OrderId == orderId && oi.IsDeleted == false)
                .ToListAsync();
            //Kiểm tra trạng thát đơn hàng nếu đang ở trạng thái Shipped trở lên (statusId >= 4) thì không thể hủy
            if (order.OrderStatusId >= 4)
            {
                response.Success = false;
                response.StatusCode = 400;
                response.Message = "Không thể hủy đơn hàng đã được giao hoặc đang trong quá trình giao hàng";
                response.Data = false;
                response.DateTime = DateTime.Now;
                return response;
            }
            try
            {
                // Gửi message tới Kafka để xử lý hủy đơn hàng
                var orderCancelledMessage = new OrderCreatedMessage
                {
                    RequestId = Guid.NewGuid().ToString(),
                    OrderId = order.OrderId,
                    UserId = order.UserId,
                    OrderItems = orderItemsFromDb.Select(oi => new OrderItemData
                    {
                        ProductId = oi.ProductId,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice
                    }).ToList(),
                    CreatedAt = order.CreatedAt.Value
                };
                await _kafkaProducer.SendMessageAsync(
                    "order-cancelled",
                    order.OrderId.ToString(),
                    orderCancelledMessage);
            }
            catch (Exception kafkaEx)
            {
            }
            //Nếu message gửi thành công, cập nhật trạng thái đơn hàng
            order.OrderStatusId = (await _unitOfWork._orderStatusRepository.Query()
                .FirstOrDefaultAsync(os => os.StatusName == "Cancelled" && os.IsDeleted == false)).StatusId;
            // Đánh dấu đơn hàng là đã bị xóa
            if (order.OrderStatusId == 8)
            {
                order.UpdatedAt = DateTime.Now;
                _unitOfWork._orderRepository.Update(order);
                await _unitOfWork.SaveChangesAsync();
                await _hubContext.Clients.All.SendAsync("OrderStatusChanged", orderId, order.UserId, order.OrderStatusId, "Cancelled");
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Hủy đơn hàng thành công";
                response.Data = true;
                response.DateTime = DateTime.Now;
            }
            await _unitOfWork.CommitTransaction();
            await InvalidateAllOrderCaches(orderId, order.UserId, order.OrderStatusId);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi hủy đơn hàng: {ex.Message}";
            response.Data = false;
            response.DateTime = DateTime.Now;
        }
        return response;
    }


public async Task<HTTPResponseClient<IEnumerable<OrderWithDetailsVM>>> GetOrdersBySellerWithDetails(int sellerId)
{
    var response = new HTTPResponseClient<IEnumerable<OrderWithDetailsVM>>();
    try
    {
        string cacheKey = $"OrdersBySellerWithDetails_{sellerId}";
        var cachedOrders = await _cacheService.GetAsync<IEnumerable<OrderWithDetailsVM>>(cacheKey);
        if (cachedOrders != null)
        {
            response.Data = cachedOrders;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy đơn hàng theo seller từ cache thành công";
            response.DateTime = DateTime.Now;
            return response;
        }

        // 🔥 BƯỚC 1: Gọi Product service CHỈ 1 LẦN để lấy tất cả products của seller
        var sellerProducts = await GetProductsBySellerFromProductService(sellerId);
        var sellerProductIds = sellerProducts.Select(p => p.ProductId).ToHashSet();
        
        // 🔥 BƯỚC 2: Nếu seller không có product nào thì return empty
        if (!sellerProductIds.Any())
        {
            response.Data = Enumerable.Empty<OrderWithDetailsVM>();
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Seller không có sản phẩm nào";
            response.DateTime = DateTime.Now;
            return response;
        }

        // 🔥 BƯỚC 3: Query database với filter ProductId IN sellerProductIds
        var ordersWithDetails = await _unitOfWork._orderRepository.Query()
            .Where(o => o.IsDeleted == false)
            .Join(_unitOfWork._orderItemRepository.Query().Where(oi => 
                oi.IsDeleted == false && 
                sellerProductIds.Contains(oi.ProductId)), // 🔥 Filter ngay trong JOIN
                order => order.OrderId,
                orderItem => orderItem.OrderId,
                (order, orderItem) => new { Order = order, OrderItem = orderItem })
            .GroupBy(joined => joined.Order)
            .Select(group => new OrderWithDetailsVM
            {
                OrderId = group.Key.OrderId,
                UserId = group.Key.UserId,
                OrderStatusId = group.Key.OrderStatusId,
                OrderDate = group.Key.OrderDate,
                TotalAmount = group.Key.TotalAmount,
                ShippingAddressId = group.Key.ShippingAddressId,
                CouponId = group.Key.CouponId,
                CreatedAt = group.Key.CreatedAt,
                UpdatedAt = group.Key.UpdatedAt,
                IsDeleted = group.Key.IsDeleted,
                OrderItems = group.Select(g => new OrderItemWithProductVM
                {
                    OrderItemId = g.OrderItem.OrderItemId,
                    OrderId = g.OrderItem.OrderId,
                    ProductId = g.OrderItem.ProductId,
                    Quantity = g.OrderItem.Quantity,
                    UnitPrice = g.OrderItem.UnitPrice,
                    TotalPrice = g.OrderItem.TotalPrice,
                    CreatedAt = g.OrderItem.CreatedAt,
                    UpdatedAt = g.OrderItem.UpdatedAt,
                    IsDeleted = g.OrderItem.IsDeleted,
                    SellerId = sellerId,
                    ProductName = "Unknown Product" // Set default value, will be populated later
                }).ToList()
            })
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        // 🔥 BƯỚC 4: Populate product names after query execution
        foreach (var order in ordersWithDetails)
        {
            foreach (var orderItem in order.OrderItems)
            {
                var product = sellerProducts.FirstOrDefault(p => p.ProductId == orderItem.ProductId);
                orderItem.ProductName = product?.ProductName ?? "Unknown Product";
            }
        }

        // 🔥 BƯỚC 4: Không cần vòng lặp để filter nữa vì đã filter trong query rồi!
        
        await _cacheService.SetAsync(cacheKey, ordersWithDetails, TimeSpan.FromDays(1));

        response.Data = ordersWithDetails;
        response.Success = true;
        response.StatusCode = 200;
        response.Message = "Lấy đơn hàng theo seller thành công";
        response.DateTime = DateTime.Now;
        
    }
    catch (Exception ex)
    {
        response.Success = false;
        response.StatusCode = 500;
        response.Message = $"Lỗi khi lấy đơn hàng theo seller: {ex.Message}";
        response.DateTime = DateTime.Now;
    }
    return response;
}
    public async Task<HTTPResponseClient<AdminOrdersCompleteView>> GetAllOrdersWithCompleteDetails()
    {
        var response = new HTTPResponseClient<AdminOrdersCompleteView>();
        try
        {
            string cacheKey = "AdminOrdersCompleteView";
            var cachedData = await _cacheService.GetAsync<AdminOrdersCompleteView>(cacheKey);
            if (cachedData != null)
            {
                response.Data = cachedData;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy dữ liệu đơn hàng từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            // 🔥 SINGLE COMPLEX QUERY với tất cả joins
            var ordersWithDetails = await _unitOfWork._orderRepository.Query()
                .Where(o => o.IsDeleted == false)
                .Include(o => o.OrderItems.Where(oi => oi.IsDeleted == false))
                .Include(o => o.User)
                .Include(o => o.OrderStatus)
                .Select(o => new OrderWithCompleteDetailsVM
                {
                    // Order info
                    OrderId = o.OrderId,
                    UserId = o.UserId,
                    OrderStatusId = o.OrderStatusId,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    ShippingAddressId = o.ShippingAddressId,
                    CouponId = o.CouponId,
                    CreatedAt = o.CreatedAt,
                    UpdatedAt = o.UpdatedAt,
                    IsDeleted = o.IsDeleted,

                    // Order Status info
                    OrderStatusName = o.OrderStatus.StatusName,

                    // Customer info
                    CustomerFirstName = o.User.FirstName,
                    CustomerLastName = o.User.LastName,
                    CustomerEmail = o.User.Email,
                    CustomerPhone = o.User.PhoneNumber,

                    // Order Items
                    OrderItems = o.OrderItems.Select(oi => new OrderItemWithProductDetailsVM
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
                    }).ToList()
                })
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            // 🔥 SINGLE BATCH CALL đến Product Service để lấy tất cả product info
            var allProductIds = ordersWithDetails
                .SelectMany(o => o.OrderItems.Select(oi => oi.ProductId))
                .Distinct()
                .ToList();

            var productDetails = await GetAllProductsFromProductService();
            var productDetailsDict = productDetails.ToDictionary(p => p.ProductId, p => p);
            // 🔥 Populate product information từ dữ liệu đã có
            foreach (var order in ordersWithDetails)
            {
                foreach (var item in order.OrderItems)
                {
                    if (productDetailsDict.TryGetValue(item.ProductId, out var productInfo))
                    {
                        item.ProductName = productInfo.ProductName;
                        item.SellerStoreName = _unitOfWork._sellerProfileRepository
                            .Query()
                            .Where(sp => sp.SellerId == productInfo.SellerId && sp.IsDeleted == false)
                            .Select(sp => sp.StoreName)
                            .FirstOrDefault() ?? "Unknown Store";
                        item.SellerId = productInfo.SellerId;
                    }
                    else
                    {
                        item.ProductName = "Unknown Product";
                        item.SellerId = 0;
                    }
                }
            }

            var completeView = new AdminOrdersCompleteView
            {
                Orders = ordersWithDetails,
                TotalOrders = ordersWithDetails.Count,
                PendingOrders = ordersWithDetails.Count(o => o.OrderStatusName == "Pending"),
                LoadedAt = DateTime.Now
            };

            // Cache for 10 minutes
            await _cacheService.SetAsync(cacheKey, completeView, TimeSpan.FromDays(1));

            response.Data = completeView;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy dữ liệu đơn hàng thành công";
            response.DateTime = DateTime.Now;

        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy dữ liệu đơn hàng: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }
    private async Task<IEnumerable<ProductVM>> GetAllProductsFromProductService()
    {
        var response = await _httpClient.GetAsync("https://localhost:7252/api/Product/GetAllProducts");
            
            // Call to Product service - you can inject HttpClient or use existing service
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<ProductVM>>>();
            return result?.Data ?? Enumerable.Empty<ProductVM>();
        }

        return Enumerable.Empty<ProductVM>();
    
}
    private async Task<IEnumerable<ProductVM>> GetProductsBySellerFromProductService(int sellerId)
    {
        // Call to Product service - you can inject HttpClient or use existing service
        var response = await _httpClient.GetAsync($"https://localhost:7252/api/Product/GetProductsBySeller/{sellerId}");

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<ProductVM>>>();
            return result?.Data ?? Enumerable.Empty<ProductVM>();
        }

        return Enumerable.Empty<ProductVM>();
    }
    private async Task InvalidateAllOrderCaches(int orderId, int userId, int statusId)
    {
        var cacheKeys = new[]
        {
            "AllOrders",
            $"Order_{orderId}",
            $"OrdersByUser_{userId}",
            $"OrdersByStatus_{statusId}",
            $"OrderItemsByOrder_{orderId}",
            $"OrderStatus_{orderId}",
            $"AllProducts",
            $"OrdersBySellerWithDetails_*",
            $"AdminOrdersCompleteView"
        };

        var tasks = cacheKeys.Select(key => _cacheService.DeleteByPatternAsync(key));
        await Task.WhenAll(tasks);
    }
    // ...existing code...
}