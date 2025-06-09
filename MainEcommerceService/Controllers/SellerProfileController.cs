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
    public class SellerProfileController : ControllerBase
    {
        private readonly ISellerProfileService _sellerProfileService;

        public SellerProfileController(ISellerProfileService sellerProfileService)
        {
            _sellerProfileService = sellerProfileService;
        }

        /// <summary>
        /// Lấy tất cả seller profile - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("GetAllSellerProfiles")]
        public async Task<IActionResult> GetAllSellerProfiles()
        {
            var response = await _sellerProfileService.GetAllSellerProfiles();
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
        /// Lấy seller profile theo ID - Cho tất cả user đã đăng nhập
        /// </summary>
        [HttpGet("GetSellerProfileById/{sellerId}")]
        public async Task<IActionResult> GetSellerProfileById(int sellerId)
        {
            var response = await _sellerProfileService.GetSellerProfileById(sellerId);
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
        /// Lấy seller profile theo UserId - Cho tất cả user đã đăng nhập
        /// </summary>
        [HttpGet("GetSellerProfileByUserId/{userId}")]
        public async Task<IActionResult> GetSellerProfileByUserId(int userId)
        {
            var response = await _sellerProfileService.GetSellerProfileByUserId(userId);
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
        /// Tạo seller profile mới - Cho tất cả user đã đăng nhập
        /// </summary>
        [HttpPost("CreateSellerProfile")]
        public async Task<IActionResult> CreateSellerProfile([FromBody] SellerProfileVM sellerProfileVM)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _sellerProfileService.CreateSellerProfile(sellerProfileVM);
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
        /// Cập nhật seller profile - Chỉ chủ sở hữu hoặc Admin
        /// </summary>
        [Authorize(Roles = "Seller,Admin")]
        [HttpPut("UpdateSellerProfile")]
        public async Task<IActionResult> UpdateSellerProfile([FromBody] SellerProfileVM sellerProfileVM)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _sellerProfileService.UpdateSellerProfile(sellerProfileVM);
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
        /// Xóa seller profile - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("DeleteSellerProfile/{sellerId}")]
        public async Task<IActionResult> DeleteSellerProfile(int sellerId)
        {
            var response = await _sellerProfileService.DeleteSellerProfile(sellerId);
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
        /// Xác minh seller profile - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("VerifySellerProfile/{sellerId}")]
        public async Task<IActionResult> VerifySellerProfile(int sellerId)
        {
            var response = await _sellerProfileService.VerifySellerProfile(sellerId);
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
        /// Hủy xác minh seller profile - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("UnverifySellerProfile/{sellerId}")]
        public async Task<IActionResult> UnverifySellerProfile(int sellerId)
        {
            var response = await _sellerProfileService.UnverifySellerProfile(sellerId);
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
        /// Lấy danh sách seller profile đã được xác minh - Cho tất cả người dùng
        /// </summary>
        [AllowAnonymous]
        [HttpGet("GetVerifiedSellerProfiles")]
        public async Task<IActionResult> GetVerifiedSellerProfiles()
        {
            var response = await _sellerProfileService.GetVerifiedSellerProfiles();
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
        /// Lấy danh sách seller profile chờ xác minh - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("GetPendingVerificationProfiles")]
        public async Task<IActionResult> GetPendingVerificationProfiles()
        {
            var response = await _sellerProfileService.GetPendingVerificationProfiles();
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
        /// Kiểm tra user có seller profile hay không - Cho tất cả user đã đăng nhập
        /// </summary>
        [HttpGet("CheckUserHasSellerProfile/{userId}")]
        public async Task<IActionResult> CheckUserHasSellerProfile(int userId)
        {
            var response = await _sellerProfileService.CheckUserHasSellerProfile(userId);
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