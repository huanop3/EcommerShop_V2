using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace MainEcommerceService.Models.ViewModel
{
    namespace ViewModels.ShipmentVM
    {
        public class ShipmentVM
        {
            public int ShipmentId { get; set; }

            public int OrderId { get; set; }

            public int ShipperId { get; set; }

            public string? TrackingNumber { get; set; }

            public DateTime? ShippedDate { get; set; }

            public DateTime? DeliveredDate { get; set; }

            public string Status { get; set; } = null!;

            public DateTime? CreatedAt { get; set; }

            public DateTime? UpdatedAt { get; set; }

            public bool? IsDeleted { get; set; }
        }
        public class ShipmentDashboardVM
        {
            public OrderInfoVM OrderInfo { get; set; }
            public BuyerInfoVM BuyerInfo { get; set; }
            public ShippingAddressVM ShippingAddress { get; set; }
            public List<OrderItemVM> OrderItems { get; set; }
            public ShipmentInfoVM ShipmentInfo { get; set; }
            public List<OrderStatusOptionVM> AvailableStatusUpdates { get; set; }
        }

        public class OrderInfoVM
        {
            public int OrderId { get; set; }
            public decimal TotalAmount { get; set; }
            public DateTime OrderDate { get; set; }
            public string CurrentOrderStatus { get; set; }
            public int OrderStatusId { get; set; }
        }

        public class BuyerInfoVM
        {
            public int UserId { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string PhoneNumber { get; set; }
        }

        public class ShippingAddressVM
        {
            public int AddressId { get; set; }
            public string AddressLine1 { get; set; }
            public string AddressLine2 { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string PostalCode { get; set; }
            public string Country { get; set; }
            public string FullAddress => $"{AddressLine1}, {AddressLine2}, {City}, {State} {PostalCode}, {Country}";
        }

        public class OrderItemVM
        {
            public int OrderItemId { get; set; }
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal TotalPrice { get; set; }
        }

        public class ShipmentInfoVM
        {
            public int ShipmentId { get; set; }
            public int ShipperId { get; set; }
            public string TrackingNumber { get; set; }
            public string Status { get; set; }
            public DateTime? ShippedDate { get; set; }
            public DateTime? DeliveredDate { get; set; }
            public DateTime? CreatedDate { get; set; }
        }

        public class AssignedOrderVM
        {
            public int OrderId { get; set; }
            public string OrderCode { get; set; }
            public decimal TotalAmount { get; set; }
            public DateTime OrderDate { get; set; }
            public string BuyerName { get; set; }
            public string ShippingAddress { get; set; }
            public string CurrentStatus { get; set; }
            public int OrderStatusId { get; set; }
        }

        public class OrderStatusOptionVM
        {
            public int StatusId { get; set; }
            public string StatusName { get; set; }
            public string Description { get; set; }
        }

        public class UpdateShipmentStatusRequest
        {
            [Required]
            public int NewStatusId { get; set; }

            public string Note { get; set; }

            public DateTime? UpdatedDate { get; set; } = DateTime.Now;
        }

        public class AssignShipmentRequest
        {
            [Required]
            public int OrderId { get; set; }

            [Required]
            public int ShipperId { get; set; }

            public string? TrackingNumber { get; set; }
        }

        public class ShipmentValidationResponse
        {
            public bool IsValid { get; set; }
            public string Message { get; set; }
            public int? OrderStatusId { get; set; }
        }
    }
}