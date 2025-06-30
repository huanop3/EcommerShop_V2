using Microsoft.AspNetCore.SignalR;
using MainEcommerceService.Hubs;
using MainEcommerceService.Helper;
using MainEcommerceService.Models.dbMainEcommer;
using MainEcommerceService.Models.ViewModel;
using Microsoft.EntityFrameworkCore;
using MainEcommerceService.Kafka;

public interface IPaymentService
{
    Task<HTTPResponseClient<IEnumerable<PaymentVM>>> GetAllPayments();
    Task<HTTPResponseClient<PaymentVM>> GetPaymentById(int paymentId);
    Task<HTTPResponseClient<IEnumerable<PaymentVM>>> GetPaymentsByOrderId(int orderId);
    Task<HTTPResponseClient<bool>> CreatePayment(PaymentVM paymentVM);
    Task<HTTPResponseClient<bool>> UpdatePayment(PaymentVM paymentVM);
    Task<HTTPResponseClient<bool>> DeletePayment(int paymentId);
    Task<HTTPResponseClient<bool>> UpdatePaymentStatus(int paymentId, string status);
    Task<HTTPResponseClient<IEnumerable<PaymentVM>>> GetPaymentsByStatus(string status);
}

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly RedisHelper _cacheService;
    private readonly IHubContext<NotificationHub> _hubContext;
    public PaymentService(
        IUnitOfWork unitOfWork,
        RedisHelper cacheService,
        IHubContext<NotificationHub> hubContext)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _hubContext = hubContext;
    }

    public async Task<HTTPResponseClient<IEnumerable<PaymentVM>>> GetAllPayments()
    {
        var response = new HTTPResponseClient<IEnumerable<PaymentVM>>();
        try
        {
            const string cacheKey = "AllPayments";
            var cachedPayments = await _cacheService.GetAsync<IEnumerable<PaymentVM>>(cacheKey);
            if (cachedPayments != null)
            {
                response.Data = cachedPayments;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy danh sách thanh toán từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var payments = await _unitOfWork._paymentRepository.Query()
                .Where(p => p.IsDeleted == false)
                .ToListAsync();

            if (payments == null || !payments.Any())
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy thanh toán nào";
                return response;
            }

            var paymentVMs = payments.Select(p => new PaymentVM
            {
                PaymentId = p.PaymentId,
                OrderId = p.OrderId,
                PaymentMethod = p.PaymentMethod,
                TransactionId = p.TransactionId,
                PaymentDate = p.PaymentDate,
                Amount = p.Amount,
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                IsDeleted = p.IsDeleted
            }).ToList();

            await _cacheService.SetAsync(cacheKey, paymentVMs, TimeSpan.FromDays(1));

            response.Data = paymentVMs;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy danh sách thanh toán thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy danh sách thanh toán: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<PaymentVM>> GetPaymentById(int paymentId)
    {
        var response = new HTTPResponseClient<PaymentVM>();
        try
        {
            string cacheKey = $"Payment_{paymentId}";
            var cachedPayment = await _cacheService.GetAsync<PaymentVM>(cacheKey);
            if (cachedPayment != null)
            {
                response.Data = cachedPayment;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy thông tin thanh toán từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var payment = await _unitOfWork._paymentRepository.Query()
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId && p.IsDeleted == false);

            if (payment == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy thanh toán";
                return response;
            }

            var paymentVM = new PaymentVM
            {
                PaymentId = payment.PaymentId,
                OrderId = payment.OrderId,
                PaymentMethod = payment.PaymentMethod,
                TransactionId = payment.TransactionId,
                PaymentDate = payment.PaymentDate,
                Amount = payment.Amount,
                Status = payment.Status,
                CreatedAt = payment.CreatedAt,
                UpdatedAt = payment.UpdatedAt,
                IsDeleted = payment.IsDeleted
            };

            await _cacheService.SetAsync(cacheKey, paymentVM, TimeSpan.FromDays(1));

            response.Data = paymentVM;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy thông tin thanh toán thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy thông tin thanh toán: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<IEnumerable<PaymentVM>>> GetPaymentsByOrderId(int orderId)
    {
        var response = new HTTPResponseClient<IEnumerable<PaymentVM>>();
        try
        {
            string cacheKey = $"PaymentsByOrder_{orderId}";
            var cachedPayments = await _cacheService.GetAsync<IEnumerable<PaymentVM>>(cacheKey);
            if (cachedPayments != null)
            {
                response.Data = cachedPayments;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy danh sách thanh toán theo đơn hàng từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var payments = await _unitOfWork._paymentRepository.Query()
                .Where(p => p.OrderId == orderId && p.IsDeleted == false)
                .ToListAsync();

            var paymentVMs = payments.Select(p => new PaymentVM
            {
                PaymentId = p.PaymentId,
                OrderId = p.OrderId,
                PaymentMethod = p.PaymentMethod,
                TransactionId = p.TransactionId,
                PaymentDate = p.PaymentDate,
                Amount = p.Amount,
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                IsDeleted = p.IsDeleted
            }).ToList();

            await _cacheService.SetAsync(cacheKey, paymentVMs, TimeSpan.FromDays(1));

            response.Data = paymentVMs;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy danh sách thanh toán theo đơn hàng thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy danh sách thanh toán theo đơn hàng: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    public async Task<HTTPResponseClient<bool>> CreatePayment(PaymentVM paymentVM)
    {
        var response = new HTTPResponseClient<bool>();
        try
        {
            await _unitOfWork.BeginTransaction();

            // Kiểm tra đơn hàng có tồn tại không
            var order = await _unitOfWork._orderRepository.GetByIdAsync(paymentVM.OrderId);
            if (order == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy đơn hàng";
                return response;
            }

            var payment = new Payment
            {
                OrderId = paymentVM.OrderId,
                PaymentMethod = paymentVM.PaymentMethod,
                TransactionId = paymentVM.TransactionId,
                PaymentDate = paymentVM.PaymentDate,
                Amount = paymentVM.Amount,
                Status = paymentVM.Status,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsDeleted = false
            };

            await _unitOfWork._paymentRepository.AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            await InvalidateAllPaymentCaches(payment.PaymentId, paymentVM.OrderId);

            await _hubContext.Clients.All.SendAsync("PaymentCreated", payment.PaymentId, payment.OrderId);

            response.Success = true;
            response.StatusCode = 201;
            response.Message = "Tạo thanh toán thành công";
            response.Data = true;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi tạo thanh toán: {ex.Message}";
        }
        return response;
    }

    public async Task<HTTPResponseClient<bool>> UpdatePayment(PaymentVM paymentVM)
    {
        var response = new HTTPResponseClient<bool>();
        try
        {
            await _unitOfWork.BeginTransaction();

            var payment = await _unitOfWork._paymentRepository.GetByIdAsync(paymentVM.PaymentId);
            if (payment == null || payment.IsDeleted == true)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy thanh toán";
                return response;
            }

            payment.PaymentMethod = paymentVM.PaymentMethod;
            payment.TransactionId = paymentVM.TransactionId;
            payment.PaymentDate = paymentVM.PaymentDate;
            payment.Amount = paymentVM.Amount;
            payment.Status = paymentVM.Status;
            payment.UpdatedAt = DateTime.Now;

            _unitOfWork._paymentRepository.Update(payment);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            await InvalidateAllPaymentCaches(paymentVM.PaymentId, payment.OrderId);

            await _hubContext.Clients.All.SendAsync("PaymentUpdated", payment.PaymentId, payment.OrderId);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Cập nhật thanh toán thành công";
            response.Data = true;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi cập nhật thanh toán: {ex.Message}";
        }
        return response;
    }

    public async Task<HTTPResponseClient<bool>> DeletePayment(int paymentId)
    {
        var response = new HTTPResponseClient<bool>();
        try
        {
            await _unitOfWork.BeginTransaction();

            var payment = await _unitOfWork._paymentRepository.GetByIdAsync(paymentId);
            if (payment == null || payment.IsDeleted == true)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy thanh toán";
                return response;
            }

            payment.IsDeleted = true;
            payment.UpdatedAt = DateTime.Now;
            _unitOfWork._paymentRepository.Update(payment);
            
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            await InvalidateAllPaymentCaches(paymentId, payment.OrderId);

            await _hubContext.Clients.All.SendAsync("PaymentDeleted", paymentId);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Xóa thanh toán thành công";
            response.Data = true;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi xóa thanh toán: {ex.Message}";
        }
        return response;
    }

    public async Task<HTTPResponseClient<bool>> UpdatePaymentStatus(int paymentId, string status)
    {
        var response = new HTTPResponseClient<bool>();
        try
        {
            await _unitOfWork.BeginTransaction();

            var payment = await _unitOfWork._paymentRepository.GetByIdAsync(paymentId);
            if (payment == null || payment.IsDeleted == true)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Không tìm thấy thanh toán";
                return response;
            }

            payment.Status = status;
            payment.UpdatedAt = DateTime.Now;
            _unitOfWork._paymentRepository.Update(payment);
            
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransaction();

            await InvalidateAllPaymentCaches(paymentId, payment.OrderId);

            await _hubContext.Clients.All.SendAsync("PaymentStatusUpdated", paymentId, status);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Cập nhật trạng thái thanh toán thành công";
            response.Data = true;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransaction();
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi cập nhật trạng thái thanh toán: {ex.Message}";
        }
        return response;
    }

    public async Task<HTTPResponseClient<IEnumerable<PaymentVM>>> GetPaymentsByStatus(string status)
    {
        var response = new HTTPResponseClient<IEnumerable<PaymentVM>>();
        try
        {
            string cacheKey = $"PaymentsByStatus_{status}";
            var cachedPayments = await _cacheService.GetAsync<IEnumerable<PaymentVM>>(cacheKey);
            if (cachedPayments != null)
            {
                response.Data = cachedPayments;
                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Lấy danh sách thanh toán theo trạng thái từ cache thành công";
                response.DateTime = DateTime.Now;
                return response;
            }

            var payments = await _unitOfWork._paymentRepository.Query()
                .Where(p => p.Status == status && p.IsDeleted == false)
                .ToListAsync();

            var paymentVMs = payments.Select(p => new PaymentVM
            {
                PaymentId = p.PaymentId,
                OrderId = p.OrderId,
                PaymentMethod = p.PaymentMethod,
                TransactionId = p.TransactionId,
                PaymentDate = p.PaymentDate,
                Amount = p.Amount,
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                IsDeleted = p.IsDeleted
            }).ToList();

            await _cacheService.SetAsync(cacheKey, paymentVMs, TimeSpan.FromDays(1));

            response.Data = paymentVMs;
            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Lấy danh sách thanh toán theo trạng thái thành công";
            response.DateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.StatusCode = 500;
            response.Message = $"Lỗi khi lấy danh sách thanh toán theo trạng thái: {ex.Message}";
            response.DateTime = DateTime.Now;
        }
        return response;
    }

    private async Task InvalidateAllPaymentCaches(int paymentId, int orderId)
    {
        var cacheKeys = new[]
        {
            "AllPayments",
            $"Payment_{paymentId}",
            $"PaymentsByOrder_{orderId}"
        };

        var tasks = cacheKeys.Select(key => _cacheService.DeleteByPatternAsync(key));
        await Task.WhenAll(tasks);
    }
}