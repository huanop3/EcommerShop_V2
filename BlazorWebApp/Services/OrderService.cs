using Blazored.LocalStorage;
using MainEcommerceService.Models.ViewModel;

namespace BlazorWebApp.Services
{
    public class OrderService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;

        public OrderService(HttpClient httpClient, ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
        }

        private async Task SetAuthorizationHeader()
        {
            var token = await _localStorage.GetItemAsStringAsync("token");
            if (string.IsNullOrEmpty(token))
            {
                token = await _localStorage.GetItemAsStringAsync("refreshToken");
            }

            if (!string.IsNullOrEmpty(token))
            {
                token = token.Trim('"');
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        #region Order Management

        /// <summary>
        /// Lấy tất cả đơn hàng - Chỉ Admin
        /// </summary>
        public async Task<IEnumerable<OrderVM>> GetAllOrdersAsync()
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync("main/api/Order/GetAllOrders");

            if (!response.IsSuccessStatusCode)
            {
                return Enumerable.Empty<OrderVM>();
            }

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<OrderVM>>>();

            if (result?.Success == true && result.Data != null)
            {
                return result.Data;
            }

            return Enumerable.Empty<OrderVM>();
        }

        /// <summary>
        /// Lấy đơn hàng theo ID
        /// </summary>
        public async Task<OrderVM> GetOrderByIdAsync(int orderId)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync($"main/api/Order/GetOrderById/{orderId}");

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<OrderVM>>();

            if (result?.Success == true && result.Data != null)
                return result.Data;

            return null;
        }

        /// <summary>
        /// Lấy đơn hàng theo UserId
        /// </summary>
        public async Task<IEnumerable<OrderVM>> GetOrdersByUserIdAsync(int userId)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync($"main/api/Order/GetOrdersByUserId/{userId}");

            if (!response.IsSuccessStatusCode)
            {
                return Enumerable.Empty<OrderVM>();
            }

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<OrderVM>>>();

            if (result?.Success == true && result.Data != null)
            {
                return result.Data;
            }

