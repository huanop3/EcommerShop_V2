using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MainEcommerceService.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MainEcommerceService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShipperProfileController : ControllerBase
    {
        private readonly IShipperProfileService _shipperProfileService;

        public ShipperProfileController(IShipperProfileService shipperProfileService)
        {
            _shipperProfileService = shipperProfileService;
        }

        /// <summary>
        /// Lấy tất cả shipper profile - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("GetAllShipperProfiles")]
        public async Task<IActionResult> GetAllShipperProfiles()
        {
            var response = await _shipperProfileService.GetAllShipperProfiles();
            if (response.Success)
                return Ok(response);
            return BadRequest(response);
        }

        /// <summary>
        /// Lấy shipper profile theo ID - Cho tất cả user đã đăng nhập
        /// </summary>
        [HttpGet("GetShipperProfileById/{shipperId}")]
        public async Task<IActionResult> GetShipperProfileById(int shipperId)
        {
            var response = await _shipperProfileService.GetShipperProfileById(shipperId);
            if (response.Success)
                return Ok(response);
            return BadRequest(response);
        }

        /// <summary>
        /// Lấy shipper profile theo UserId - Cho tất cả user đã đăng nhập
        /// </summary>
        [HttpGet("GetShipperProfileByUserId/{userId}")]
        public async Task<IActionResult> GetShipperProfileByUserId(int userId)
        {
            var response = await _shipperProfileService.GetShipperProfileByUserId(userId);
            if (response.Success)
                return Ok(response);
            return BadRequest(response);
        }

        /// <summary>
        /// Tạo shipper profile mới - Cho tất cả user đã đăng nhập
        /// </summary>
        [HttpPost("CreateShipperProfile")]
        public async Task<IActionResult> CreateShipperProfile([FromBody] ShipperProfileVM shipperProfileVM)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _shipperProfileService.CreateShipperProfile(shipperProfileVM);
            if (response.Success)
                return Ok(response);
            return BadRequest(response);
        }

        /// <summary>
        /// Cập nhật shipper profile - Chỉ chủ sở hữu hoặc Admin
        /// </summary>
        [Authorize(Roles = "Shipper,Admin")]
        [HttpPut("UpdateShipperProfile")]
        public async Task<IActionResult> UpdateShipperProfile([FromBody] ShipperProfileVM shipperProfileVM)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _shipperProfileService.UpdateShipperProfile(shipperProfileVM);
            if (response.Success)
                return Ok(response);
            return BadRequest(response);
        }

        /// <summary>
        /// Xóa shipper profile - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("DeleteShipperProfile/{shipperId}")]
        public async Task<IActionResult> DeleteShipperProfile(int shipperId)
        {
            var response = await _shipperProfileService.DeleteShipperProfile(shipperId);
            if (response.Success)
                return Ok(response);
            return BadRequest(response);
        }

        /// <summary>
        /// Kích hoạt shipper profile - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("ActivateShipperProfile/{shipperId}")]
        public async Task<IActionResult> ActivateShipperProfile(int shipperId)
        {
            var response = await _shipperProfileService.ActivateShipperProfile(shipperId);
            if (response.Success)
                return Ok(response);
            return BadRequest(response);
        }

        /// <summary>
        /// Hủy kích hoạt shipper profile - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("DeactivateShipperProfile/{shipperId}")]
        public async Task<IActionResult> DeactivateShipperProfile(int shipperId)
        {
            var response = await _shipperProfileService.DeactivateShipperProfile(shipperId);
            if (response.Success)
                return Ok(response);
            return BadRequest(response);
        }

        /// <summary>
        /// Lấy danh sách shipper profile đang hoạt động - Cho tất cả người dùng
        /// </summary>
        [AllowAnonymous]
        [HttpGet("GetActiveShipperProfiles")]
        public async Task<IActionResult> GetActiveShipperProfiles()
        {
            var response = await _shipperProfileService.GetActiveShipperProfiles();
            if (response.Success)
                return Ok(response);
            return BadRequest(response);
        }

        /// <summary>
        /// Lấy danh sách shipper profile không hoạt động - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("GetInactiveShipperProfiles")]
        public async Task<IActionResult> GetInactiveShipperProfiles()
        {
            var response = await _shipperProfileService.GetInactiveShipperProfiles();
            if (response.Success)
                return Ok(response);
            return BadRequest(response);
        }

        /// <summary>
        /// Kiểm tra user có shipper profile hay không - Cho tất cả user đã đăng nhập
        /// </summary>
        [HttpGet("CheckUserHasShipperProfile/{userId}")]
        public async Task<IActionResult> CheckUserHasShipperProfile(int userId)
        {
            var response = await _shipperProfileService.CheckUserHasShipperProfile(userId);
            if (response.Success)
                return Ok(response);
            return BadRequest(response);
        }
    }
}