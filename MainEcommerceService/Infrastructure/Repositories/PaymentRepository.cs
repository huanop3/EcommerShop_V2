using MainEcommerceService.Models.dbMainEcommer;

public interface IPaymentRepository : IRepository<Payment>
{
    // Add custom methods for Payment here if needed
}

public class PaymentRepository : Repository<Payment>, IPaymentRepository
{
    public PaymentRepository(MainEcommerDBContext context) : base(context)
    {
    }
}