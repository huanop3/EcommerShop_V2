using BlazorWebApp.ViewModel;
using Blazored.LocalStorage;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BlazorWebApp.Services
{
    public class ShipmentService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        private const string BaseUrl = "main/api/Shipment";

        public ShipmentService(HttpClient httpClient, ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
        }

        private async Task SetAuthorizationHeader()
        {
            var token = await _localStorage.GetItemAsStringAsync("token");
            if (string.IsNullOrEmpty(token))
            {
                // Nếu không có token, thử refresh
                token = await _localStorage.GetItemAsStringAsync("refreshToken");
            }

            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        }

        #region Dashboard & Data Retrieval

        /// <summary>
        /// Lấy thông tin dashboard shipment theo OrderId
        /// </summary>
        public async Task<ShipmentDashboardVM?> GetShipmentDashboardByOrderIdAsync(int orderId)
        {
            if (orderId <= 0) return null;

            await SetAuthorizationHeader();
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/order/{orderId}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<ShipmentDashboardVM>>();
                    
                    if (result?.Success == true && result.Data != null)
                    {
                        return result.Data;
                    }
                }
                
                Console.WriteLine($"Error getting shipment dashboard: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting shipment dashboard: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Lấy danh sách đơn hàng được assign cho shipper
        /// </summary>
        public async Task<List<AssignedOrderVM>> GetAssignedOrdersAsync(int shipperId)
        {
            if (shipperId <= 0) return new List<AssignedOrderVM>();

            await SetAuthorizationHeader();
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/shipper/{shipperId}/orders");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<List<AssignedOrderVM>>>();
                    
                    if (result?.Success == true && result.Data != null)
                    {
                        return result.Data;
                    }
                }
                
                Console.WriteLine($"Error getting assigned orders: {response.StatusCode}");
                return new List<AssignedOrderVM>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting assigned orders: {ex.Message}");
                return new List<AssignedOrderVM>();
            }
        }

        /// <summary>
        /// Lấy orders được assign cho shipper hiện tại (từ JWT token)
        /// </summary>
        public async Task<List<AssignedOrderVM>> GetMyAssignedOrdersAsync(int shipperId)
        {
            await SetAuthorizationHeader();
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/my-orders/{shipperId}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<List<AssignedOrderVM>>>();
                    
                    if (result?.Success == true && result.Data != null)
                    {
                        return result.Data;
                    }
                }
                
                Console.WriteLine($"Error getting my assigned orders: {response.StatusCode}");
                return new List<AssignedOrderVM>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting my assigned orders: {ex.Message}");
                return new List<AssignedOrderVM>();
            }
        }

        #endregion

        #region Status Management

        /// <summary>
        /// Cập nhật trạng thái shipment
        /// </summary>
        public async Task<bool> UpdateShipmentStatusAsync(int shipmentId, UpdateShipmentStatusRequest request)
        {
            if (shipmentId <= 0 || request == null) return false;

            await SetAuthorizationHeader();
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{BaseUrl}/{shipmentId}/status", content);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<object>>();
                    return result?.Success == true;
                }
                
                Console.WriteLine($"Error updating shipment status: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating shipment status: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Lấy danh sách status updates có thể thực hiện
        /// </summary>
        public async Task<List<OrderStatusOptionVM>> GetAvailableStatusUpdatesAsync(int currentStatusId)
        {
            if (currentStatusId <= 0) return new List<OrderStatusOptionVM>();

            await SetAuthorizationHeader();
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/status/{currentStatusId}/available-updates");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<List<OrderStatusOptionVM>>>();
                    
                    if (result?.Success == true && result.Data != null)
                    {
                        return result.Data;
                    }
                }
                
                return new List<OrderStatusOptionVM>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting available status updates: {ex.Message}");
                return new List<OrderStatusOptionVM>();
            }
        }

        #endregion

        #region Admin Functions

        /// <summary>
        /// Assign shipment cho shipper - Admin only (với request object)
        /// </summary>
        public async Task<bool> AssignShipmentActionAsync(AssignShipmentRequest request)
        {
            if (request == null) return false;

            await SetAuthorizationHeader();
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/assign", request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<object>>();
                    return result?.Success == true;
                }
                
                Console.WriteLine($"Error assigning shipment: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error assigning shipment: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Assign shipment cho shipper - Overload với parameters (cho shipper tự assign)
        /// </summary>
        public async Task<bool> AssignShipmentAsync(int orderId, int shipperId)
        {
            var request = new AssignShipmentRequest
            {
                OrderId = orderId,
                ShipperId = shipperId,
                TrackingNumber = "" // Có thể để null nếu không cần tracking number
                
            };

            return await AssignShipmentActionAsync(request);
        }

        #endregion

        #region Validation & Utility

        /// <summary>
        /// Kiểm tra order có thể ship không - Trả về bool đơn giản
        /// </summary>
        public async Task<bool> CanOrderBeShippedAsync(int orderId)
        {
            if (orderId <= 0) return false;

            await SetAuthorizationHeader();
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/order/{orderId}/can-ship");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<bool>>();
                    return result?.Success == true && result.Data == true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking shipping eligibility: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Kiểm tra order có thể ship không - Trả về detailed response
        /// </summary>
        public async Task<ShipmentValidationResponse> CanOrderBeShippedDetailedAsync(int orderId)
        {
            if (orderId <= 0) 
                return new ShipmentValidationResponse { IsValid = false, Message = "Invalid Order ID" };

            await SetAuthorizationHeader();
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/order/{orderId}/can-ship");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<bool>>();
                    
                    if (result?.Success == true)
                    {
                        return new ShipmentValidationResponse { IsValid = result.Data, Message = result.Message };
                    }
                }
                
                return new ShipmentValidationResponse { IsValid = false, Message = "Order cannot be shipped" };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking shipping eligibility: {ex.Message}");
                return new ShipmentValidationResponse { IsValid = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// Health check cho shipment service
        /// </summary>
        public async Task<bool> CheckServiceHealthAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/health");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Service health check failed: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Dashboard Summary (Additional Features)

        /// <summary>
        /// Lấy thống kê tổng quan cho shipper dashboard
        /// </summary>
        public async Task<ShipperDashboardSummaryVM> GetShipperDashboardSummaryAsync(int shipperId)
        {
            try
            {
                var orders = await GetAssignedOrdersAsync(shipperId);
                
                var summary = new ShipperDashboardSummaryVM
                {
                    TotalAssignedOrders = orders.Count,
                    OrdersInProgress = orders.Count(o => new[] { 4, 5, 6 }.Contains(o.OrderStatusId)),
                    OrdersCompleted = orders.Count(o => o.OrderStatusId == 7),
                    OrdersReturned = orders.Count(o => o.OrderStatusId == 9),
                    TotalEarnings = orders.Where(o => o.OrderStatusId == 7).Sum(o => o.TotalAmount * 0.05m), // 5% commission
                };

                // Tính success rate
                var totalProcessed = summary.OrdersCompleted + summary.OrdersReturned;
                summary.SuccessRate = totalProcessed > 0 ? (double)summary.OrdersCompleted / totalProcessed * 100 : 0;

                return summary;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting dashboard summary: {ex.Message}");
                return new ShipperDashboardSummaryVM();
            }
        }

        #endregion
    }

    // HTTP Response wrapper - tương tự như các service khác
    public class HTTPResponseClient<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }
}