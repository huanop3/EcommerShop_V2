using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Blazored.LocalStorage;
using MainEcommerceService.Models.ViewModel;
using MudBlazor;
using System;
using System.Threading.Tasks;

namespace BlazorWebApp.Services
{
    public class SignalRService : IAsyncDisposable
    {
        private readonly NavigationManager _navigationManager;
        private readonly ILocalStorageService _localStorage;
        private readonly ISnackbar _snackbar;

        // Hai kết nối hub riêng biệt
        private HubConnection? _mainHubConnection; // MainEcommerceService
        private HubConnection? _productHubConnection; // ProductService

        private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);

        // Events cho user management (MainEcommerceService)
        public event Action<string>? UserCreated;
        public event Action<int, string>? UserUpdated;
        public event Action<int>? UserDeleted;
        public event Action<int, string>? UserStatusChanged;
        public event Action<int, string, string>? UserRoleUpdated; // ✅ BỔ SUNG Event thiếu

        // Events cho category management (ProductService)
        public event Action<string>? CategoryCreated;
        public event Action<int, string>? CategoryUpdated;
        public event Action<int, string>? CategoryDeleted;

        // Events cho product management (ProductService)
        public event Action<int, string, string>? ProductCreated;
        public event Action<int, string, decimal>? ProductUpdated;
        public event Action<int, string>? ProductDeleted;
        public event Action<int, string, int>? ProductStockChanged;
        public event Action<int, string, decimal, decimal>? ProductPriceChanged;
        public event Action<int, string, int, int>? LowStockAlert;

        // Events cho coupon management (MainEcommerceService)
        public event Action<string>? CouponCreated;
        public event Action<int, string>? CouponUpdated;
        public event Action<int>? CouponDeleted;
        public event Action<int, string>? CouponStatusChanged;

        // Events cho address management (MainEcommerceService)
        public event Action<int, string>? AddressCreated;
        public event Action<int, string>? AddressUpdated;
        public event Action<int>? AddressDeleted;
        public event Action<int, int>? DefaultAddressChanged;

        // Events cho thông báo
        public event Action<string>? PrivateNotificationReceived;
        public event Action<string, string>? MessageReceived;
        public event Action<string, string>? CategoryNotificationReceived;

        // Events cho kết nối
        public event Action<string>? UserConnected;
        public event Action<string>? UserDisconnected;

        // Seller Profile events (MainEcommerceService)
        public event Action<string>? SellerProfileCreated;
        public event Action<int, string>? SellerProfileUpdated;
        public event Action<int>? SellerProfileDeleted;
        public event Action<int, string>? SellerProfileVerified;
        public event Action<int, string>? SellerProfileUnverified;

        // ✅ BỔ SUNG: Shipper Profile events (MainEcommerceService)
        public event Action<string>? ShipperProfileCreated;
        public event Action<int, string>? ShipperProfileUpdated;
        public event Action<int>? ShipperProfileDeleted;
        public event Action<int>? ShipperProfileActivated;
        public event Action<int>? ShipperProfileDeactivated;

        // ✅ Thêm Events cho Order Management
        public event Action<int, int, decimal>? OrderCreated;
        public event Action<int, int, decimal>? OrderUpdated;
        public event Action<int>? OrderDeleted;
        public event Action<int, int, int, string>? OrderStatusChanged;
        public event Action<int, int, int>? OrderAssignedToShipper; // ✅ BỔ SUNG Event thiếu

        // ✅ Thêm Events cho OrderItem Management
        public event Action<int, int, int, int>? OrderItemCreated;
        public event Action<int, int, int, int>? OrderItemUpdated;
        public event Action<int, int>? OrderItemDeleted;

        // ✅ Thêm Events cho OrderStatus Management
        public event Action<int, string>? OrderStatusCreated;
        public event Action<int, string>? OrderStatusUpdated;
        public event Action<int>? OrderStatusDeleted;
        // 🔥 THÊM: New events for Kafka-based order processing
        public event Action<int>? OrderConfirmed;
        public event Action<int, string>? OrderCancelled;
        public event Action<int, string>? YourOrderConfirmed;
        public event Action<int, string, string>? YourOrderCancelled;

        // ✅ Thêm Events cho Payment Management
        public event Action<int, int, decimal>? PaymentCreated;
        public event Action<int, int, decimal>? PaymentUpdated;
        public event Action<int>? PaymentDeleted;
        public event Action<int, int, string>? PaymentStatusChanged;

        // ✅ Thêm Events cho Private User Notifications
        public event Action<int, decimal, string>? YourOrderCreated;
        public event Action<int, decimal>? YourOrderUpdated;
        public event Action<int, string, string>? YourOrderStatusChanged;
        public event Action<int, string>? YourPaymentStatusChanged;
        public event Action<int>? YourOrderAssignedToShipper; // ✅ BỔ SUNG Event thiếu

