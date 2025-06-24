using MainEcommerceService.Infrastructure.Services;
using MainEcommerceService.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MainEcommerceService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Get Admin Dashboard Data
        /// </summary>
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminDashboard()
        {
            var response = await _dashboardService.GetAdminDashboardAsync();
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
        /// Get Seller Dashboard Data
        /// </summary>
        [HttpGet("seller/{sellerId}")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetSellerDashboard(int sellerId)
        {
            var response = await _dashboardService.GetSellerDashboardAsync(sellerId);
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
        /// Get Seller Dashboard Data by ID (Admin access)
        /// </summary>
        // [HttpGet("seller/{sellerId}")]
        // [Authorize(Roles = "Admin")]
        // public async Task<IActionResult> GetSellerDashboardById(int sellerId)
        // {
        //     var response = await _dashboardService.GetSellerDashboardAsync(sellerId);
        //     if (response.Success)
        //     {
        //         return Ok(response);
        //     }
        //     else
        //     {
        //         return BadRequest(response);
        //     }
        // }

        /// <summary>
        /// Get Basic Dashboard Stats (Lightweight)
        /// </summary>
        [HttpGet("stats")]
        [Authorize(Roles = "Admin,Seller")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var response = await _dashboardService.GetDashboardStatsAsync();
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
        /// Get System Health Check
        /// </summary>
        [HttpGet("health")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSystemHealth()
        {
            var response = await _dashboardService.GetDashboardStatsAsync();
            if (response.Success)
            {
                var health = new
                {
                    Status = "Healthy",
                    Timestamp = DateTime.Now,
                    Alerts = new
                    {
                        LowStockProducts = response.Data.LowStockProducts,
                        PendingOrders = response.Data.PendingOrders,
                        VerificationPending = response.Data.VerificationPending
                    },
                    Performance = new
                    {
                        TotalUsers = response.Data.TotalUsers,
                        TotalOrders = response.Data.TotalOrders,
                        TotalProducts = response.Data.TotalProducts,
                        TotalRevenue = response.Data.TotalRevenue
                    }
                };

                return Ok(new HTTPResponseClient<object>
                {
                    Success = true,
                    StatusCode = 200,
                    Message = "Lấy thông tin sức khỏe hệ thống thành công",
                    Data = health,
                    DateTime = DateTime.Now
                });
            }
            else
            {
                return BadRequest(response);
            }
        }
    }
}