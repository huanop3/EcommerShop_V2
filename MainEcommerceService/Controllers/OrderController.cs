using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MainEcommerceService.Models.dbMainEcommer;
using MainEcommerceService.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MainEcommerceService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IOrderItemService _orderItemService;
        private readonly IOrderStatusService _orderStatusService;

        public OrderController(
            IOrderService orderService,
            IOrderItemService orderItemService,
            IOrderStatusService orderStatusService)
        {
            _orderService = orderService;
            _orderItemService = orderItemService;
            _orderStatusService = orderStatusService;
        }

        #region Order Management

        /// <summary>
        /// Lấy tất cả đơn hàng - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin,Seller")]
        [HttpGet("GetAllOrders")]
        public async Task<IActionResult> GetAllOrders()
        {
            var response = await _orderService.GetAllOrders();
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        /// <summary>
        /// Lấy đơn hàng theo ID - Admin hoặc chủ sở hữu
        /// </summary>
        [Authorize]
        [HttpGet("GetOrderById/{orderId}")]
        public async Task<IActionResult> GetOrderById(int orderId)
        {
            var response = await _orderService.GetOrderById(orderId);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        /// <summary>
        /// Lấy đơn hàng theo UserId - Admin hoặc chủ sở hữu
        /// </summary>
        [Authorize]
        [HttpGet("GetOrdersByUserId/{userId}")]
        public async Task<IActionResult> GetOrdersByUserId(int userId)
        {
            var response = await _orderService.GetOrdersByUserId(userId);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        /// <summary>
        /// Lấy đơn hàng theo trạng thái - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin,Seller")]
        [HttpGet("GetOrdersByStatus/{statusId}")]
        public async Task<IActionResult> GetOrdersByStatus(int statusId)
        {
            var response = await _orderService.GetOrdersByStatus(statusId);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        /// <summary>
        /// Lấy đơn hàng theo khoảng thời gian - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin,Seller")]
        [HttpGet("GetOrdersByDateRange")]
        public async Task<IActionResult> GetOrdersByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var response = await _orderService.GetOrdersByDateRange(startDate, endDate);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        /// <summary>
        /// Tạo đơn hàng mới - Cho tất cả user đã đăng nhập
        /// </summary>
        [Authorize]
        [HttpPost("CreateOrder")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderVM orderVM)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _orderService.CreateOrder(orderVM);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        /// <summary>
        /// Tạo đơn hàng với chi tiết - Cho tất cả user đã đăng nhập
        /// </summary>
        [Authorize]
        [HttpPost("CreateOrderWithItems")]
        public async Task<IActionResult> CreateOrderWithItems([FromBody] CreateOrderRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var orderVM = new OrderVM
            {
                UserId = request.UserId,
                ShippingAddressId = request.ShippingAddressId,
                CouponId = request.CouponId,
                TotalAmount = request.OrderItems.Sum(item => item.Quantity * item.UnitPrice)
            };

            var orderItems = request.OrderItems.Select(item => new OrderItemVM
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList();

            var response = await _orderService.CreateOrderWithItems(orderVM, orderItems);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        /// <summary>
        /// Cập nhật đơn hàng - Admin hoặc chủ sở hữu
        /// </summary>
        [Authorize]
        [HttpPut("UpdateOrder")]
        public async Task<IActionResult> UpdateOrder([FromBody] OrderVM orderVM)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _orderService.UpdateOrder(orderVM);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        /// <summary>
        /// Cập nhật trạng thái đơn hàng - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin,Seller")]
        [HttpPut("UpdateOrderStatus/{orderId}")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] UpdateOrderStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _orderService.UpdateOrderStatus(orderId, request.StatusId);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        /// <summary>
        /// Cập nhật trạng thái đơn hàng theo tên - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin,Seller")]
        [HttpPut("UpdateOrderStatusByName/{orderId}")]
        public async Task<IActionResult> UpdateOrderStatusByName(int orderId, [FromBody] UpdateOrderStatusByNameRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _orderService.UpdateOrderStatusByName(orderId, request.StatusName);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        /// <summary>
        /// Xóa đơn hàng - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin,Seller")]
        [HttpDelete("DeleteOrder/{orderId}")]
        public async Task<IActionResult> DeleteOrder(int orderId)
        {
            var response = await _orderService.DeleteOrder(orderId);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }
        [HttpGet("GetOrderStatusNameByOrderId/{orderId}")]
        public async Task<IActionResult> GetOrderStatusNameByOrderId(int orderId)
        {
            var response = await _orderService.GetOrderStatusNameByOrderId(orderId);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }
        [HttpPut("CancelOrder/{orderId}")]
        public async Task<IActionResult> CancelOrder([FromBody] int orderId)
        {
            var response = await _orderService.CancelOrder(orderId);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }
        [Authorize(Roles = "Seller")]
        [HttpGet("GetOrdersBySellerWithDetails/{sellerId}")]
        public async Task<IActionResult> GetOrdersBySellerWithDetails(int sellerId)
        {
            var response = await _orderService.GetOrdersBySellerWithDetails(sellerId);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }
        /// <summary>
        /// Lấy tất cả đơn hàng với đầy đủ thông tin cho Admin - CHỈ 1 API CALL
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("GetAllOrdersWithCompleteDetails")]
        public async Task<IActionResult> GetAllOrdersWithCompleteDetails()
        {
            var response = await _orderService.GetAllOrdersWithCompleteDetails();
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }
        #endregion

        #region OrderItem Management

        /// <summary>
        /// Lấy tất cả chi tiết đơn hàng - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin,Seller")]
        [HttpGet("OrderItems/GetAllOrderItems")]
        public async Task<IActionResult> GetAllOrderItems()
        {
            var response = await _orderItemService.GetAllOrderItems();
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        /// <summary>
        /// Lấy chi tiết đơn hàng theo ID - Admin hoặc chủ sở hữu đơn hàng
        /// </summary>
        [Authorize]
        [HttpGet("OrderItems/GetOrderItemById/{orderItemId}")]
        public async Task<IActionResult> GetOrderItemById(int orderItemId)
        {
            var response = await _orderItemService.GetOrderItemById(orderItemId);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        /// <summary>
        /// Lấy chi tiết đơn hàng theo OrderId - Admin hoặc chủ sở hữu đơn hàng
        /// </summary>
        [Authorize]
        [HttpGet("OrderItems/GetOrderItemsByOrderId/{orderId}")]
        public async Task<IActionResult> GetOrderItemsByOrderId(int orderId)
        {
            var response = await _orderItemService.GetOrderItemsByOrderId(orderId);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        /// <summary>
        /// Lấy chi tiết đơn hàng theo ProductId - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin,Seller")]
        [HttpGet("OrderItems/GetOrderItemsByProductId/{productId}")]
        public async Task<IActionResult> GetOrderItemsByProductId(int productId)
        {
            var response = await _orderItemService.GetOrderItemsByProductId(productId);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        /// <summary>
        /// Lấy tổng tiền theo OrderId - Admin hoặc chủ sở hữu đơn hàng
        /// </summary>
        [Authorize]
        [HttpGet("OrderItems/GetTotalAmountByOrderId/{orderId}")]
        public async Task<IActionResult> GetTotalAmountByOrderId(int orderId)
        {
            var response = await _orderItemService.GetTotalAmountByOrderId(orderId);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        /// <summary>
        /// Tạo chi tiết đơn hàng mới - Admin hoặc chủ sở hữu đơn hàng
        /// </summary>
        [Authorize]
        [HttpPost("OrderItems/CreateOrderItem")]
        public async Task<IActionResult> CreateOrderItem([FromBody] OrderItemVM orderItemVM)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _orderItemService.CreateOrderItem(orderItemVM);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        /// <summary>
        /// Cập nhật chi tiết đơn hàng - Admin hoặc chủ sở hữu đơn hàng
        /// </summary>
        [Authorize]
        [HttpPut("OrderItems/UpdateOrderItem")]
        public async Task<IActionResult> UpdateOrderItem([FromBody] OrderItemVM orderItemVM)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _orderItemService.UpdateOrderItem(orderItemVM);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        /// <summary>
        /// Xóa chi tiết đơn hàng - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin,Seller")]
        [HttpDelete("OrderItems/DeleteOrderItem/{orderItemId}")]
        public async Task<IActionResult> DeleteOrderItem(int orderItemId)
        {
            var response = await _orderItemService.DeleteOrderItem(orderItemId);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        #endregion

        #region OrderStatus Management

        /// <summary>
        /// Lấy tất cả trạng thái đơn hàng - Cho tất cả người dùng
        /// </summary>
        [AllowAnonymous]
        [HttpGet("OrderStatus/GetAllOrderStatuses")]
        public async Task<IActionResult> GetAllOrderStatuses()
        {
            var response = await _orderStatusService.GetAllOrderStatuses();
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        /// <summary>
        /// Lấy trạng thái đơn hàng theo ID - Cho tất cả người dùng
        /// </summary>
        [AllowAnonymous]
        [HttpGet("OrderStatus/GetOrderStatusById/{statusId}")]
        public async Task<IActionResult> GetOrderStatusById(int statusId)
        {
            var response = await _orderStatusService.GetOrderStatusById(statusId);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        /// <summary>
        /// Lấy trạng thái đơn hàng theo tên - Cho tất cả người dùng
        /// </summary>
        [AllowAnonymous]
        [HttpGet("OrderStatus/GetOrderStatusByName/{statusName}")]
        public async Task<IActionResult> GetOrderStatusByName(string statusName)
        {
            var response = await _orderStatusService.GetOrderStatusByName(statusName);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        /// <summary>
        /// Tạo trạng thái đơn hàng mới - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin,Seller")]
        [HttpPost("OrderStatus/CreateOrderStatus")]
        public async Task<IActionResult> CreateOrderStatus([FromBody] OrderStatusVM orderStatusVM)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _orderStatusService.CreateOrderStatus(orderStatusVM);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        /// <summary>
        /// Cập nhật trạng thái đơn hàng - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin,Seller")]
        [HttpPut("OrderStatus/UpdateOrderStatus")]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] OrderStatusVM orderStatusVM)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _orderStatusService.UpdateOrderStatus(orderStatusVM);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        /// <summary>
        /// Xóa trạng thái đơn hàng - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin,Seller")]
        [HttpDelete("OrderStatus/DeleteOrderStatus/{statusId}")]
        public async Task<IActionResult> DeleteOrderStatus(int statusId)
        {
            var response = await _orderStatusService.DeleteOrderStatus(statusId);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        #endregion
    }

}