        // ✅ BỔ SUNG: Shipment events (MainEcommerceService)
        public event Action<int, int, int>? ShipmentCreated;
        public event Action<int, int, string>? ShipmentUpdated;
        public event Action<int>? ShipmentDeleted;
        public event Action<int, int, string>? ShipmentStatusUpdated;
        public event Action<int, int>? ShipperAssigned;
        public event Action<int, string>? TrackingNumberUpdated;
        public event Action<int, int>? ShipmentDelivered;
        public event Action<int, int>? ShipmentPickedUp;
        public event Action<int, int>? ShipmentInTransit;
        public event Action<int, int>? ShipmentOutForDelivery;
        public event Action<int, int, string>? ShipmentFailedDelivery;

        // ✅ BỔ SUNG: Private Shipment Notifications
        public event Action<int, string, string>? YourShipmentStatusChanged;
        public event Action<int, string>? YourShipmentAssigned;
        public event Action<int, string>? YourShipmentDelivered;
        public event Action<int, string>? NewDeliveryAssignment;

        public bool IsMainHubConnected => _mainHubConnection?.State == HubConnectionState.Connected;
        public bool IsProductHubConnected => _productHubConnection?.State == HubConnectionState.Connected;

        public SignalRService(NavigationManager navigationManager,
                             ILocalStorageService localStorage,
                             ISnackbar snackbar)
        {
            _navigationManager = navigationManager;
            _localStorage = localStorage;
            _snackbar = snackbar;
        }

        public async Task StartConnectionAsync()
        {
            await _connectionSemaphore.WaitAsync();
            
            // Khởi tạo kết nối đến MainEcommerceService
            await StartMainHubConnectionAsync();

            // Khởi tạo kết nối đến ProductService
            await StartProductHubConnectionAsync();

#if DEBUG
            _snackbar.Add("Connected to notification systems", Severity.Success);
#endif

            // Đăng ký user connection
            string userId = await GetCurrentUserIdAsync();
            if (!string.IsNullOrEmpty(userId))
            {
                await RegisterUserConnectionAsync(userId);
            }
            
            _connectionSemaphore.Release();
        }

        private async Task StartMainHubConnectionAsync()
        {
            if (_mainHubConnection != null && IsMainHubConnected)
                return;

            string mainHubUrl = "http://gateway_service:8080/main/notificationHub";

            _mainHubConnection = new HubConnectionBuilder()
                .WithUrl(mainHubUrl, options =>
                {
                    options.AccessTokenProvider = async () =>
                    {
                        var token = await _localStorage.GetItemAsStringAsync("token");
                        return token;
                    };
                    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
                    options.SkipNegotiation = true;
                })
                .Build();

            RegisterMainHubEventHandlers();
            await _mainHubConnection.StartAsync();
        }

        private async Task StartProductHubConnectionAsync()
        {
            if (_productHubConnection != null && IsProductHubConnected)
                return;

            string productHubUrl = "http://gateway_service:8080/product/notificationHub"; // Sửa từ 7262 thành 7252

            _productHubConnection = new HubConnectionBuilder()
                .WithUrl(productHubUrl, options =>
                {
                    options.AccessTokenProvider = async () =>
                    {
                        var token = await _localStorage.GetItemAsStringAsync("token");
                        return token;
                    };
                    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
                    options.SkipNegotiation = true;
                })
                .Build();

            RegisterProductHubEventHandlers();
            await _productHubConnection.StartAsync();
        }

