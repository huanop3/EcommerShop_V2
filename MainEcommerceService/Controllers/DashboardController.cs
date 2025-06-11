using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MainEcommerceService.Infrastructure.Services;

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
        /// Lấy tất cả dữ liệu dashboard cho Admin - CHỈ 1 API CALL
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("GetAdminDashboardComplete")]
        public async Task<IActionResult> GetAdminDashboardComplete()
        {
            var response = await _dashboardService.GetAdminDashboardComplete();
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
        /// Lấy tất cả dữ liệu dashboard cho Seller - CHỈ 1 API CALL
        /// </summary>
        [Authorize(Roles = "Seller")]
        [HttpGet("GetSellerDashboardComplete/{sellerId}")]
        public async Task<IActionResult> GetSellerDashboardComplete(int sellerId)
        {
            var response = await _dashboardService.GetSellerDashboardComplete(sellerId);
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
        /// Lấy thống kê tổng quan cho dashboard - API nhẹ
        /// </summary>
        [Authorize]
        [HttpGet("GetDashboardStats")]
        public async Task<IActionResult> GetDashboardStats(string userRole, int? sellerId = null)
        {
            var response = await _dashboardService.GetDashboardStats(userRole, sellerId);
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
}