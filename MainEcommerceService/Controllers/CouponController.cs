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
    public class CouponController : ControllerBase
    {
        private readonly ICouponService _couponService;

        public CouponController(ICouponService couponService)
        {
            _couponService = couponService;
        }

        /// <summary>
        /// Lấy tất cả mã giảm giá - Chỉ Admin
        /// </summary>
        [HttpGet("GetAllCoupons")]
        public async Task<IActionResult> GetAllCoupons()
        {
            var response = await _couponService.GetAllCoupons();
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
        /// Lấy mã giảm giá theo ID - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("GetCouponById/{couponId}")]
        public async Task<IActionResult> GetCouponById(int couponId)
        {
            var response = await _couponService.GetCouponById(couponId);
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
        /// Lấy mã giảm giá theo mã code - Cho tất cả user đã đăng nhập
        /// </summary>
        [HttpGet("GetCouponByCode/{couponCode}")]
        public async Task<IActionResult> GetCouponByCode(string couponCode)
        {
            var response = await _couponService.GetCouponByCode(couponCode);
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
        /// Tạo mã giảm giá mới - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("CreateCoupon")]
        public async Task<IActionResult> CreateCoupon([FromBody] CouponVM couponVM)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _couponService.CreateCoupon(couponVM);
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
        /// Cập nhật mã giảm giá - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("UpdateCoupon")]
        public async Task<IActionResult> UpdateCoupon([FromBody] CouponVM couponVM)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _couponService.UpdateCoupon(couponVM);
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
        /// Xóa mã giảm giá - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("DeleteCoupon/{couponId}")]
        public async Task<IActionResult> DeleteCoupon(int couponId)
        {
            var response = await _couponService.DeleteCoupon(couponId);
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
        /// Kích hoạt mã giảm giá - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("ActivateCoupon/{couponId}")]
        public async Task<IActionResult> ActivateCoupon(int couponId)
        {
            var response = await _couponService.ActivateCoupon(couponId);
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
        /// Vô hiệu hóa mã giảm giá - Chỉ Admin
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("DeactivateCoupon/{couponId}")]
        public async Task<IActionResult> DeactivateCoupon(int couponId)
        {
            var response = await _couponService.DeactivateCoupon(couponId);
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
        /// Lấy danh sách mã giảm giá đang hoạt động - Cho tất cả user đã đăng nhập
        /// </summary>
        [HttpGet("GetActiveCoupons")]
        public async Task<IActionResult> GetActiveCoupons()
        {
            var response = await _couponService.GetActiveCoupons();
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
        /// Kiểm tra tính hợp lệ của mã giảm giá - Cho tất cả user đã đăng nhập
        /// </summary>
        [HttpGet("ValidateCoupon/{couponCode}")]
        public async Task<IActionResult> ValidateCoupon(string couponCode)
        {
            var response = await _couponService.ValidateCoupon(couponCode);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        [HttpPut("UpdateCouponUsageCount/{couponId}")]
        public async Task<IActionResult> UpdateCouponUsageCountAsync([FromBody] int couponId)
        {
            var response = await _couponService.UpdateCouponUsageCount(couponId);
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