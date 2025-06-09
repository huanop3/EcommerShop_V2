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
    public class AddressController : ControllerBase
    {
        private readonly IAddressService _addressService;

        public AddressController(IAddressService addressService)
        {
            _addressService = addressService;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("GetAllAddresses")]
        public async Task<IActionResult> GetAllAddresses()
        {
            var response = await _addressService.GetAllAddresses();
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }
        [HttpGet("GetAddressesByUserId")]
        public async Task<IActionResult> GetAddressesByUserId(int userId)
        {
            try
            {
                var response = await _addressService.GetAddressesByUserId(userId);
                
                // Even if no addresses found, return success with empty list
                // This is normal behavior for new users
                if (response.Success || (response.Data != null && !response.Data.Any()))
                {
                    // If no addresses found, create a successful response with empty list
                    if (response.Data == null || !response.Data.Any())
                    {
                        return Ok(new HTTPResponseClient<IEnumerable<AddressVM>>
                        {
                            Success = true,
                            Message = "User has no addresses yet",
                            Data = new List<AddressVM>()
                        });
                    }
                    
                    return Ok(response);
                }
                else
                {
                    // Only return BadRequest for actual errors, not for "no data found"
                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new HTTPResponseClient<IEnumerable<AddressVM>>
                {
                    Success = false,
                    Message = $"Error retrieving addresses: {ex.Message}",
                    Data = null
                });
            }
        }

        [HttpGet("GetAddressById")]
        public async Task<IActionResult> GetAddressById(int addressId)
        {
            var response = await _addressService.GetAddressById(addressId);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        [Authorize]
        [HttpPost("CreateAddress")]
        public async Task<IActionResult> CreateAddress([FromBody] AddressVM addressCreateRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _addressService.CreateAddress(addressCreateRequest);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        [Authorize]
        [HttpPut("UpdateAddress")]
        public async Task<IActionResult> UpdateAddress([FromBody] AddressVM addressUpdateRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _addressService.UpdateAddress(addressUpdateRequest);
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        [Authorize]
        [HttpDelete("DeleteAddress")]
        public async Task<IActionResult> DeleteAddress(string addressId)
        {
            var response = await _addressService.DeleteAddress(int.Parse(addressId));
            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        [Authorize]
        [HttpPut("SetDefaultAddress")]
        public async Task<IActionResult> SetDefaultAddress(int addressId, int userId)
        {
            var response = await _addressService.SetDefaultAddress(addressId, userId);
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
