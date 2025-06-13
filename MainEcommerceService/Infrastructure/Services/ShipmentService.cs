using Microsoft.EntityFrameworkCore;
using MainEcommerceService.Models.ViewModel.ViewModels.ShipmentVM;
using ProductService.Models.ViewModel;
using MainEcommerceService.Models.dbMainEcommer;
using MainEcommerceService.Helper;
using MainEcommerceService.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Interfaces
{
    public interface IShipmentService
    {
        Task<HTTPResponseClient<ShipmentDashboardVM>> GetShipmentDashboardByOrderIdAsync(int orderId);
        Task<HTTPResponseClient<bool>> UpdateShipmentStatusAsync(int shipmentId, int newStatusId);
        Task<List<AssignedOrderVM>> GetAssignedOrdersAsync(int shipperId);
        Task<HTTPResponseClient<bool>> AssignShipmentAsync(int orderId, int shipperId);
        Task<bool> CanOrderBeShippedAsync(int orderId);
        Task<List<OrderStatusOptionVM>> GetAvailableStatusUpdatesAsync(int currentStatusId);
    }

    public class ShipmentService : IShipmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ShipmentService> _logger;
        private readonly RedisHelper _cacheService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public ShipmentService(
            IUnitOfWork unitOfWork, 
            HttpClient httpClient, 
            ILogger<ShipmentService> logger,
            RedisHelper cacheService,
            IHubContext<NotificationHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _httpClient = httpClient;
            _logger = logger;
            _cacheService = cacheService;
            _hubContext = hubContext;
        }

// ✅ SỬA METHOD GetShipmentDashboardByOrderIdAsync THEO PATTERN CHUẨN
public async Task<HTTPResponseClient<ShipmentDashboardVM>> GetShipmentDashboardByOrderIdAsync(int orderId)
{
    var response = new HTTPResponseClient<ShipmentDashboardVM>();
    try
    {
        // ✅ VALIDATION INPUT
        if (orderId <= 0)
        {
            response.Success = false;
            response.StatusCode = 400;
            response.Message = "Order ID không hợp lệ";
            response.Data = null;
            response.DateTime = DateTime.Now;
            return response;
        }

        var order = await _unitOfWork._orderRepository.Query()
            .Include(o => o.User) // Buyer info
            .Include(o => o.ShippingAddress) // Shipping address
            .Include(o => o.OrderStatus) // Order status
            .Include(o => o.OrderItems) // Order items
            .Include(o => o.Shipments) // Shipment info
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.IsDeleted != true);

        if (order == null)
        {
            response.Success = false;
            response.StatusCode = 404;
            response.Message = $"Không tìm thấy đơn hàng với ID {orderId}";
            response.Data = null;
            response.DateTime = DateTime.Now;
            return response;
        }

        // ✅ LUỒNG DUY NHẤT: Hiển thị thông tin bất kể status
        var canUpdate = await CanOrderBeShippedAsync(orderId) || 
                       IsShipmentInProgress(order.OrderStatusId);

        // 🔥 LẤY PRODUCT INFORMATION TỪ PRODUCT SERVICE
        var orderItemsWithProducts = await GetOrderItemsWithProductInfo(order.OrderItems?.ToList() ?? new List<OrderItem>());

        var result = new ShipmentDashboardVM
        {
            OrderInfo = new OrderInfoVM
            {
                OrderId = order.OrderId,
                TotalAmount = order.TotalAmount,
                OrderDate = order.OrderDate,
                CurrentOrderStatus = order.OrderStatus?.StatusName ?? "Unknown",
                OrderStatusId = order.OrderStatusId
            },
            BuyerInfo = new BuyerInfoVM
            {
                UserId = order.UserId,
                FullName = $"{order.User?.FirstName} {order.User?.LastName}".Trim(),
                Email = order.User?.Email ?? "",
                PhoneNumber = order.User?.PhoneNumber ?? ""
            },
            ShippingAddress = order.ShippingAddress != null ? new ShippingAddressVM
            {
                AddressId = order.ShippingAddress.AddressId,
                AddressLine1 = order.ShippingAddress.AddressLine1 ?? "",
                AddressLine2 = order.ShippingAddress.AddressLine2 ?? "",
                City = order.ShippingAddress.City ?? "",
                State = order.ShippingAddress.State ?? "",
                PostalCode = order.ShippingAddress.PostalCode ?? "",
                Country = order.ShippingAddress.Country ?? ""
            } : new ShippingAddressVM(),
            OrderItems = orderItemsWithProducts,
            ShipmentInfo = order.Shipments?.FirstOrDefault() != null ? new ShipmentInfoVM
            {
                ShipmentId = order.Shipments.First().ShipmentId,
                TrackingNumber = order.Shipments.First().TrackingNumber ?? "",
                Status = order.Shipments.First().Status ?? "",
                ShippedDate = order.Shipments.First().ShippedDate,
                DeliveredDate = order.Shipments.First().DeliveredDate,
                CreatedDate = order.Shipments.First().CreatedAt
            } : null,
            AvailableStatusUpdates = canUpdate ? 
                await GetAvailableStatusUpdatesAsync(order.OrderStatusId) : 
                new List<OrderStatusOptionVM>()
        };

        // ✅ SUCCESS RESPONSE
        response.Success = true;
        response.StatusCode = 200;
        response.Message = "Lấy thông tin shipment dashboard thành công";
        response.Data = result;
        response.DateTime = DateTime.Now;

        _logger.LogInformation("✅ Retrieved shipment dashboard for order {OrderId}", orderId);
        return response;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ Error getting shipment dashboard for order {OrderId}", orderId);
        
        response.Success = false;
        response.StatusCode = 500;
        response.Message = $"Lỗi khi lấy thông tin shipment dashboard: {ex.Message}";
        response.Data = null;
        response.DateTime = DateTime.Now;
        
        return response;
    }
}

        // ✅ SỬA METHOD UPDATE THEO PATTERN CHUẨN
        public async Task<HTTPResponseClient<bool>> UpdateShipmentStatusAsync(int shipmentId, int newStatusId)
        {
            var response = new HTTPResponseClient<bool>();
            try
            {
                // 🔥 BẮT ĐẦU TRANSACTION - GIỐNG CÁC SERVICE KHÁC
                await _unitOfWork.BeginTransaction();

                var shipment = await _unitOfWork._shipmentRepository.Query()
                    .Include(s => s.Order)
                    .FirstOrDefaultAsync(s => s.ShipmentId == shipmentId);

                if (shipment == null)
                {
                    response.Success = false;
                    response.StatusCode = 404;
                    response.Message = "Không tìm thấy shipment";
                    response.Data = false;
                    return response;
                }

                var currentStatusId = shipment.Order?.OrderStatusId ?? 0;
                var allowedTransitions = GetAllowedStatusTransitions();
                
                if (!allowedTransitions.ContainsKey(currentStatusId) || 
                    !allowedTransitions[currentStatusId].Contains(newStatusId))
                {
                    response.Success = false;
                    response.StatusCode = 400;
                    response.Message = $"Không thể chuyển từ trạng thái {currentStatusId} sang {newStatusId}";
                    response.Data = false;
                    return response;
                }

                // ✅ CẬP NHẬT SHIPMENT
                shipment.Status = await GetStatusNameByIdAsync(newStatusId);
                shipment.UpdatedAt = DateTime.Now;

                switch (newStatusId)
                {
                    case 5: // In Transit
                        shipment.ShippedDate = DateTime.Now;
                        break;
                    case 7: // Delivered
                        shipment.DeliveredDate = DateTime.Now;
                        break;
                }

                // ✅ CẬP NHẬT ORDER STATUS - QUAN TRỌNG!
                if (shipment.Order != null)
                {
                    shipment.Order.OrderStatusId = newStatusId;
                    shipment.Order.UpdatedAt = DateTime.Now;
                    _unitOfWork._orderRepository.Update(shipment.Order);
                    //signalR
                    
                }

                // ✅ UPDATE SHIPMENT
                _unitOfWork._shipmentRepository.Update(shipment);
                
                // ✅ SAVE CHANGES
                await _unitOfWork.SaveChangesAsync();
                
                // ✅ COMMIT TRANSACTION
                await _unitOfWork.CommitTransaction();

                // ✅ XÓA CACHE
                await InvalidateShipmentCaches(shipmentId, shipment.OrderId);

                // ✅ GỬI THÔNG BÁO REALTIME
                await _hubContext.Clients.All.SendAsync("ShipmentStatusUpdated", shipmentId, newStatusId, shipment.Order?.OrderId);

                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Cập nhật trạng thái shipment thành công";
                response.Data = true;
                response.DateTime = DateTime.Now;

                _logger.LogInformation("✅ Updated shipment {ShipmentId} status to {StatusId}", shipmentId, newStatusId);
                return response;
            }
            catch (Exception ex)
            {
                // ✅ ROLLBACK TRANSACTION NẾU CÓ LỖI
                await _unitOfWork.RollbackTransaction();

                _logger.LogError(ex, "❌ Error updating shipment {ShipmentId} status", shipmentId);
                
                response.Success = false;
                response.StatusCode = 500;
                response.Message = $"Lỗi khi cập nhật trạng thái shipment: {ex.Message}";
                response.Data = false;
                response.DateTime = DateTime.Now;
                
                return response;
            }
        }

        // ✅ SỬA METHOD ASSIGN THEO PATTERN CHUẨN
        public async Task<HTTPResponseClient<bool>> AssignShipmentAsync(int orderId, int shipperId)
        {
            var response = new HTTPResponseClient<bool>();
            try
            {
                // 🔥 BẮT ĐẦU TRANSACTION
                await _unitOfWork.BeginTransaction();

                if (!await CanOrderBeShippedAsync(orderId))
                {
                    response.Success = false;
                    response.StatusCode = 400;
                    response.Message = "Đơn hàng không thể giao";
                    response.Data = false;
                    return response;
                }

                var existingShipment = await _unitOfWork._shipmentRepository.Query()
                    .FirstOrDefaultAsync(s => s.OrderId == orderId);

                if (existingShipment != null)
                {
                    // ✅ CẬP NHẬT SHIPMENT CÓ SẴN
                    existingShipment.ShipperId = shipperId;
                    existingShipment.UpdatedAt = DateTime.Now;
                    _unitOfWork._shipmentRepository.Update(existingShipment);
                }
                else
                {
                    // ✅ TẠO SHIPMENT MỚI
                    var newShipment = new ShipmentVM
                    {
                        OrderId = orderId,
                        ShipperId = shipperId,
                        TrackingNumber = GenerateTrackingNumber(),
                        Status = "In Transit",
                        CreatedAt = DateTime.Now,
                        IsDeleted = false
                    };

                    await _unitOfWork._shipmentRepository.AddAsync(new Shipment
                    {
                        OrderId = newShipment.OrderId,
                        ShipperId = newShipment.ShipperId,
                        TrackingNumber = newShipment.TrackingNumber,
                        Status = newShipment.Status,
                        CreatedAt = newShipment.CreatedAt,
                        IsDeleted = newShipment.IsDeleted
                    });
                }

                // ✅ SAVE CHANGES VÀ COMMIT
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransaction();

                // ✅ XÓA CACHE
                await InvalidateShipmentCaches(0, orderId);

                // ✅ GỬI THÔNG BÁO REALTIME
                await _hubContext.Clients.All.SendAsync("ShipmentAssigned", orderId, shipperId);

                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Assign shipment thành công";
                response.Data = true;
                response.DateTime = DateTime.Now;

                return response;
            }
            catch (Exception ex)
            {
                // ✅ ROLLBACK TRANSACTION
                await _unitOfWork.RollbackTransaction();

                _logger.LogError(ex, "❌ Error assigning shipment for order {OrderId}", orderId);
                
                response.Success = false;
                response.StatusCode = 500;
                response.Message = $"Lỗi khi assign shipment: {ex.Message}";
                response.Data = false;
                response.DateTime = DateTime.Now;
                
                return response;
            }
        }

        // 🔥 THÊM METHOD ĐỂ LẤY PRODUCT INFO TỪ PRODUCT SERVICE
        private async Task<List<OrderItemVM>> GetOrderItemsWithProductInfo(List<OrderItem> orderItems)
        {
            try
            {
                if (!orderItems.Any())
                {
                    return new List<OrderItemVM>();
                }

                var productIds = orderItems.Select(oi => oi.ProductId).Distinct().ToList();
                var productDict = await GetProductInfoFromProductService(productIds);

                var result = orderItems.Select(oi => new OrderItemVM
                {
                    OrderItemId = oi.OrderItemId,
                    ProductId = oi.ProductId,
                    ProductName = productDict.ContainsKey(oi.ProductId) 
                        ? productDict[oi.ProductId].ProductName 
                        : $"Product {oi.ProductId}",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = (oi.Quantity) * (oi.UnitPrice)
                }).ToList();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product info for order items");
                return orderItems.Select(oi => new OrderItemVM
                {
                    OrderItemId = oi.OrderItemId,
                    ProductId = oi.ProductId,
                    ProductName = $"Product {oi.ProductId}",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = (oi.Quantity) * (oi.UnitPrice)
                }).ToList();
            }
        }

        private async Task<Dictionary<int, ProductVM>> GetProductInfoFromProductService(List<int> productIds)
        {
            try
            {
                var productDict = new Dictionary<int, ProductVM>();
                var response = await _httpClient.GetAsync("https://localhost:7252/api/Product/GetAllProducts");
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<ProductVM>>>();
                    
                    if (result?.Success == true && result.Data != null)
                    {
                        productDict = result.Data
                            .Where(p => productIds.Contains(p.ProductId))
                            .ToDictionary(p => p.ProductId, p => p);
                        
                        _logger.LogInformation("✅ Retrieved {Count} products from Product Service", productDict.Count);
                    }
                }

                return productDict;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error calling Product Service");
                return new Dictionary<int, ProductVM>();
            }
        }

        public async Task<bool> CanOrderBeShippedAsync(int orderId)
        {
            try
            {
                var order = await _unitOfWork._orderRepository.Query()
                    .FirstOrDefaultAsync(o => o.OrderId == orderId && o.IsDeleted != true);

                if (order == null) return false;
                return order.OrderStatusId == 4; // Shipped status
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking order shipping eligibility for {OrderId}", orderId);
                return false;
            }
        }

        public async Task<List<AssignedOrderVM>> GetAssignedOrdersAsync(int shipperId)
        {
            try
            {
                var assignedOrders = await _unitOfWork._shipmentRepository.Query()
                    .Include(s => s.Order)
                        .ThenInclude(o => o.User)
                    .Include(s => s.Order)
                        .ThenInclude(o => o.ShippingAddress)
                    .Include(s => s.Order)
                        .ThenInclude(o => o.OrderStatus)
                    .Where(s => s.ShipperId == shipperId && s.Order.IsDeleted != true)
                    .Select(s => new AssignedOrderVM
                    {
                        OrderId = s.Order.OrderId,
                        OrderCode = $"ORD{s.Order.OrderId:D6}",
                        TotalAmount = s.Order.TotalAmount,
                        OrderDate = s.Order.OrderDate,
                        BuyerName = $"{s.Order.User.FirstName} {s.Order.User.LastName}".Trim(),
                        ShippingAddress = s.Order.ShippingAddress != null ? 
                            $"{s.Order.ShippingAddress.AddressLine1}, {s.Order.ShippingAddress.City}" : "",
                        CurrentStatus = s.Order.OrderStatus.StatusName ?? "Unknown",
                        OrderStatusId = s.Order.OrderStatusId
                    })
                    .ToListAsync();

                return assignedOrders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assigned orders for shipper {ShipperId}", shipperId);
                return new List<AssignedOrderVM>();
            }
        }

        public async Task<List<OrderStatusOptionVM>> GetAvailableStatusUpdatesAsync(int currentStatusId)
        {
            try
            {
                var allowedTransitions = GetAllowedStatusTransitions();
                
                if (!allowedTransitions.ContainsKey(currentStatusId))
                {
                    return new List<OrderStatusOptionVM>();
                }

                var allowedStatusIds = allowedTransitions[currentStatusId];
                
                var statusOptions = await _unitOfWork._orderStatusRepository.Query()
                    .Where(os => allowedStatusIds.Contains(os.StatusId))
                    .Select(os => new OrderStatusOptionVM
                    {
                        StatusId = os.StatusId,
                        StatusName = os.StatusName ?? "",
                        Description = os.Description ?? ""
                    })
                    .ToListAsync();

                return statusOptions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available status updates for {StatusId}", currentStatusId);
                return new List<OrderStatusOptionVM>();
            }
        }

        #region Private Helper Methods

        private bool IsShipmentInProgress(int statusId)
        {
            var shipmentStatuses = new List<int> { 4, 5, 6, 7, 9 };
            return shipmentStatuses.Contains(statusId);
        }

        private Dictionary<int, List<int>> GetAllowedStatusTransitions()
        {
            return new Dictionary<int, List<int>>
            {
                { 4, new List<int> { 5 } },        // Shipped → In Transit
                { 5, new List<int> { 6 } },        // In Transit → Out for Delivery
                { 6, new List<int> { 7, 9 } },     // Out for Delivery → Delivered/Returned
            };
        }

        private async Task<string> GetStatusNameByIdAsync(int statusId)
        {
            var status = await _unitOfWork._orderStatusRepository.Query()
                .FirstOrDefaultAsync(os => os.StatusId == statusId);
            
            return status?.StatusName ?? "Unknown";
        }

        private string GenerateTrackingNumber()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            return $"TRK{timestamp}{random}";
        }

        // ✅ CACHE INVALIDATION - GIỐNG CÁC SERVICE KHÁC
        private async Task InvalidateShipmentCaches(int shipmentId, int orderId)
        {
            try
            {
                await _cacheService.DeleteByPatternAsync("AllShipments");
                await _cacheService.DeleteByPatternAsync("PagedShipments_*");
                await _cacheService.DeleteByPatternAsync($"Shipment_{shipmentId}_*");
                await _cacheService.DeleteByPatternAsync($"Order_{orderId}_*");
                await _cacheService.DeleteByPatternAsync("ShipperDashboard_*");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating shipment caches");
            }
        }

        #endregion
    }
}