        private void RegisterMainHubEventHandlers()
        {
            if (_mainHubConnection == null) return;

            // User management events
            _mainHubConnection.On<string>("UserCreated", async (name) =>
            {
                await Task.Run(() => UserCreated?.Invoke(name));
            });

            _mainHubConnection.On<int, string>("UserUpdated", async (userId, name) =>
            {
                await Task.Run(() => UserUpdated?.Invoke(userId, name));
            });

            _mainHubConnection.On<int>("UserDeleted", async (userId) =>
            {
                await Task.Run(() => UserDeleted?.Invoke(userId));
            });

            _mainHubConnection.On<int, string>("UserStatusChanged", async (userId, status) =>
            {
                await Task.Run(() => UserStatusChanged?.Invoke(userId, status));
            });

            // ✅ BỔ SUNG: User Role Updated Event Handler
            _mainHubConnection.On<int, string, string>("UserRoleUpdated", async (userId, username, newRole) =>
            {
                await Task.Run(() => UserRoleUpdated?.Invoke(userId, username, newRole));
            });

            // Notification events
            _mainHubConnection.On<string>("PrivateNotification", async (message) =>
            {
                await Task.Run(() => PrivateNotificationReceived?.Invoke(message));
            });

            _mainHubConnection.On<string, string>("ReceiveMessage", async (user, message) =>
            {
                await Task.Run(() => MessageReceived?.Invoke(user, message));
            });

            // Connection events
            _mainHubConnection.On<string>("UserConnected", async (connectionId) =>
            {
                await Task.Run(() => UserConnected?.Invoke(connectionId));
            });

            _mainHubConnection.On<string>("UserDisconnected", async (connectionId) =>
            {
                await Task.Run(() => UserDisconnected?.Invoke(connectionId));
            });

            // Coupon management events
            _mainHubConnection.On<string>("CouponCreated", async (couponCode) =>
            {
                await Task.Run(() => CouponCreated?.Invoke(couponCode));
            });

            _mainHubConnection.On<int, string>("CouponUpdated", async (couponId, couponCode) =>
            {
                await Task.Run(() => CouponUpdated?.Invoke(couponId, couponCode));
            });

            _mainHubConnection.On<int>("CouponDeleted", async (couponId) =>
            {
                await Task.Run(() => CouponDeleted?.Invoke(couponId));
            });

            _mainHubConnection.On<int, string>("CouponStatusChanged", async (couponId, status) =>
            {
                await Task.Run(() => CouponStatusChanged?.Invoke(couponId, status));
            });

            // Address management events
            _mainHubConnection.On<int, string>("AddressCreated", async (userId, addressInfo) =>
            {
                await Task.Run(() => AddressCreated?.Invoke(userId, addressInfo));
            });

            _mainHubConnection.On<int, string>("AddressUpdated", async (userId, addressInfo) =>
            {
                await Task.Run(() => AddressUpdated?.Invoke(userId, addressInfo));
            });

            _mainHubConnection.On<int>("AddressDeleted", async (addressId) =>
            {
                await Task.Run(() => AddressDeleted?.Invoke(addressId));
            });

            _mainHubConnection.On<int, int>("DefaultAddressChanged", async (userId, addressId) =>
            {
                await Task.Run(() => DefaultAddressChanged?.Invoke(userId, addressId));
            });

            // Seller Profile management events
            _mainHubConnection.On<string>("SellerProfileCreated", async (storeName) =>
            {
                await Task.Run(() => SellerProfileCreated?.Invoke(storeName));
            });

            _mainHubConnection.On<int, string>("SellerProfileUpdated", async (sellerId, storeName) =>
            {
                await Task.Run(() => SellerProfileUpdated?.Invoke(sellerId, storeName));
            });

            _mainHubConnection.On<int>("SellerProfileDeleted", async (sellerId) =>
            {
                await Task.Run(() => SellerProfileDeleted?.Invoke(sellerId));
            });

            _mainHubConnection.On<int, string>("SellerProfileVerified", async (sellerId, storeName) =>
            {
                await Task.Run(() => SellerProfileVerified?.Invoke(sellerId, storeName));
            });

            _mainHubConnection.On<int, string>("SellerProfileUnverified", async (sellerId, storeName) =>
            {
                await Task.Run(() => SellerProfileUnverified?.Invoke(sellerId, storeName));
            });

            // ✅ BỔ SUNG: Shipper Profile management events
            _mainHubConnection.On<string>("ShipperProfileCreated", async (shipperName) =>
            {
                await Task.Run(() => ShipperProfileCreated?.Invoke(shipperName));
            });

            _mainHubConnection.On<int, string>("ShipperProfileUpdated", async (shipperId, shipperName) =>
            {
                await Task.Run(() => ShipperProfileUpdated?.Invoke(shipperId, shipperName));
            });

            _mainHubConnection.On<int>("ShipperProfileDeleted", async (shipperId) =>
            {
                await Task.Run(() => ShipperProfileDeleted?.Invoke(shipperId));
            });

            _mainHubConnection.On<int>("ShipperProfileActivated", async (shipperId) =>
            {
                await Task.Run(() => ShipperProfileActivated?.Invoke(shipperId));
            });

            _mainHubConnection.On<int>("ShipperProfileDeactivated", async (shipperId) =>
            {
                await Task.Run(() => ShipperProfileDeactivated?.Invoke(shipperId));
            });

            // ✅ Order Management Events
            _mainHubConnection.On<int, int, decimal>("OrderCreated", async (orderId, userId, totalAmount) =>
            {
                await Task.Run(() => OrderCreated?.Invoke(orderId, userId, totalAmount));
            });

            _mainHubConnection.On<int, int, decimal>("OrderUpdated", async (orderId, userId, totalAmount) =>
            {
                await Task.Run(() => OrderUpdated?.Invoke(orderId, userId, totalAmount));
            });

            _mainHubConnection.On<int>("OrderDeleted", async (orderId) =>
            {
                await Task.Run(() => OrderDeleted?.Invoke(orderId));
            });

            _mainHubConnection.On<int, int, int, string>("OrderStatusChanged", async (orderId, userId, statusId, statusName) =>
            {
                await Task.Run(() => OrderStatusChanged?.Invoke(orderId, userId, statusId, statusName));
            });

            // ✅ BỔ SUNG: Order Assigned to Shipper Event
            _mainHubConnection.On<int, int, int>("OrderAssignedToShipper", async (orderId, shipperId, customerId) =>
            {
                await Task.Run(() => OrderAssignedToShipper?.Invoke(orderId, shipperId, customerId));
            });

            // ✅ OrderItem Management Events
            _mainHubConnection.On<int, int, int, int>("OrderItemCreated", async (orderItemId, orderId, productId, quantity) =>
            {
                await Task.Run(() => OrderItemCreated?.Invoke(orderItemId, orderId, productId, quantity));
            });

            _mainHubConnection.On<int, int, int, int>("OrderItemUpdated", async (orderItemId, orderId, productId, quantity) =>
            {
                await Task.Run(() => OrderItemUpdated?.Invoke(orderItemId, orderId, productId, quantity));
            });

            _mainHubConnection.On<int, int>("OrderItemDeleted", async (orderItemId, orderId) =>
            {
                await Task.Run(() => OrderItemDeleted?.Invoke(orderItemId, orderId));
            });
            // 🔥 THÊM: Kafka-based order events
            _mainHubConnection.On<int>("OrderConfirmed", (orderId) =>
            {
                Console.WriteLine($"🔔 SignalR: Order {orderId} confirmed");
                OrderConfirmed?.Invoke(orderId);
            });

            _mainHubConnection.On<int, string>("OrderCancelled", (orderId, reason) =>
            {
                Console.WriteLine($"🔔 SignalR: Order {orderId} cancelled: {reason}");
                OrderCancelled?.Invoke(orderId, reason);
            });

            // ✅ BỔ SUNG: Your Order Confirmed Event
            _mainHubConnection.On<int, string>("YourOrderConfirmed", async (orderId, message) =>
            {
                await Task.Run(() =>
                {
                    YourOrderConfirmed?.Invoke(orderId, message);
                    _snackbar.Add($"Order #{orderId} confirmed: {message}", Severity.Success);
                });
            });

            // ✅ BỔ SUNG: Your Order Cancelled Event
            _mainHubConnection.On<int, string, string>("YourOrderCancelled", async (orderId, reason, message) =>
            {
                await Task.Run(() =>
                {
                    YourOrderCancelled?.Invoke(orderId, reason, message);
                    _snackbar.Add($"Order #{orderId} cancelled: {reason}", Severity.Warning);
                });
            });

            // ✅ BỔ SUNG: Your Order Assigned to Shipper Event
            _mainHubConnection.On<int>("YourOrderAssignedToShipper", async (orderId) =>
            {
                await Task.Run(() =>
                {
                    YourOrderAssignedToShipper?.Invoke(orderId);
                    _snackbar.Add($"Order #{orderId} has been assigned to a shipper!", Severity.Info);
                });
            });

            // ✅ OrderStatus Management Events
            _mainHubConnection.On<int, string>("OrderStatusCreated", async (statusId, statusName) =>
            {
                await Task.Run(() => OrderStatusCreated?.Invoke(statusId, statusName));
            });

            _mainHubConnection.On<int, string>("OrderStatusUpdated", async (statusId, statusName) =>
            {
                await Task.Run(() => OrderStatusUpdated?.Invoke(statusId, statusName));
            });

            _mainHubConnection.On<int>("OrderStatusDeleted", async (statusId) =>
            {
                await Task.Run(() => OrderStatusDeleted?.Invoke(statusId));
            });

            // ✅ Payment Management Events
            _mainHubConnection.On<int, int, decimal>("PaymentCreated", async (paymentId, orderId, amount) =>
            {
                await Task.Run(() => PaymentCreated?.Invoke(paymentId, orderId, amount));
            });

            _mainHubConnection.On<int, int, decimal>("PaymentUpdated", async (paymentId, orderId, amount) =>
            {
                await Task.Run(() => PaymentUpdated?.Invoke(paymentId, orderId, amount));
            });

            _mainHubConnection.On<int>("PaymentDeleted", async (paymentId) =>
            {
                await Task.Run(() => PaymentDeleted?.Invoke(paymentId));
            });

            _mainHubConnection.On<int, int, string>("PaymentStatusChanged", async (paymentId, orderId, status) =>
            {
                await Task.Run(() => PaymentStatusChanged?.Invoke(paymentId, orderId, status));
            });

            // ✅ Private User Order Notifications
            _mainHubConnection.On<int, decimal, string>("YourOrderCreated", async (orderId, totalAmount, message) =>
            {
                await Task.Run(() =>
                {
                    YourOrderCreated?.Invoke(orderId, totalAmount, message);
                    _snackbar.Add($"Order #{orderId} created successfully! Total: ${totalAmount:F2}", Severity.Success);
                });
            });

            _mainHubConnection.On<int, decimal>("YourOrderUpdated", async (orderId, totalAmount) =>
            {
                await Task.Run(() =>
                {
                    YourOrderUpdated?.Invoke(orderId, totalAmount);
                    _snackbar.Add($"Order #{orderId} has been updated", Severity.Info);
                });
            });

            _mainHubConnection.On<int, string, string>("YourOrderStatusChanged", async (orderId, statusName, message) =>
            {
                await Task.Run(() =>
                {
                    YourOrderStatusChanged?.Invoke(orderId, statusName, message);
                    _snackbar.Add($"Order #{orderId}: {message}", Severity.Info);
                });
            });

            _mainHubConnection.On<int, string>("YourPaymentStatusChanged", async (paymentId, status) =>
            {
                await Task.Run(() =>
                {
                    YourPaymentStatusChanged?.Invoke(paymentId, status);
                    _snackbar.Add($"Payment status updated: {status}", Severity.Info);
                });
            });

            // ✅ BỔ SUNG: Shipment events (MainEcommerceService)
            _mainHubConnection.On<int, int, int>("ShipmentCreated", async (shipmentId, orderId, userId) =>
            {
                await Task.Run(() => ShipmentCreated?.Invoke(shipmentId, orderId, userId));
            });

            _mainHubConnection.On<int, int, string>("ShipmentUpdated", async (shipmentId, orderId, status) =>
            {
                await Task.Run(() => ShipmentUpdated?.Invoke(shipmentId, orderId, status));
            });

            _mainHubConnection.On<int>("ShipmentDeleted", async (shipmentId) =>
            {
                await Task.Run(() => ShipmentDeleted?.Invoke(shipmentId));
            });

            _mainHubConnection.On<int, int, string>("ShipmentStatusUpdated", async (shipmentId, orderId, status) =>
            {
                await Task.Run(() => ShipmentStatusUpdated?.Invoke(shipmentId, orderId, status));
            });

            _mainHubConnection.On<int, int>("ShipperAssigned", async (shipmentId, shipperId) =>
            {
                await Task.Run(() => ShipperAssigned?.Invoke(shipmentId, shipperId));
            });

            _mainHubConnection.On<int, string>("TrackingNumberUpdated", async (shipmentId, trackingNumber) =>
            {
                await Task.Run(() => TrackingNumberUpdated?.Invoke(shipmentId, trackingNumber));
            });

            _mainHubConnection.On<int, int>("ShipmentDelivered", async (shipmentId, orderId) =>
            {
                await Task.Run(() => ShipmentDelivered?.Invoke(shipmentId, orderId));
            });

            _mainHubConnection.On<int, int>("ShipmentPickedUp", async (shipmentId, orderId) =>
            {
                await Task.Run(() => ShipmentPickedUp?.Invoke(shipmentId, orderId));
            });

            _mainHubConnection.On<int, int>("ShipmentInTransit", async (shipmentId, orderId) =>
            {
                await Task.Run(() => ShipmentInTransit?.Invoke(shipmentId, orderId));
            });

            _mainHubConnection.On<int, int>("ShipmentOutForDelivery", async (shipmentId, orderId) =>
            {
                await Task.Run(() => ShipmentOutForDelivery?.Invoke(shipmentId, orderId));
            });

            _mainHubConnection.On<int, int, string>("ShipmentFailedDelivery", async (shipmentId, orderId, reason) =>
            {
                await Task.Run(() => ShipmentFailedDelivery?.Invoke(shipmentId, orderId, reason));
            });

            // ✅ BỔ SUNG: Private Shipment Notifications
            _mainHubConnection.On<int, string, string>("YourShipmentStatusChanged", async (shipmentId, status, message) =>
            {
                await Task.Run(() =>
                {
                    YourShipmentStatusChanged?.Invoke(shipmentId, status, message);
                    _snackbar.Add($"Shipment #{shipmentId} status changed to {status}: {message}", Severity.Info);
                });
            });

            _mainHubConnection.On<int, string>("YourShipmentAssigned", async (shipmentId, shipperName) =>
            {
                await Task.Run(() =>
                {
                    YourShipmentAssigned?.Invoke(shipmentId, shipperName);
                    _snackbar.Add($"Shipment #{shipmentId} has been assigned to {shipperName}", Severity.Info);
                });
            });

            _mainHubConnection.On<int, string>("YourShipmentDelivered", async (shipmentId, deliveryInfo) =>
            {
                await Task.Run(() =>
                {
                    YourShipmentDelivered?.Invoke(shipmentId, deliveryInfo);
                    _snackbar.Add($"Shipment #{shipmentId} delivered: {deliveryInfo}", Severity.Success);
                });
            });

            // Register other events as needed
        }

