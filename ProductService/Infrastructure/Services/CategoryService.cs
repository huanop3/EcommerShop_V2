using Microsoft.AspNetCore.SignalR;
using MainEcommerceService.Models.ViewModel;
using Microsoft.EntityFrameworkCore;
using ProductService.Models.dbProduct;
using ProductService.Hubs;

public interface ICategoryService
{
    Task<HTTPResponseClient<IEnumerable<CategoryVM>>> GetAllCategoriesAsync();
    Task<HTTPResponseClient<CategoryVM>> GetCategoryByIdAsync(int id);
    Task<HTTPResponseClient<string>> CreateCategoryAsync(CategoryVM category);
    Task<HTTPResponseClient<string>> UpdateCategoryAsync(int id, CategoryVM category);
    Task<HTTPResponseClient<string>> DeleteCategoryAsync(int id);
}

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly RedisHelper _cacheService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public CategoryService(
        IUnitOfWork unitOfWork,
        RedisHelper cacheService,
        IHubContext<NotificationHub> hubContext)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _hubContext = hubContext;
    }

    public async Task<HTTPResponseClient<IEnumerable<CategoryVM>>> GetAllCategoriesAsync()
    {
        var response = new HTTPResponseClient<IEnumerable<CategoryVM>>();
        try
        {
            const string cacheKey = "AllCategories";

            // Kiểm tra cache trước
            var cachedCategories = await _cacheService.GetAsync<IEnumerable<CategoryVM>>(cacheKey);
            if (cachedCategories != null)
            {
                response.Data = cachedCategories;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy danh sách danh mục từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }
            // Nếu không có trong cache, lấy từ database
            var categories = await _unitOfWork._categoryRepository.Query()
                .Where(c => c.IsDeleted == false)
                .ToListAsync();

            if (categories == null || !categories.Any())
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy danh mục nào";
                return response;
            }

            var categoryVMs = categories.Select(c => new CategoryVM
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                Description = c.Description,
                ParentCategoryId = c.ParentCategoryId,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                IsDeleted = c.IsDeleted
            }).ToList();

            // Lưu vào cache
            await _cacheService.SetAsync(cacheKey, categoryVMs, TimeSpan.FromDays(1));

            response.Data = categoryVMs;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy danh sách danh mục thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy danh sách danh mục: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<CategoryVM>> GetCategoryByIdAsync(int id)
    {
        var response = new HTTPResponseClient<CategoryVM>();
        try
        {
            var category = await _unitOfWork._categoryRepository.Query()
                .FirstOrDefaultAsync(c => c.CategoryId == id && c.IsDeleted == false);

            if (category == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy danh mục";
                return response;
            }

            var categoryVM = new CategoryVM
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                Description = category.Description,
                ParentCategoryId = category.ParentCategoryId,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt,
                IsDeleted = category.IsDeleted
            };

            response.Data = categoryVM;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy thông tin danh mục thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy thông tin danh mục: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<string>> CreateCategoryAsync(CategoryVM category)
    {
        var response = new HTTPResponseClient<string>();
        try
        {
            await _unitOfWork.BeginTransaction();

            var newCategory = new Category
            {
                CategoryName = category.CategoryName,
                Description = category.Description,
                ParentCategoryId = category.ParentCategoryId,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await _unitOfWork._categoryRepository.AddAsync(newCategory);
            await _unitOfWork.SaveChangesAsync();

            await _unitOfWork.CommitTransaction();

            // Xóa cache
            await _cacheService.DeleteByPatternAsync("AllCategories");
            await _cacheService.DeleteByPatternAsync("PagedCategories_*");

            // Gửi thông báo realtime qua SignalR
            await _hubContext.Clients.All.SendAsync("CategoryCreated", newCategory.CategoryId, newCategory.CategoryName);

            response.Success = true;
            response.StatusCode = 201;
            response.Message = "Tạo danh mục thành công";
            response.Data = "Tạo thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi tạo danh mục: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<string>> UpdateCategoryAsync(int id, CategoryVM category)
    {
        var response = new HTTPResponseClient<string>();
        try
        {
            await _unitOfWork.BeginTransaction();


            var existingCategory = await _unitOfWork._categoryRepository.GetByIdAsync(id);
            if (existingCategory == null || existingCategory.IsDeleted == true)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy danh mục";
                return response;
            }

            existingCategory.CategoryName = category.CategoryName;
            existingCategory.Description = category.Description;
            existingCategory.ParentCategoryId = category.ParentCategoryId;
            existingCategory.UpdatedAt = DateTime.UtcNow;

            _unitOfWork._categoryRepository.Update(existingCategory);
            await _unitOfWork.SaveChangesAsync();

            await _unitOfWork.CommitTransaction();

            // Xóa cache
            await _cacheService.DeleteByPatternAsync("AllCategories");
            await _cacheService.DeleteByPatternAsync("PagedCategories_*");

            // Gửi thông báo realtime qua SignalR
            await _hubContext.Clients.All.SendAsync("CategoryUpdated", id, existingCategory.CategoryName);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Cập nhật danh mục thành công";
            response.Data = "Cập nhật thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi cập nhật danh mục: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<string>> DeleteCategoryAsync(int id)
    {
        var response = new HTTPResponseClient<string>();
        try
        {
            await _unitOfWork.BeginTransaction();

            var category = await _unitOfWork._categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy danh mục";
                return response;
            }

            // Soft delete
            category.IsDeleted = true;
            category.UpdatedAt = DateTime.UtcNow;

            _unitOfWork._categoryRepository.Update(category);
            await _unitOfWork.SaveChangesAsync();

            await _unitOfWork.CommitTransaction();

            // Xóa cache
            await _cacheService.DeleteByPatternAsync("AllCategories");
            await _cacheService.DeleteByPatternAsync("PagedCategories_*");

            // Gửi thông báo realtime qua SignalR
            await _hubContext.Clients.All.SendAsync("CategoryDeleted", id);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Xóa danh mục thành công";
            response.Data = "Xóa thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi xóa danh mục: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }
}