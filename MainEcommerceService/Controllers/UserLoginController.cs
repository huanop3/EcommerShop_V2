using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MainEcommerceService.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
//using MainEcommerceService.Models;

namespace MainEcommerceService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserLoginController : ControllerBase
    {
        private readonly IUserLoginService _userLoginService;

        public UserLoginController(IUserLoginService userLoginService)
        {
            _userLoginService = userLoginService;
        }

        [HttpPost("LoginUser")]
        public async Task<IActionResult> LoginUser(LoginRequestVM userLoginVM)
        {
            var response = await _userLoginService.Login(userLoginVM);
            //set the token in the swagger

            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }
        [HttpPost("RegisterUser")]
        public async Task<IActionResult> RegisterUser(RegisterLoginVM registerLoginVM)
        {
            var response = await _userLoginService.Register(registerLoginVM);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }
        [HttpPost("refresh-Token")]
        public async Task<IActionResult> RefreshToken(UserLoginResponseVM checkToken)
        {
            var response = await _userLoginService.RefreshToken(checkToken.AccessToken, checkToken.RefreshToken);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }
        [HttpPut("Logout")]
        public async Task<IActionResult> Logout([FromBody] string checkToken)
        {
            var response = await _userLoginService.UpdateRevokeRefreshToken(checkToken);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }
                [HttpPost("forgot")]
        public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordVM forgotPasswordVM)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _userLoginService.ForgotPassword(forgotPasswordVM);
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }

        [HttpPost("reset")]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordVM resetPasswordVM)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _userLoginService.ResetPassword(resetPasswordVM);
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
    }
}