        private void RegisterProductHubEventHandlers()
        {
            if (_productHubConnection == null) return;

            // Product management events
            _productHubConnection.On<int, string, string>("ProductCreated", async (productId, productName, categoryName) =>
            {
                await Task.Run(() => ProductCreated?.Invoke(productId, productName, categoryName));
            });

            _productHubConnection.On<int, string, decimal>("ProductUpdated", async (productId, productName, price) =>
            {
                await Task.Run(() => ProductUpdated?.Invoke(productId, productName, price));
            });

            _productHubConnection.On<int, string>("ProductDeleted", async (productId, productName) =>
            {
                await Task.Run(() => ProductDeleted?.Invoke(productId, productName));
            });

            _productHubConnection.On<int, string, int>("ProductStockChanged", async (productId, productName, newStock) =>
            {
                await Task.Run(() => ProductStockChanged?.Invoke(productId, productName, newStock));
            });

            _productHubConnection.On<int, string, decimal, decimal>("ProductPriceChanged", async (productId, productName, oldPrice, newPrice) =>
            {
                await Task.Run(() => ProductPriceChanged?.Invoke(productId, productName, oldPrice, newPrice));
            });

            _productHubConnection.On<int, string, int, int>("LowStockAlert", async (productId, productName, currentStock, minStock) =>
            {
                await Task.Run(() => LowStockAlert?.Invoke(productId, productName, currentStock, minStock));
            });

            // Category management events
            _productHubConnection.On<string>("CategoryCreated", async (categoryName) =>
            {
                await Task.Run(() => CategoryCreated?.Invoke(categoryName));
            });

            _productHubConnection.On<int, string>("CategoryUpdated", async (categoryId, categoryName) =>
            {
                await Task.Run(() => CategoryUpdated?.Invoke(categoryId, categoryName));
            });

            _productHubConnection.On<int, string>("CategoryDeleted", async (categoryId, categoryName) =>
            {
                await Task.Run(() => CategoryDeleted?.Invoke(categoryId, categoryName));
            });

            // Category notification events
            _productHubConnection.On<string, string>("CategoryNotification", async (categoryId, message) =>
            {
                await Task.Run(() => CategoryNotificationReceived?.Invoke(categoryId, message));
            });
        }

