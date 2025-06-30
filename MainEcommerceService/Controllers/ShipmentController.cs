using Microsoft.AspNetCore.Mvc;
using Infrastructure.Interfaces;
using MainEcommerceService.Models.ViewModel.ViewModels.ShipmentVM;
using Microsoft.AspNetCore.Authorization;
using MainEcommerceService.Helper;

namespace Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShipmentController : ControllerBase
    {
        private readonly IShipmentService _shipmentService;

        public ShipmentController(IShipmentService shipmentService)
        {
            _shipmentService = shipmentService;
        }

        /// <summary>
        /// Lấy thông tin dashboard shipment theo OrderId
        /// </summary>
        [HttpGet("order/{orderId}")]
        [Authorize(Roles = "Shipper,Admin")]
        public async Task<IActionResult> GetShipmentDashboard(int orderId)
        {
            try
            {
                var result = await _shipmentService.GetShipmentDashboardByOrderIdAsync(orderId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new HTTPResponseClient<object>
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error",
                    DateTime = DateTime.Now
                });
            }
        }

        /// <summary>
        /// Cập nhật trạng thái shipment
        /// </summary>
        [HttpPut("{shipmentId}/status")]
        [Authorize(Roles = "Shipper")]
        public async Task<IActionResult> UpdateShipmentStatus(int shipmentId, [FromBody] UpdateShipmentStatusRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new HTTPResponseClient<object>
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "Invalid request data",
                        DateTime = DateTime.Now
                    });
                }

                var result = await _shipmentService.UpdateShipmentStatusAsync(shipmentId, request.NewStatusId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new HTTPResponseClient<object>
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error",
                    DateTime = DateTime.Now
                });
            }
        }

        /// <summary>
        /// Lấy danh sách đơn hàng được assign cho shipper
        /// </summary>
        [HttpGet("shipper/{shipperId}/orders")]
        [Authorize(Roles = "Shipper,Admin")]
        public async Task<IActionResult> GetAssignedOrders(int shipperId)
        {
            try
            {
                var orders = await _shipmentService.GetAssignedOrdersAsync(shipperId);
                return Ok(new HTTPResponseClient<List<AssignedOrderVM>>
                {
                    Success = true,
                    StatusCode = 200,
                    Message = "Success",
                    Data = orders,
                    DateTime = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new HTTPResponseClient<object>
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error",
                    DateTime = DateTime.Now
                });
            }
        }

        /// <summary>
        /// Assign shipment cho shipper
        /// </summary>
        [HttpPost("assign")]
        [Authorize(Roles = "Admin,Shipper")]
        public async Task<IActionResult> AssignShipment(AssignShipmentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new HTTPResponseClient<object>
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "Invalid request data",
                        DateTime = DateTime.Now
                    });
                }

                var result = await _shipmentService.AssignShipmentAsync(request.OrderId, request.ShipperId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new HTTPResponseClient<object>
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error",
                    DateTime = DateTime.Now
                });
            }
        }

        /// <summary>
        /// Kiểm tra order có thể ship không
        /// </summary>
        [HttpGet("order/{orderId}/can-ship")]
        [Authorize(Roles = "Shipper,Admin")]
        public async Task<IActionResult> CanOrderBeShipped(int orderId)
        {
            try
            {
                var canShip = await _shipmentService.CanOrderBeShippedAsync(orderId);
                return Ok(new HTTPResponseClient<bool>
                {
                    Success = true,
                    StatusCode = 200,
                    Message = "Success",
                    Data = canShip,
                    DateTime = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new HTTPResponseClient<object>
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error",
                    DateTime = DateTime.Now
                });
            }
        }

        /// <summary>
        /// Lấy danh sách status updates có thể thực hiện
        /// </summary>
        [HttpGet("status/{currentStatusId}/available-updates")]
        [Authorize(Roles = "Shipper,Admin")]
        public async Task<IActionResult> GetAvailableStatusUpdates(int currentStatusId)
        {
            try
            {
                var availableStatuses = await _shipmentService.GetAvailableStatusUpdatesAsync(currentStatusId);
                return Ok(new HTTPResponseClient<List<OrderStatusOptionVM>>
                {
                    Success = true,
                    StatusCode = 200,
                    Message = "Success",
                    Data = availableStatuses,
                    DateTime = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new HTTPResponseClient<object>
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error",
                    DateTime = DateTime.Now
                });
            }
        }

        /// <summary>
        /// Lấy orders được assign cho shipper hiện tại
        /// </summary>
        [HttpGet("my-orders/{shipperId}")]
        [Authorize(Roles = "Shipper")]
        public async Task<IActionResult> GetMyAssignedOrders(int shipperId)
        {
            try
            {

                var orders = await _shipmentService.GetAssignedOrdersAsync(shipperId);
                return Ok(new HTTPResponseClient<List<AssignedOrderVM>>
                {
                    Success = true,
                    StatusCode = 200,
                    Message = "Success",
                    Data = orders,
                    DateTime = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new HTTPResponseClient<object>
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Internal server error",
                    DateTime = DateTime.Now
                });
            }
        }
    }
}