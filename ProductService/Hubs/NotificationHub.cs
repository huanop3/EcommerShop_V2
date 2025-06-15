using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;

namespace ProductService.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;
        private static readonly ConcurrentDictionary<string, string> _userConnections = new ConcurrentDictionary<string, string>();

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        // Phương thức chung để gửi thông báo
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        // CRUD Product Operations
        public async Task NotifyProductCreated(int productId, string productName, string categoryName)
        {
            _logger.LogInformation($"Gửi thông báo tạo sản phẩm mới: {productId} - {productName}");
            await Clients.All.SendAsync("ProductCreated", productId, productName, categoryName);
        }

        public async Task NotifyProductUpdated(int productId, string productName, decimal price)
        {
            _logger.LogInformation($"Gửi thông báo cập nhật sản phẩm: {productId} - {productName}");
            await Clients.All.SendAsync("ProductUpdated", productId, productName, price);
        }

        [Authorize(Roles = "Admin,Seller")]
        public async Task NotifyProductDeleted(int productId, string productName)
        {
            _logger.LogInformation($"Gửi thông báo xóa sản phẩm: {productId} - {productName}");
            await Clients.All.SendAsync("ProductDeleted", productId, productName);
        }

        public async Task NotifyProductStockChanged(int productId, string productName, int newStock)
        {
            _logger.LogInformation($"Gửi thông báo thay đổi tồn kho sản phẩm: {productId} - Stock: {newStock}");
            await Clients.All.SendAsync("ProductStockChanged", productId, productName, newStock);
        }

        public async Task NotifyProductPriceChanged(int productId, string productName, decimal oldPrice, decimal newPrice)
        {
            _logger.LogInformation($"Gửi thông báo thay đổi giá sản phẩm: {productId} - {oldPrice} -> {newPrice}");
            await Clients.All.SendAsync("ProductPriceChanged", productId, productName, oldPrice, newPrice);
        }

        // Category Operations
        public async Task NotifyCategoryCreated(string categoryName)
        {
            _logger.LogInformation($"Gửi thông báo tạo danh mục mới: {categoryName}");
            await Clients.All.SendAsync("CategoryCreated", categoryName);
        }

        public async Task NotifyCategoryUpdated(int categoryId, string categoryName)
        {
            _logger.LogInformation($"Gửi thông báo cập nhật danh mục: {categoryId} - {categoryName}");
            await Clients.All.SendAsync("CategoryUpdated", categoryId, categoryName);
        }

        [Authorize(Roles = "Admin")]
        public async Task NotifyCategoryDeleted(int categoryId, string categoryName)
        {
            _logger.LogInformation($"Gửi thông báo xóa danh mục: {categoryId} - {categoryName}");
            await Clients.All.SendAsync("CategoryDeleted", categoryId, categoryName);
        }
        // Group Management for Categories
        public async Task JoinCategoryGroup(string categoryId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Category_{categoryId}");
            _logger.LogInformation($"Client {Context.ConnectionId} đã tham gia nhóm Category_{categoryId}");
        }

        public async Task LeaveCategoryGroup(string categoryId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Category_{categoryId}");
            _logger.LogInformation($"Client {Context.ConnectionId} đã rời khỏi nhóm Category_{categoryId}");
        }

        // Gửi thông báo cho nhóm danh mục cụ thể
        public async Task SendCategoryNotification(string categoryId, string message)
        {
            _logger.LogInformation($"Gửi thông báo đến nhóm Category_{categoryId}: {message}");
            await Clients.Group($"Category_{categoryId}").SendAsync("CategoryNotification", categoryId, message);
        }

        // Đăng ký user-connection mapping
        public async Task RegisterUserConnection(string userId)
        {
            _userConnections[Context.ConnectionId] = userId;
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            _logger.LogInformation($"User {userId} đã đăng ký kết nối với ID: {Context.ConnectionId}");
        }

        // Gửi thông báo riêng cho từng user (ví dụ: sản phẩm trong wishlist có thay đổi)
        public async Task SendPrivateNotification(string userId, string message)
        {
            _logger.LogInformation($"Gửi thông báo riêng đến user {userId}: {message}");
            await Clients.Group($"User_{userId}").SendAsync("PrivateNotification", message);
        }

        // Thông báo sản phẩm sắp hết hàng (chỉ cho Admin/Seller)
        [Authorize(Roles = "Admin,Seller")]
        public async Task NotifyLowStock(int productId, string productName, int currentStock, int minStock)
        {
            _logger.LogInformation($"Cảnh báo tồn kho thấp: {productName} - {currentStock}/{minStock}");
            await Clients.Group("Sellers").SendAsync("LowStockAlert", productId, productName, currentStock, minStock);
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"Client kết nối: {Context.ConnectionId}");
            
            // Tự động thêm vào nhóm tất cả người dùng để nhận thông báo chung
            await Groups.AddToGroupAsync(Context.ConnectionId, "AllUsers");
            
            // Nếu là Seller thì thêm vào nhóm Sellers
            if (Context.User?.IsInRole("Seller") == true || Context.User?.IsInRole("Admin") == true)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Sellers");
            }
            
            await Clients.All.SendAsync("UserConnected", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation($"Client ngắt kết nối: {Context.ConnectionId}");
            
            // Xóa mapping khi user ngắt kết nối
            if (_userConnections.TryRemove(Context.ConnectionId, out string userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
                _logger.LogInformation($"Đã xóa user {userId} khỏi nhóm");
            }
            
            await Clients.All.SendAsync("UserDisconnected", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}