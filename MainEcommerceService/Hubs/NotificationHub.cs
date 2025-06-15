using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;

namespace MainEcommerceService.Hubs
{
    public class NotificationHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> _userConnections = new();

        #region Connection Management

        public override async Task OnConnectedAsync()
        {
            // Kiểm tra nếu là admin thì tự động join AdminGroup
            if (Context.User?.IsInRole("Admin") == true)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "AdminGroup");
            }

            // Kiểm tra nếu là seller thì join VerifiedSellers group
            if (Context.User?.IsInRole("Seller") == true)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "VerifiedSellers");
            }

            // Kiểm tra nếu là shipper thì join ActiveShippers group
            if (Context.User?.IsInRole("Shipper") == true)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "ActiveShippers");
            }

            // Gửi thông báo user connected
            await Clients.All.SendAsync("UserConnected", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Xóa mapping khi user ngắt kết nối
            if (_userConnections.TryRemove(Context.ConnectionId, out string userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
            }

            // Xóa khỏi AdminGroup nếu là admin
            if (Context.User?.IsInRole("Admin") == true)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AdminGroup");
            }

            // Xóa khỏi VerifiedSellers nếu là seller
            if (Context.User?.IsInRole("Seller") == true)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "VerifiedSellers");
            }

            // Xóa khỏi ActiveShippers nếu là shipper
            if (Context.User?.IsInRole("Shipper") == true)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "ActiveShippers");
            }

            // Gửi thông báo user disconnected
            await Clients.All.SendAsync("UserDisconnected", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task RegisterUserConnection(string userId)
        {
            _userConnections[Context.ConnectionId] = userId;
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        }

        #endregion

        #region Address Management Operations

        public async Task NotifyAddressCreated(int userId, string addressType)
        {
            await Clients.All.SendAsync("AddressCreated", userId, addressType);
            await Clients.Group($"User_{userId}").SendAsync("YourAddressCreated", addressType);
        }

        public async Task NotifyAddressUpdated(int addressId, int userId, string addressType)
        {
            await Clients.All.SendAsync("AddressUpdated", addressId, userId, addressType);
            await Clients.Group($"User_{userId}").SendAsync("YourAddressUpdated", addressId, addressType);
        }

        public async Task NotifyAddressDeleted(int addressId, int userId)
        {
            await Clients.All.SendAsync("AddressDeleted", addressId, userId);
            await Clients.Group($"User_{userId}").SendAsync("YourAddressDeleted", addressId);
        }

        public async Task NotifyDefaultAddressChanged(int userId, int newDefaultAddressId)
        {
            await Clients.All.SendAsync("DefaultAddressChanged", userId, newDefaultAddressId);
            await Clients.Group($"User_{userId}").SendAsync("YourDefaultAddressChanged", newDefaultAddressId);
        }

        #endregion

        #region Seller Profile Management Operations

        public async Task NotifySellerProfileCreated(string companyName)
        {
            await Clients.All.SendAsync("SellerProfileCreated", companyName);
            await Clients.Groups("AdminGroup").SendAsync("NewSellerApplication", companyName);
        }

        public async Task NotifySellerProfileUpdated(int sellerId, string companyName)
        {
            await Clients.All.SendAsync("SellerProfileUpdated", sellerId, companyName);
            await Clients.Group($"Seller_{sellerId}").SendAsync("YourSellerProfileUpdated", companyName);
        }

        public async Task NotifySellerProfileDeleted(int sellerId)
        {
            await Clients.All.SendAsync("SellerProfileDeleted", sellerId);
            await Clients.Group($"Seller_{sellerId}").SendAsync("YourSellerProfileDeleted", "Your seller profile has been deleted by admin");
        }

        public async Task NotifySellerProfileVerified(int sellerId)
        {
            await Clients.All.SendAsync("SellerProfileVerified", sellerId);
            await Clients.Group($"Seller_{sellerId}").SendAsync("YourSellerProfileVerified", "Your seller profile has been verified! You can now start selling.");
            var userId = GetUserIdFromSellerId(sellerId);
            await Clients.Group($"User_{userId}").SendAsync("SellerVerificationApproved", "Your seller profile is now verified!");
        }

        public async Task NotifySellerProfileUnverified(int sellerId)
        {
            await Clients.All.SendAsync("SellerProfileUnverified", sellerId);
            await Clients.Group($"Seller_{sellerId}").SendAsync("YourSellerProfileUnverified", "Your seller profile has been unverified by admin");
            var userId = GetUserIdFromSellerId(sellerId);
            await Clients.Group($"User_{userId}").SendAsync("SellerVerificationRevoked", "Your seller verification has been revoked");
        }

        #endregion

        #region Seller Groups Management

        public async Task RegisterSellerConnection(int sellerId, int userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Seller_{sellerId}");
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        }

        public async Task UnregisterSellerConnection(int sellerId, int userId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Seller_{sellerId}");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
        }

        [Authorize(Roles = "Admin")]
        public async Task SendPrivateSellerNotification(int sellerId, string message, string type = "info")
        {
            await Clients.Group($"Seller_{sellerId}").SendAsync("PrivateSellerNotification", message, type);
        }

        [Authorize(Roles = "Admin")]
        public async Task SendSellerBroadcast(string message, string type = "info")
        {
            await Clients.Groups("VerifiedSellers").SendAsync("SellerBroadcast", message, type);
        }

        #endregion

        #region Shipper Profile Management Operations

        public async Task NotifyShipperProfileCreated(string shipperName)
        {
            await Clients.All.SendAsync("ShipperProfileCreated", shipperName);
            await Clients.Groups("AdminGroup").SendAsync("NewShipperApplication", shipperName);
        }

        public async Task NotifyShipperProfileUpdated(int shipperId, string shipperName)
        {
            await Clients.All.SendAsync("ShipperProfileUpdated", shipperId, shipperName);
            await Clients.Group($"Shipper_{shipperId}").SendAsync("YourShipperProfileUpdated", shipperName);
        }

        public async Task NotifyShipperProfileDeleted(int shipperId)
        {
            await Clients.All.SendAsync("ShipperProfileDeleted", shipperId);
            await Clients.Group($"Shipper_{shipperId}").SendAsync("YourShipperProfileDeleted", "Your shipper profile has been deleted by admin");
        }

        public async Task NotifyShipperProfileActivated(int shipperId)
        {
            await Clients.All.SendAsync("ShipperProfileActivated", shipperId);
            await Clients.Group($"Shipper_{shipperId}").SendAsync("YourShipperProfileActivated", "Your shipper profile has been activated! You can now accept deliveries.");
            var userId = GetUserIdFromShipperId(shipperId);
            await Clients.Group($"User_{userId}").SendAsync("ShipperActivationApproved", "Your shipper profile is now active!");
        }

        public async Task NotifyShipperProfileDeactivated(int shipperId)
        {
            await Clients.All.SendAsync("ShipperProfileDeactivated", shipperId);
            await Clients.Group($"Shipper_{shipperId}").SendAsync("YourShipperProfileDeactivated", "Your shipper profile has been deactivated by admin");
            var userId = GetUserIdFromShipperId(shipperId);
            await Clients.Group($"User_{userId}").SendAsync("ShipperDeactivationNotice", "Your shipper profile has been deactivated");
        }

        #endregion

        #region Shipper Groups Management

        public async Task RegisterShipperConnection(int shipperId, int userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Shipper_{shipperId}");
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        }

        public async Task UnregisterShipperConnection(int shipperId, int userId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Shipper_{shipperId}");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
        }

        [Authorize(Roles = "Admin")]
        public async Task SendPrivateShipperNotification(int shipperId, string message, string type = "info")
        {
            await Clients.Group($"Shipper_{shipperId}").SendAsync("PrivateShipperNotification", message, type);
        }

        [Authorize(Roles = "Admin")]
        public async Task SendShipperBroadcast(string message, string type = "info")
        {
            await Clients.Groups("ActiveShippers").SendAsync("ShipperBroadcast", message, type);
        }

        #endregion

        #region User Management Operations

        public async Task NotifyUserUpdated(int userId, string userName)
        {
            await Clients.All.SendAsync("UserUpdated", userId, userName);
            await Clients.Group($"User_{userId}").SendAsync("YourProfileUpdated", "Your profile has been updated successfully");
        }

        public async Task NotifyUserStatusChanged(int userId, bool isActive)
        {
            await Clients.All.SendAsync("UserStatusChanged", userId, isActive);
            await Clients.Group($"User_{userId}").SendAsync("YourStatusChanged", isActive ? "Your account has been activated" : "Your account has been deactivated");
        }

        [Authorize(Roles = "Admin")]
        public async Task SendPrivateUserNotification(int userId, string message, string type = "info")
        {
            await Clients.Group($"User_{userId}").SendAsync("PrivateUserNotification", message, type);
        }

        [Authorize(Roles = "Admin")]
        public async Task SendGlobalBroadcast(string message, string type = "info")
        {
            await Clients.All.SendAsync("GlobalBroadcast", message, type);
        }

        #endregion

        #region Order Management Operations

        public async Task NotifyOrderCreated(int orderId, int customerId, int? sellerId = null)
        {
            await Clients.All.SendAsync("OrderCreated", orderId);
            await Clients.Group($"User_{customerId}").SendAsync("YourOrderCreated", orderId);
            
            if (sellerId.HasValue)
            {
                await Clients.Group($"Seller_{sellerId}").SendAsync("NewOrderReceived", orderId);
            }
            
            await Clients.Groups("AdminGroup").SendAsync("NewOrderNotification", orderId);
        }

        public async Task NotifyOrderStatusChanged(int orderId, string newStatus, int customerId, int? sellerId = null, int? shipperId = null)
        {
            await Clients.All.SendAsync("OrderStatusChanged", orderId, newStatus);
            await Clients.Group($"User_{customerId}").SendAsync("YourOrderStatusChanged", orderId, newStatus, $"Your order status has been updated to: {newStatus}");
            
            if (sellerId.HasValue)
            {
                await Clients.Group($"Seller_{sellerId}").SendAsync("OrderStatusUpdated", orderId, newStatus);
            }
            
            if (shipperId.HasValue)
            {
                await Clients.Group($"Shipper_{shipperId}").SendAsync("DeliveryStatusUpdated", orderId, newStatus);
            }
        }

        public async Task NotifyOrderAssignedToShipper(int orderId, int shipperId, int customerId)
        {
            await Clients.Group($"Shipper_{shipperId}").SendAsync("OrderAssignedToYou", orderId);
            await Clients.Group($"User_{customerId}").SendAsync("YourOrderAssignedToShipper", orderId);
            await Clients.Groups("AdminGroup").SendAsync("OrderAssignedNotification", orderId, shipperId);
        }

        #endregion

        #region Product Management Operations

        public async Task NotifyProductCreated(int productId, string productName, int sellerId)
        {
            await Clients.All.SendAsync("ProductCreated", productId, productName);
            await Clients.Group($"Seller_{sellerId}").SendAsync("YourProductCreated", productId, productName);
        }

        public async Task NotifyProductUpdated(int productId, string productName, int sellerId)
        {
            await Clients.All.SendAsync("ProductUpdated", productId, productName);
            await Clients.Group($"Seller_{sellerId}").SendAsync("YourProductUpdated", productId, productName);
        }

        public async Task NotifyProductDeleted(int productId, int sellerId)
        {
            await Clients.All.SendAsync("ProductDeleted", productId);
            await Clients.Group($"Seller_{sellerId}").SendAsync("YourProductDeleted", productId);
        }

        public async Task NotifyProductOutOfStock(int productId, string productName, int sellerId)
        {
            await Clients.Group($"Seller_{sellerId}").SendAsync("ProductOutOfStock", productId, productName);
            await Clients.Groups("AdminGroup").SendAsync("ProductStockAlert", productId, productName);
        }

        #endregion

        #region Shipment Management Operations

        public async Task NotifyShipmentCreated(int shipmentId, int orderId, int shipperId)
        {
            await Clients.All.SendAsync("ShipmentCreated", shipmentId, orderId, shipperId);
            await Clients.Group($"Shipper_{shipperId}").SendAsync("NewDeliveryAssignment", orderId, "New delivery assignment");
            
            // Notify customer about shipment creation
            var customerId = await GetCustomerIdFromOrderId(orderId);
            await Clients.Group($"User_{customerId}").SendAsync("YourShipmentAssigned", orderId, $"Shipper_{shipperId}");
            
            await Clients.Groups("AdminGroup").SendAsync("ShipmentCreatedNotification", shipmentId, orderId);
        }

        public async Task NotifyShipmentUpdated(int shipmentId, int orderId, string status)
        {
            await Clients.All.SendAsync("ShipmentUpdated", shipmentId, orderId, status);
            
            // Get related parties
            var customerId = await GetCustomerIdFromOrderId(orderId);
            var shipperId = await GetShipperIdFromShipmentId(shipmentId);
            
            await Clients.Group($"User_{customerId}").SendAsync("YourShipmentStatusChanged", orderId, status, $"Your shipment status has been updated to: {status}");
            await Clients.Group($"Shipper_{shipperId}").SendAsync("ShipmentStatusUpdated", shipmentId, status);
        }

        public async Task NotifyShipmentDeleted(int shipmentId)
        {
            await Clients.All.SendAsync("ShipmentDeleted", shipmentId);
            await Clients.Groups("AdminGroup").SendAsync("ShipmentDeletedNotification", shipmentId);
        }

        public async Task NotifyShipmentStatusUpdated(int shipmentId, int orderId, string status)
        {
            await Clients.All.SendAsync("ShipmentStatusUpdated", shipmentId, orderId, status);
            
            var customerId = await GetCustomerIdFromOrderId(orderId);
            var shipperId = await GetShipperIdFromShipmentId(shipmentId);
            
            // Different messages based on status
            string customerMessage = status switch
            {
                "Picked Up" => "Your order has been picked up by the shipper",
                "In Transit" => "Your order is on the way",
                "Out for Delivery" => "Your order is out for delivery",
                "Delivered" => "Your order has been delivered successfully!",
                "Failed Delivery" => "Delivery attempt failed. The shipper will retry.",
                _ => $"Your shipment status: {status}"
            };
            
            await Clients.Group($"User_{customerId}").SendAsync("YourShipmentStatusChanged", orderId, status, customerMessage);
            await Clients.Group($"Shipper_{shipperId}").SendAsync("DeliveryStatusUpdated", shipmentId, orderId, status);
        }

        public async Task NotifyShipperAssigned(int orderId, int shipperId)
        {
            await Clients.All.SendAsync("ShipperAssigned", orderId, shipperId);
            
            var customerId = await GetCustomerIdFromOrderId(orderId);
            var shipperName = await GetShipperNameFromId(shipperId);
            
            await Clients.Group($"Shipper_{shipperId}").SendAsync("NewDeliveryAssignment", orderId, "You have been assigned a new delivery");
            await Clients.Group($"User_{customerId}").SendAsync("YourShipmentAssigned", orderId, shipperName);
            await Clients.Groups("AdminGroup").SendAsync("ShipperAssignedNotification", orderId, shipperId);
        }

        public async Task NotifyTrackingNumberUpdated(int shipmentId, string trackingNumber)
        {
            await Clients.All.SendAsync("TrackingNumberUpdated", shipmentId, trackingNumber);
            
            var orderId = await GetOrderIdFromShipmentId(shipmentId);
            var customerId = await GetCustomerIdFromOrderId(orderId);
            var shipperId = await GetShipperIdFromShipmentId(shipmentId);
            
            await Clients.Group($"User_{customerId}").SendAsync("YourTrackingNumberUpdated", orderId, trackingNumber);
            await Clients.Group($"Shipper_{shipperId}").SendAsync("TrackingNumberAssigned", shipmentId, trackingNumber);
        }

        public async Task NotifyShipmentDelivered(int shipmentId, int orderId)
        {
            await Clients.All.SendAsync("ShipmentDelivered", shipmentId, orderId);
            
            var customerId = await GetCustomerIdFromOrderId(orderId);
            var shipperId = await GetShipperIdFromShipmentId(shipmentId);
            
            await Clients.Group($"User_{customerId}").SendAsync("YourShipmentDelivered", orderId, "Your order has been successfully delivered!");
            await Clients.Group($"Shipper_{shipperId}").SendAsync("DeliveryCompleted", shipmentId, orderId);
            await Clients.Groups("AdminGroup").SendAsync("DeliveryCompletedNotification", shipmentId, orderId);
        }

        public async Task NotifyShipmentPickedUp(int shipmentId, int orderId)
        {
            await Clients.All.SendAsync("ShipmentPickedUp", shipmentId, orderId);
            
            var customerId = await GetCustomerIdFromOrderId(orderId);
            var shipperId = await GetShipperIdFromShipmentId(shipmentId);
            
            await Clients.Group($"User_{customerId}").SendAsync("YourShipmentStatusChanged", orderId, "Picked Up", "Your order has been picked up by the shipper");
            await Clients.Group($"Shipper_{shipperId}").SendAsync("PickupConfirmed", shipmentId, orderId);
        }

        public async Task NotifyShipmentInTransit(int shipmentId, int orderId)
        {
            await Clients.All.SendAsync("ShipmentInTransit", shipmentId, orderId);
            
            var customerId = await GetCustomerIdFromOrderId(orderId);
            var shipperId = await GetShipperIdFromShipmentId(shipmentId);
            
            await Clients.Group($"User_{customerId}").SendAsync("YourShipmentStatusChanged", orderId, "In Transit", "Your order is now in transit");
            await Clients.Group($"Shipper_{shipperId}").SendAsync("TransitConfirmed", shipmentId, orderId);
        }

        public async Task NotifyShipmentOutForDelivery(int shipmentId, int orderId)
        {
            await Clients.All.SendAsync("ShipmentOutForDelivery", shipmentId, orderId);
            
            var customerId = await GetCustomerIdFromOrderId(orderId);
            var shipperId = await GetShipperIdFromShipmentId(shipmentId);
            
            await Clients.Group($"User_{customerId}").SendAsync("YourShipmentStatusChanged", orderId, "Out for Delivery", "Your order is out for delivery and will arrive soon!");
            await Clients.Group($"Shipper_{shipperId}").SendAsync("OutForDeliveryConfirmed", shipmentId, orderId);
        }

        public async Task NotifyShipmentFailedDelivery(int shipmentId, int orderId, string reason)
        {
            await Clients.All.SendAsync("ShipmentFailedDelivery", shipmentId, orderId, reason);
            
            var customerId = await GetCustomerIdFromOrderId(orderId);
            var shipperId = await GetShipperIdFromShipmentId(shipmentId);
            
            await Clients.Group($"User_{customerId}").SendAsync("YourShipmentStatusChanged", orderId, "Failed Delivery", $"Delivery attempt failed: {reason}. The shipper will retry.");
            await Clients.Group($"Shipper_{shipperId}").SendAsync("DeliveryFailed", shipmentId, orderId, reason);
            await Clients.Groups("AdminGroup").SendAsync("DeliveryFailedNotification", shipmentId, orderId, reason);
        }

        #endregion

        #region Shipment Groups Management

        public async Task RegisterShipmentTracking(string trackingNumber, int customerId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Tracking_{trackingNumber}");
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{customerId}");
        }

        public async Task UnregisterShipmentTracking(string trackingNumber, int customerId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Tracking_{trackingNumber}");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{customerId}");
        }

        [Authorize(Roles = "Admin,Shipper")]
        public async Task SendShipmentUpdate(string trackingNumber, string status, string message)
        {
            await Clients.Group($"Tracking_{trackingNumber}").SendAsync("ShipmentTrackingUpdate", trackingNumber, status, message);
        }

        [Authorize(Roles = "Admin")]
        public async Task SendShipmentBroadcast(string message, string type = "info")
        {
            await Clients.Groups("ActiveShippers").SendAsync("ShipmentBroadcast", message, type);
        }

        #endregion

        #region Helper Methods

        private string GetUserIdFromSellerId(int sellerId)
        {
            return sellerId.ToString();
        }

        private string GetUserIdFromShipperId(int shipperId)
        {
            return shipperId.ToString();
        }

        private async Task<int> GetUserIdFromOrderId(int orderId)
        {
            return orderId;
        }

        private async Task<int> GetCustomerIdFromOrderId(int orderId)
        {
            return orderId;
        }

        private async Task<int> GetShipperIdFromShipmentId(int shipmentId)
        {
            return shipmentId;
        }

        private async Task<string> GetShipperNameFromId(int shipperId)
        {
            return "Shipper Name"; // Replace with actual logic to get shipper name
        }

        private async Task<int> GetOrderIdFromShipmentId(int shipmentId)
        {
            return shipmentId;
        }

        #endregion
    }
}