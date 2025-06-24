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
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        /// <summary>
        /// Lấy tất cả thanh toán - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("GetAllPayments")]
        public async Task<IActionResult> GetAllPayments()
        {
            var response = await _paymentService.GetAllPayments();
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
        /// Lấy thanh toán theo ID - Admin hoặc chủ sở hữu đơn hàng
        /// </summary>
        [HttpGet("GetPaymentById/{paymentId}")]
        public async Task<IActionResult> GetPaymentById(int paymentId)
        {
            var response = await _paymentService.GetPaymentById(paymentId);
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
        /// Lấy thanh toán theo OrderId - Admin hoặc chủ sở hữu đơn hàng
        /// </summary>
        [HttpGet("GetPaymentsByOrderId/{orderId}")]
        public async Task<IActionResult> GetPaymentsByOrderId(int orderId)
        {
            var response = await _paymentService.GetPaymentsByOrderId(orderId);
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
        /// Lấy thanh toán theo trạng thái - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("GetPaymentsByStatus/{status}")]
        public async Task<IActionResult> GetPaymentsByStatus(string status)
        {
            var response = await _paymentService.GetPaymentsByStatus(status);
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
        /// Tạo thanh toán mới - Cho tất cả user đã đăng nhập
        /// </summary>
        [HttpPost("CreatePayment")]
        public async Task<IActionResult> CreatePayment([FromBody] PaymentVM paymentVM)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _paymentService.CreatePayment(paymentVM);
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
        /// Cập nhật thanh toán - Admin hoặc chủ sở hữu đơn hàng
        /// </summary>
        [HttpPut("UpdatePayment")]
        public async Task<IActionResult> UpdatePayment([FromBody] PaymentVM paymentVM)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _paymentService.UpdatePayment(paymentVM);
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
        /// Cập nhật trạng thái thanh toán - Admin hoặc Payment Gateway
        /// </summary>
        [HttpPut("UpdatePaymentStatus/{paymentId}")]
        public async Task<IActionResult> UpdatePaymentStatus(int paymentId, [FromBody] UpdatePaymentStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _paymentService.UpdatePaymentStatus(paymentId, request.Status);
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
        /// Xóa thanh toán - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("DeletePayment/{paymentId}")]
        public async Task<IActionResult> DeletePayment(int paymentId)
        {
            var response = await _paymentService.DeletePayment(paymentId);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }
    }

    /// <summary>
    /// DTO cho việc cập nhật trạng thái thanh toán
    /// </summary>
    public class UpdatePaymentStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}