        private void RegisterEventHandlers()
        {
            // Phương thức này giờ không dùng nữa, đã tách thành 2 phương thức riêng
        }

        private async Task<string> GetCurrentUserIdAsync()
        {
            var userId = await _localStorage.GetItemAsStringAsync("userId");
            return userId ?? string.Empty;
        }

        // User connection methods (sử dụng cả 2 hub)
        public async Task RegisterUserConnectionAsync(string userId)
        {
            var tasks = new List<Task>();

            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                tasks.Add(_mainHubConnection.SendAsync("RegisterUserConnection", userId));
            }

            if (_productHubConnection is not null && IsProductHubConnected)
            {
                tasks.Add(_productHubConnection.SendAsync("RegisterUserConnection", userId));
            }

            if (tasks.Any())
            {
                await Task.WhenAll(tasks);
            }
        }

        // ✅ BỔ SUNG: Register Seller Connection
        public async Task RegisterSellerConnectionAsync(int sellerId, int userId)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("RegisterSellerConnection", sellerId, userId);
            }
        }

        // ✅ BỔ SUNG: Unregister Seller Connection
        public async Task UnregisterSellerConnectionAsync(int sellerId, int userId)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("UnregisterSellerConnection", sellerId, userId);
            }
        }

        // ✅ BỔ SUNG: Register Shipper Connection
        public async Task RegisterShipperConnectionAsync(int shipperId, int userId)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("RegisterShipperConnection", shipperId, userId);
            }
        }

        // ✅ BỔ SUNG: Unregister Shipper Connection
        public async Task UnregisterShipperConnectionAsync(int shipperId, int userId)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("UnregisterShipperConnection", shipperId, userId);
            }
        }

        // Main Hub methods (MainEcommerceService)
        public async Task SendMessageAsync(string user, string message)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("SendMessage", user, message);
            }
        }

        // ✅ BỔ SUNG: Send Private Message
        public async Task SendPrivateMessageAsync(string targetUserId, string message)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("SendPrivateMessage", targetUserId, message);
            }
        }

        //Order notification methods (MainEcommerceService)
        public async Task NotifyOrderCreatedAsync(int orderId, int userId, decimal totalAmount)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyOrderCreated", orderId, userId, totalAmount);
            }
        }
        public async Task NotifyOrderUpdatedAsync(int orderId, int userId, decimal totalAmount)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyOrderUpdated", orderId, userId, totalAmount);
            }
        }

        // ✅ BỔ SUNG: Notify Order Deleted
        public async Task NotifyOrderDeletedAsync(int orderId)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyOrderDeleted", orderId);
            }
        }

        // ✅ BỔ SUNG: Notify Order Status Changed
        public async Task NotifyOrderStatusChangedAsync(int orderId, int customerId, int statusId, string statusName)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyOrderStatusChanged", orderId, customerId, statusId, statusName);
            }
        }

        // ✅ BỔ SUNG: Notify Order Assigned to Shipper
        public async Task NotifyOrderAssignedToShipperAsync(int orderId, int shipperId, int customerId)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyOrderAssignedToShipper", orderId, shipperId, customerId);
            }
        }

        // User notification methods (MainEcommerceService)
        public async Task NotifyUserCreatedAsync(string username)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyUserCreated", username);
            }
        }

        public async Task NotifyUserUpdatedAsync(int userId, string name)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyUserUpdated", userId, name);
            }
        }

        public async Task NotifyUserDeletedAsync(int userId)
        {
            if (_mainHubConnection != null && _mainHubConnection.State == HubConnectionState.Connected)
            {
                await _mainHubConnection.SendAsync("NotifyUserDeleted", userId);
            }
        }

        public async Task NotifyUserStatusChangedAsync(int userId, string status)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyUserStatusChanged", userId, status);
            }
        }

        // Product notification methods (ProductService)
        public async Task NotifyProductCreatedAsync(int productId, string productName, string categoryName)
        {
            if (_productHubConnection is not null && IsProductHubConnected)
            {
                await _productHubConnection.SendAsync("NotifyProductCreated", productId, productName, categoryName);
            }
        }

        public async Task NotifyProductUpdatedAsync(int productId, string productName, decimal price)
        {
            if (_productHubConnection is not null && IsProductHubConnected)
            {
                await _productHubConnection.SendAsync("NotifyProductUpdated", productId, productName, price);
            }
        }

        public async Task NotifyProductDeletedAsync(int productId, string productName)
        {
            if (_productHubConnection is not null && IsProductHubConnected)
            {
                await _productHubConnection.SendAsync("NotifyProductDeleted", productId, productName);
            }
        }

        public async Task NotifyProductStockChangedAsync(int productId, string productName, int newStock)
        {
            if (_productHubConnection is not null && IsProductHubConnected)
            {
                await _productHubConnection.SendAsync("NotifyProductStockChanged", productId, productName, newStock);
            }
        }

        public async Task NotifyProductPriceChangedAsync(int productId, string productName, decimal oldPrice, decimal newPrice)
        {
            if (_productHubConnection is not null && IsProductHubConnected)
            {
                await _productHubConnection.SendAsync("NotifyProductPriceChanged", productId, productName, oldPrice, newPrice);
            }
        }

        public async Task NotifyLowStockAsync(int productId, string productName, int currentStock, int minStock)
        {
            if (_productHubConnection is not null && IsProductHubConnected)
            {
                await _productHubConnection.SendAsync("NotifyLowStock", productId, productName, currentStock, minStock);
            }
        }

        // Category notification methods (ProductService)
        public async Task NotifyCategoryCreatedAsync(string categoryName)
        {
            if (_productHubConnection is not null && IsProductHubConnected)
            {
                await _productHubConnection.SendAsync("NotifyCategoryCreated", categoryName);
            }
        }

        public async Task NotifyCategoryUpdatedAsync(int categoryId, string categoryName)
        {
            if (_productHubConnection is not null && IsProductHubConnected)
            {
                await _productHubConnection.SendAsync("NotifyCategoryUpdated", categoryId, categoryName);
            }
        }

        public async Task NotifyCategoryDeletedAsync(int categoryId, string categoryName)
        {
            if (_productHubConnection is not null && IsProductHubConnected)
            {
                await _productHubConnection.SendAsync("NotifyCategoryDeleted", categoryId, categoryName);
            }
        }

        // Category group methods (ProductService)
        public async Task JoinCategoryGroupAsync(string categoryId)
        {
            if (_productHubConnection is not null && IsProductHubConnected)
            {
                await _productHubConnection.SendAsync("JoinCategoryGroup", categoryId);
            }
        }

        public async Task LeaveCategoryGroupAsync(string categoryId)
        {
            if (_productHubConnection is not null && IsProductHubConnected)
            {
                await _productHubConnection.SendAsync("LeaveCategoryGroup", categoryId);
            }
        }

        public async Task SendCategoryNotificationAsync(string categoryId, string message)
        {
            if (_productHubConnection is not null && IsProductHubConnected)
            {
                await _productHubConnection.SendAsync("SendCategoryNotification", categoryId, message);
            }
        }

        // Coupon notification methods (MainEcommerceService)
        public async Task NotifyCouponCreatedAsync(string couponCode)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyCouponCreated", couponCode);
            }
        }

        public async Task NotifyCouponUpdatedAsync(int couponId, string couponCode)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyCouponUpdated", couponId, couponCode);
            }
        }

        public async Task NotifyCouponDeletedAsync(int couponId)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyCouponDeleted", couponId);
            }
        }

        public async Task NotifyCouponStatusChangedAsync(int couponId, string status)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyCouponStatusChanged", couponId, status);
            }
        }

        // Address notification methods (MainEcommerceService)
        public async Task NotifyAddressCreatedAsync(int userId, string addressInfo)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyAddressCreated", userId, addressInfo);
            }
        }

        public async Task NotifyAddressUpdatedAsync(int userId, string addressInfo)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyAddressUpdated", userId, addressInfo);
            }
        }

        public async Task NotifyAddressDeletedAsync(int addressId)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyAddressDeleted", addressId);
            }
        }

        public async Task NotifyDefaultAddressChangedAsync(int userId, int addressId)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyDefaultAddressChanged", userId, addressId);
            }
        }

        // Seller Profile notification methods (MainEcommerceService)
        public async Task NotifySellerProfileCreatedAsync(string storeName)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifySellerProfileCreated", storeName);
            }
        }

        public async Task NotifySellerProfileUpdatedAsync(int sellerId, string storeName)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifySellerProfileUpdated", sellerId, storeName);
            }
        }

        public async Task NotifySellerProfileDeletedAsync(int sellerId)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifySellerProfileDeleted", sellerId);
            }
        }

        public async Task NotifySellerProfileVerifiedAsync(int sellerId, string storeName)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifySellerProfileVerified", sellerId, storeName);
            }
        }

        public async Task NotifySellerProfileUnverifiedAsync(int sellerId, string storeName)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifySellerProfileUnverified", sellerId, storeName);
            }
        }

        // ✅ BỔ SUNG: Shipper Profile notification methods (MainEcommerceService)
        public async Task NotifyShipperProfileCreatedAsync(string shipperName)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyShipperProfileCreated", shipperName);
            }
        }

        public async Task NotifyShipperProfileUpdatedAsync(int shipperId, string shipperName)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyShipperProfileUpdated", shipperId, shipperName);
            }
        }

        public async Task NotifyShipperProfileDeletedAsync(int shipperId)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyShipperProfileDeleted", shipperId);
            }
        }

        public async Task NotifyShipperProfileActivatedAsync(int shipperId)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyShipperProfileActivated", shipperId);
            }
        }

        public async Task NotifyShipperProfileDeactivatedAsync(int shipperId)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyShipperProfileDeactivated", shipperId);
            }
        }

        public async Task NotifyUserRoleUpdatedAsync(int userId, string username, string newRole)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyUserRoleUpdated", userId, username, newRole);
            }
        }
        // Thêm vào cuối file (trước DisposeAsync method)

        // ✅ BỔ SUNG: Shipment notification methods (MainEcommerceService)
        public async Task NotifyShipmentCreatedAsync(int shipmentId, int orderId, int shipperId)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyShipmentCreated", shipmentId, orderId, shipperId);
            }
        }

        public async Task NotifyShipmentUpdatedAsync(int shipmentId, int orderId, string status)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyShipmentUpdated", shipmentId, orderId, status);
            }
        }

        public async Task NotifyShipmentDeletedAsync(int shipmentId)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyShipmentDeleted", shipmentId);
            }
        }

        public async Task NotifyShipmentStatusUpdatedAsync(int shipmentId, int orderId, string status)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyShipmentStatusUpdated", shipmentId, orderId, status);
            }
        }

        public async Task NotifyShipperAssignedAsync(int orderId, int shipperId)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyShipperAssigned", orderId, shipperId);
            }
        }

        public async Task NotifyTrackingNumberUpdatedAsync(int shipmentId, string trackingNumber)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyTrackingNumberUpdated", shipmentId, trackingNumber);
            }
        }

        public async Task NotifyShipmentDeliveredAsync(int shipmentId, int orderId)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyShipmentDelivered", shipmentId, orderId);
            }
        }

        public async Task NotifyShipmentPickedUpAsync(int shipmentId, int orderId)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyShipmentPickedUp", shipmentId, orderId);
            }
        }

        public async Task NotifyShipmentInTransitAsync(int shipmentId, int orderId)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyShipmentInTransit", shipmentId, orderId);
            }
        }

        public async Task NotifyShipmentOutForDeliveryAsync(int shipmentId, int orderId)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyShipmentOutForDelivery", shipmentId, orderId);
            }
        }

        public async Task NotifyShipmentFailedDeliveryAsync(int shipmentId, int orderId, string reason)
        {
            if (_mainHubConnection is not null && IsMainHubConnected)
            {
                await _mainHubConnection.SendAsync("NotifyShipmentFailedDelivery", shipmentId, orderId, reason);
            }
        }
        public async ValueTask DisposeAsync()
        {
            _connectionSemaphore?.Dispose();

            var disposeTasks = new List<Task>();

            if (_mainHubConnection is not null)
            {
                disposeTasks.Add(Task.Run(async () =>
                {
                    await _mainHubConnection.StopAsync();
                    await _mainHubConnection.DisposeAsync();
                }));
            }

            if (_productHubConnection is not null)
            {
                disposeTasks.Add(Task.Run(async () =>
                {
                    await _productHubConnection.StopAsync();
                    await _productHubConnection.DisposeAsync();
                }));
            }

            if (disposeTasks.Any())
            {
                await Task.WhenAll(disposeTasks);
            }
        }
    } 
}