            return Enumerable.Empty<OrderVM>();
        }

        /// <summary>
        /// Lấy đơn hàng theo trạng thái - Chỉ Admin
        /// </summary>
        public async Task<IEnumerable<OrderVM>> GetOrdersByStatusAsync(int statusId)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync($"main/api/Order/GetOrdersByStatus/{statusId}");

            if (!response.IsSuccessStatusCode)
            {
                return Enumerable.Empty<OrderVM>();
            }

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<OrderVM>>>();

            if (result?.Success == true && result.Data != null)
            {
                return result.Data;
            }

            return Enumerable.Empty<OrderVM>();
        }

        /// <summary>
        /// Lấy đơn hàng theo khoảng thời gian - Chỉ Admin
        /// </summary>
        public async Task<IEnumerable<OrderVM>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync($"main/api/Order/GetOrdersByDateRange?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

            if (!response.IsSuccessStatusCode)
            {
                return Enumerable.Empty<OrderVM>();
            }

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<OrderVM>>>();

            if (result?.Success == true && result.Data != null)
            {
                return result.Data;
            }

            return Enumerable.Empty<OrderVM>();
        }

        /// <summary>
        /// Tạo đơn hàng mới
        /// </summary>
        public async Task<bool> CreateOrderAsync(OrderVM orderVM)
        {
            if (orderVM == null) return false;

            await SetAuthorizationHeader();
            var response = await _httpClient.PostAsJsonAsync("main/api/Order/CreateOrder", orderVM);

            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<string>>();
            return result?.Success ?? false;
        }

        /// <summary>
        /// Tạo đơn hàng với chi tiết
        /// </summary>
        public async Task<(bool Success, string OrderId)> CreateOrderWithItemsAsync(CreateOrderRequest request)
        {
            if (request == null) return (false, null);

            await SetAuthorizationHeader();
            var response = await _httpClient.PostAsJsonAsync("main/api/Order/CreateOrderWithItems", request);

            if (!response.IsSuccessStatusCode) return (false, null);

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<string>>();
            return result?.Success == true ? (true, result.Data) : (false, null);
        }

        /// <summary>
        /// Cập nhật đơn hàng
        /// </summary>
        public async Task<bool> UpdateOrderAsync(OrderVM orderVM)
        {
            if (orderVM == null) return false;

            await SetAuthorizationHeader();
            var response = await _httpClient.PutAsJsonAsync("main/api/Order/UpdateOrder", orderVM);

            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<string>>();
            return result?.Success ?? false;
        }

        /// <summary>
        /// Cập nhật trạng thái đơn hàng - Chỉ Admin
        /// </summary>
        public async Task<bool> UpdateOrderStatusAsync(int orderId, int statusId)
        {
            await SetAuthorizationHeader();
            var request = new { StatusId = statusId };
            var response = await _httpClient.PutAsJsonAsync($"main/api/Order/UpdateOrderStatus/{orderId}", request);

            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<string>>();
            return result?.Success ?? false;
        }

        /// <summary>
        /// Cập nhật trạng thái đơn hàng theo tên - Chỉ Admin
        /// </summary>
        public async Task<bool> UpdateOrderStatusByNameAsync(int orderId, string statusName)
        {
            if (string.IsNullOrEmpty(statusName)) return false;

            await SetAuthorizationHeader();
            var request = new { StatusName = statusName };
            var response = await _httpClient.PutAsJsonAsync($"main/api/Order/UpdateOrderStatusByName/{orderId}", request);

            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<string>>();
            return result?.Success ?? false;
        }

        /// <summary>
        /// Xóa đơn hàng - Chỉ Admin
        /// </summary>
        public async Task<bool> DeleteOrderAsync(int orderId)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.DeleteAsync($"main/api/Order/DeleteOrder/{orderId}");

            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<string>>();
            return result?.Success ?? false;
        }
        /// <summary>
        /// Lấy tên trạng thái đơn hàng theo OrderId - Cải tiến với error handling
        /// </summary>
        public async Task<string> GetOrderStatusNameAsync(int orderId)
        {
            try
            {
                await SetAuthorizationHeader();
                var response = await _httpClient.GetAsync($"main/api/Order/GetOrderStatusNameByOrderId/{orderId}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error getting order status: {response.StatusCode}");
                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<string>>();
                if (result?.Success == true && !string.IsNullOrEmpty(result.Data))
                {
                    Console.WriteLine($"✅ Order {orderId} status: {result.Data}");
                    return result.Data;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception getting order status: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Hủy đơn hàng - Cải tiến với kiểm tra trạng thái
        /// </summary>
        public async Task<(bool Success, string Message)> CancelOrderAsync(int orderId, string reason = null)
        {
            try
            {

                // Kiểm tra trạng thái hiện tại trước khi hủy
                var currentStatus = await GetOrderStatusNameAsync(orderId);
                if (string.IsNullOrEmpty(currentStatus))
                {
                    return (false, "Không thể lấy trạng thái đơn hàng");
                }

                // Chỉ cho phép hủy khi đơn hàng ở trạng thái Pending hoặc Confirmed
                var allowedStatuses = new[] { "Pending", "Confirmed" };
                if (!allowedStatuses.Contains(currentStatus, StringComparer.OrdinalIgnoreCase))
                {
                    return (false, $"Không thể hủy đơn hàng ở trạng thái '{currentStatus}'. Chỉ có thể hủy khi đơn hàng đang ở trạng thái 'Pending' hoặc 'Confirmed'.");
                }

                // Gọi API hủy đơn hàng
                var cancelRequest = new
                {
                    Reason = reason ?? "Cancelled by customer",
                    CancelledAt = DateTime.Now
                };

                var response = await _httpClient.PutAsJsonAsync($"main/api/Order/CancelOrder/{orderId}", orderId);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Cancel order failed: {response.StatusCode} - {errorContent}");
                    return (false, "Không thể hủy đơn hàng. Vui lòng thử lại sau.");
                }

                var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<bool>>();
                if (result?.Success == true)
                {
                    Console.WriteLine($"✅ Order {orderId} cancelled successfully");
                    return (true, "Đơn hàng đã được hủy thành công");
                }

                return (false, result?.Message ?? "Không thể hủy đơn hàng");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception cancelling order: {ex.Message}");
                return (false, "Có lỗi xảy ra khi hủy đơn hàng");
            }
        }

        /// <summary>
        /// Kiểm tra xem đơn hàng có thể hủy hay không
        /// </summary>
        public async Task<bool> CanCancelOrderAsync(int orderId)
        {
            try
            {
                var currentStatus = await GetOrderStatusNameAsync(orderId);
                if (string.IsNullOrEmpty(currentStatus))
                    return false;

                var allowedStatuses = new[] { "Pending", "Confirmed" };
                return allowedStatuses.Contains(currentStatus, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception checking cancel permission: {ex.Message}");
                return false;
            }
        }
        public async Task<IEnumerable<OrderWithDetailsVM>> GetOrdersBySellerWithDetailsAsync(int sellerId)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync($"main/api/Order/GetOrdersBySellerWithDetails/{sellerId}");

            if (!response.IsSuccessStatusCode)
            {
                return Enumerable.Empty<OrderWithDetailsVM>();
            }

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<OrderWithDetailsVM>>>();

            if (result?.Success == true && result.Data != null)
            {
                return result.Data;
            }

            return Enumerable.Empty<OrderWithDetailsVM>();
        }
        public async Task<AdminOrdersCompleteView> GetAllOrdersWithCompleteDetailsAsync()
{
    await SetAuthorizationHeader();
    
    try
    {
        var response = await _httpClient.GetAsync("main/api/Order/GetAllOrdersWithCompleteDetails");

        if (!response.IsSuccessStatusCode)
        {
            return new AdminOrdersCompleteView();
        }

        var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<AdminOrdersCompleteView>>();

        if (result?.Success == true && result.Data != null)
        {
            return result.Data;
        }
    }
    catch (Exception ex)
    {
        // Log error và return empty data
        Console.WriteLine($"Error calling GetAllOrdersWithCompleteDetailsAsync: {ex.Message}");
    }

    return new AdminOrdersCompleteView();
}
        #endregion

        #region OrderItem Management

        /// <summary>
        /// Lấy tất cả chi tiết đơn hàng - Chỉ Admin
        /// </summary>
        public async Task<IEnumerable<OrderItemVM>> GetAllOrderItemsAsync()
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync("main/api/Order/OrderItems/GetAllOrderItems");

            if (!response.IsSuccessStatusCode)
            {
                return Enumerable.Empty<OrderItemVM>();
            }

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<OrderItemVM>>>();

            if (result?.Success == true && result.Data != null)
            {
                return result.Data;
            }

            return Enumerable.Empty<OrderItemVM>();
        }

        /// <summary>
        /// Lấy chi tiết đơn hàng theo ID
        /// </summary>
        public async Task<OrderItemVM> GetOrderItemByIdAsync(int orderItemId)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync($"main/api/Order/OrderItems/GetOrderItemById/{orderItemId}");

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<OrderItemVM>>();

            if (result?.Success == true && result.Data != null)
                return result.Data;

            return null;
        }

        /// <summary>
        /// Lấy chi tiết đơn hàng theo OrderId
        /// </summary>
        public async Task<IEnumerable<OrderItemVM>> GetOrderItemsByOrderIdAsync(int orderId)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync($"main/api/Order/OrderItems/GetOrderItemsByOrderId/{orderId}");

            if (!response.IsSuccessStatusCode)
            {
                return Enumerable.Empty<OrderItemVM>();
            }

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<OrderItemVM>>>();

            if (result?.Success == true && result.Data != null)
            {
                return result.Data;
            }

            return Enumerable.Empty<OrderItemVM>();
        }

        /// <summary>
        /// Lấy chi tiết đơn hàng theo ProductId - Chỉ Admin
        /// </summary>
        public async Task<IEnumerable<OrderItemVM>> GetOrderItemsByProductIdAsync(int productId)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync($"main/api/Order/OrderItems/GetOrderItemsByProductId/{productId}");

            if (!response.IsSuccessStatusCode)
            {
                return Enumerable.Empty<OrderItemVM>();
            }

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<OrderItemVM>>>();

            if (result?.Success == true && result.Data != null)
            {
                return result.Data;
            }

            return Enumerable.Empty<OrderItemVM>();
        }

        /// <summary>
        /// Lấy tổng tiền theo OrderId
        /// </summary>
        public async Task<decimal> GetTotalAmountByOrderIdAsync(int orderId)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.GetAsync($"main/api/Order/OrderItems/GetTotalAmountByOrderId/{orderId}");

            if (!response.IsSuccessStatusCode)
            {
                return 0;
            }

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<decimal>>();

            if (result?.Success == true)
                return result.Data;

            return 0;
        }

        /// <summary>
        /// Tạo chi tiết đơn hàng mới
        /// </summary>
        public async Task<bool> CreateOrderItemAsync(OrderItemVM orderItemVM)
        {
            if (orderItemVM == null) return false;

            await SetAuthorizationHeader();
            var response = await _httpClient.PostAsJsonAsync("main/api/Order/OrderItems/CreateOrderItem", orderItemVM);

            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<string>>();
            return result?.Success ?? false;
        }

        /// <summary>
        /// Cập nhật chi tiết đơn hàng
        /// </summary>
        public async Task<bool> UpdateOrderItemAsync(OrderItemVM orderItemVM)
        {
            if (orderItemVM == null) return false;

            await SetAuthorizationHeader();
            var response = await _httpClient.PutAsJsonAsync("main/api/Order/OrderItems/UpdateOrderItem", orderItemVM);

            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<string>>();
            return result?.Success ?? false;
        }

        /// <summary>
        /// Xóa chi tiết đơn hàng - Chỉ Admin
        /// </summary>
        public async Task<bool> DeleteOrderItemAsync(int orderItemId)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.DeleteAsync($"main/api/Order/OrderItems/DeleteOrderItem/{orderItemId}");

            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<string>>();
            return result?.Success ?? false;
        }

        #endregion

        #region OrderStatus Management

        /// <summary>
        /// Lấy tất cả trạng thái đơn hàng
        /// </summary>
        public async Task<IEnumerable<OrderStatusVM>> GetAllOrderStatusesAsync()
        {
            var response = await _httpClient.GetAsync("main/api/Order/OrderStatus/GetAllOrderStatuses");

            if (!response.IsSuccessStatusCode)
            {
                return Enumerable.Empty<OrderStatusVM>();
            }

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<IEnumerable<OrderStatusVM>>>();

            if (result?.Success == true && result.Data != null)
            {
                return result.Data;
            }

            return Enumerable.Empty<OrderStatusVM>();
        }

        /// <summary>
        /// Lấy trạng thái đơn hàng theo ID
        /// </summary>
        public async Task<OrderStatusVM> GetOrderStatusByIdAsync(int statusId)
        {
            var response = await _httpClient.GetAsync($"main/api/Order/OrderStatus/GetOrderStatusById/{statusId}");

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<OrderStatusVM>>();

            if (result?.Success == true && result.Data != null)
                return result.Data;

            return null;
        }

        /// <summary>
        /// Lấy trạng thái đơn hàng theo tên
        /// </summary>
        public async Task<OrderStatusVM> GetOrderStatusByNameAsync(string statusName)
        {
            if (string.IsNullOrEmpty(statusName)) return null;

            var response = await _httpClient.GetAsync($"main/api/Order/OrderStatus/GetOrderStatusByName/{Uri.EscapeDataString(statusName)}");

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<OrderStatusVM>>();

            if (result?.Success == true && result.Data != null)
                return result.Data;

            return null;
        }

        /// <summary>
        /// Tạo trạng thái đơn hàng mới - Chỉ Admin
        /// </summary>
        public async Task<bool> CreateOrderStatusAsync(OrderStatusVM orderStatusVM)
        {
            if (orderStatusVM == null) return false;

            await SetAuthorizationHeader();
            var response = await _httpClient.PostAsJsonAsync("main/api/Order/OrderStatus/CreateOrderStatus", orderStatusVM);

            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<string>>();
            return result?.Success ?? false;
        }

        /// <summary>
        /// Cập nhật trạng thái đơn hàng - Chỉ Admin
        /// </summary>
        public async Task<bool> UpdateOrderStatusAsync(OrderStatusVM orderStatusVM)
        {
            if (orderStatusVM == null) return false;

            await SetAuthorizationHeader();
            var response = await _httpClient.PutAsJsonAsync("main/api/Order/OrderStatus/UpdateOrderStatus", orderStatusVM);

            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<string>>();
            return result?.Success ?? false;
        }

        /// <summary>
        /// Xóa trạng thái đơn hàng - Chỉ Admin
        /// </summary>
        public async Task<bool> DeleteOrderStatusAsync(int statusId)
        {
            await SetAuthorizationHeader();
            var response = await _httpClient.DeleteAsync($"main/api/Order/OrderStatus/DeleteOrderStatus/{statusId}");

            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<string>>();
            return result?.Success ?? false;
        }

        #endregion
    }
}