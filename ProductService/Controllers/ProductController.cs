using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ProductService.Models.ViewModel;

namespace ProductService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProdService _prodService;

        public ProductController(IProdService prodService)
        {
            _prodService = prodService;
        }

        /// <summary>
        /// Lấy tất cả sản phẩm
        /// </summary>
        [HttpGet("GetAllProducts")]
        public async Task<IActionResult> GetAllProducts()
        {
            var result = await _prodService.GetAllProductsAsync();
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
        [HttpGet("GetPagedProducts")]
        public async Task<IActionResult> GetPagedProducts([FromQuery] int pageIndex, [FromQuery] int pageSize)
        {
            var result = await _prodService.GetAllProductByPageAsync(pageIndex, pageSize);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
        /// <summary>
        /// Lấy sản phẩm theo ID
        /// </summary>
        [HttpGet("GetProductById/{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var result = await _prodService.GetProductByIdAsync(id);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        /// <summary>
        /// Lấy sản phẩm theo danh mục
        /// </summary>
        [HttpGet("GetProductsByCategory/{categoryId}")]
        public async Task<IActionResult> GetProductsByCategory(int categoryId)
        {
            var result = await _prodService.GetProductsByCategoryAsync(categoryId);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        /// <summary>
        /// Lấy sản phẩm theo người bán
        /// </summary>
        [HttpGet("GetProductsBySeller/{sellerId}")]
        public async Task<IActionResult> GetProductsBySeller(int sellerId)
        {
            var result = await _prodService.GetProductsBySellerAsync(sellerId);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        /// <summary>
        /// Tìm kiếm sản phẩm
        /// </summary>
        [HttpGet("SearchProducts")]
        public async Task<IActionResult> SearchProducts([FromQuery] string searchTerm)
        {
            var result = await _prodService.SearchProductsAsync(searchTerm);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        /// <summary>
        /// Tạo sản phẩm mới
        /// </summary>
        [HttpPost("CreateProduct")]
        [Authorize(Roles = "Seller,Admin")]
        public async Task<IActionResult> CreateProduct([FromBody] ProductVM product, int userId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _prodService.CreateProductAsync(product, userId);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        /// <summary>
        /// Cập nhật sản phẩm
        /// </summary>
        [HttpPut("UpdateProduct/{id}")]
        [Authorize(Roles = "Seller,Admin")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductVM product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _prodService.UpdateProductAsync(id, product);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        /// <summary>
        /// Xóa sản phẩm
        /// </summary>
        [HttpDelete("DeleteProduct/{id}")]
        [Authorize(Roles = "Seller,Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var result = await _prodService.DeleteProductAsync(id);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        /// <summary>
        /// Cập nhật số lượng sản phẩm
        /// </summary>
        [HttpPatch("UpdateProductQuantity/{id}")]
        [Authorize(Roles = "Seller,Admin")]
        public async Task<IActionResult> UpdateProductQuantity(int id, [FromQuery] int quantity)
        {
            var result = await _prodService.UpdateProductQuantityAsync(id, quantity);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
    }
}