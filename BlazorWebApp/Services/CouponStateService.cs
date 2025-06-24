// Services/CouponStateService.cs
using Microsoft.JSInterop;
using System.Text.Json;
using MainEcommerceService.Models.ViewModel;
using ProductService.Models.ViewModel;

namespace BlazorWebApp.Services
{
    public static class CouponStateService
    {
        // Static fields to store state in memory
        private static CouponVM? _appliedCoupon;
        private static PricingDataModel? _pricingData;
        private static CheckoutDataModel? _checkoutData;

        // Events for state change notifications
        public static event Action<CouponVM?>? CouponChanged;
        public static event Action<PricingDataModel?>? PricingDataChanged;
        public static event Action<CheckoutDataModel?>? CheckoutDataChanged;

        /// <summary>
        /// Save applied coupon to memory
        /// </summary>
        public static void SaveAppliedCoupon(CouponVM? coupon)
        {
            _appliedCoupon = coupon;
            CouponChanged?.Invoke(coupon);
        }

        /// <summary>
        /// Get applied coupon from memory
        /// </summary>
        public static CouponVM? GetAppliedCoupon()
        {
            return _appliedCoupon;
        }

        /// <summary>
        /// Remove applied coupon from memory
        /// </summary>
        public static void RemoveAppliedCoupon()
        {
            _appliedCoupon = null;
            CouponChanged?.Invoke(null);
        }

        /// <summary>
        /// Check if there's an applied coupon
        /// </summary>
        public static bool HasAppliedCoupon()
        {
            return _appliedCoupon != null;
        }

        /// <summary>
        /// Save pricing data to memory
        /// </summary>
        public static void SavePricingData(PricingDataModel? pricingData)
        {
            _pricingData = pricingData;
            PricingDataChanged?.Invoke(pricingData);
        }

        /// <summary>
        /// Get pricing data from memory
        /// </summary>
        public static PricingDataModel? GetPricingData()
        {
            return _pricingData;
        }

        /// <summary>
        /// Remove pricing data from memory
        /// </summary>
        public static void RemovePricingData()
        {
            _pricingData = null;
            PricingDataChanged?.Invoke(null);
        }

        /// <summary>
        /// Save checkout data to memory
        /// </summary>
        public static void SaveCheckoutData(CheckoutDataModel? checkoutData)
        {
            _checkoutData = checkoutData;
            CheckoutDataChanged?.Invoke(checkoutData);
        }

        /// <summary>
        /// Get checkout data from memory
        /// </summary>
        public static CheckoutDataModel? GetCheckoutData()
        {
            return _checkoutData;
        }

        /// <summary>
        /// Remove checkout data from memory
        /// </summary>
        public static void RemoveCheckoutData()
        {
            _checkoutData = null;
            CheckoutDataChanged?.Invoke(null);
        }

        /// <summary>
        /// Clear all data from memory
        /// </summary>
        public static void ClearAllData()
        {
            RemoveAppliedCoupon();
            RemovePricingData();
            RemoveCheckoutData();
        }

        /// <summary>
        /// Save complete order data
        /// </summary>
        public static void SaveCompleteOrderData(CouponVM? coupon, PricingDataModel? pricingData, CheckoutDataModel? checkoutData)
        {
            SaveAppliedCoupon(coupon);
            SavePricingData(pricingData);
            SaveCheckoutData(checkoutData);
        }

        /// <summary>
        /// Get complete order data
        /// </summary>
        public static CompleteOrderDataModel? GetCompleteOrderData()
        {
            if (_pricingData != null || _checkoutData != null)
            {
                return new CompleteOrderDataModel
                {
                    AppliedCoupon = _appliedCoupon,
                    PricingData = _pricingData ?? new PricingDataModel(),
                    CheckoutData = _checkoutData ?? new CheckoutDataModel()
                };
            }
            return null;
        }

        /// <summary>
        /// Validate if order data is complete and valid
        /// </summary>
        public static bool HasValidOrderData()
        {
            var orderData = GetCompleteOrderData();
            return orderData?.IsValid == true;
        }

        /// <summary>
        /// Create and save pricing data from current cart and coupon
        /// </summary>
        public static void UpdatePricingData(List<CartVM> cartItems, CouponVM? coupon, string deliveryOption)
        {
            var breakdown = PricingCalculationService.GetPricingBreakdown(cartItems, coupon, deliveryOption);
            
            var pricingData = new PricingDataModel
            {
                Subtotal = breakdown.Subtotal,
                ItemDiscounts = breakdown.ItemDiscounts,
                CouponDiscount = breakdown.CouponDiscount,
                ShippingCost = breakdown.ShippingCost,
                TaxAmount = breakdown.TaxAmount,
                TotalAmount = breakdown.TotalAmount,
                TotalSavings = breakdown.TotalSavings,
                TotalItems = breakdown.ItemCount,
                AppliedCoupon = breakdown.AppliedCoupon,
                DeliveryOption = breakdown.DeliveryOption,
                CalculatedAt = breakdown.CalculatedAt,
                IsPricingValid = true
            };

            SavePricingData(pricingData);
        }

        /// <summary>
        /// Create and save checkout data
        /// </summary>
        public static void UpdateCheckoutData(int? selectedAddressId, string? deliveryOption, string? userNotes = null)
        {
            var checkoutData = new CheckoutDataModel
            {
                SelectedAddressId = selectedAddressId,
                SelectedDeliveryOption = deliveryOption,
                UserNotes = userNotes,
                CreatedAt = DateTime.Now
            };

            SaveCheckoutData(checkoutData);
        }

        /// <summary>
        /// Get current pricing breakdown without saving
        /// </summary>
        public static PricingBreakdown GetCurrentPricingBreakdown(List<CartVM> cartItems, string deliveryOption)
        {
            return PricingCalculationService.GetPricingBreakdown(cartItems, _appliedCoupon, deliveryOption);
        }

        /// <summary>
        /// Apply coupon and update pricing automatically
        /// </summary>
        public static bool ApplyCouponWithPricing(CouponVM coupon, List<CartVM> cartItems, string deliveryOption)
        {
            if (PricingCalculationService.IsValidCoupon(coupon))
            {
                SaveAppliedCoupon(coupon);
                UpdatePricingData(cartItems, coupon, deliveryOption);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Remove coupon and update pricing automatically
        /// </summary>
        public static void RemoveCouponWithPricing(List<CartVM> cartItems, string deliveryOption)
        {
            RemoveAppliedCoupon();
            UpdatePricingData(cartItems, null, deliveryOption);
        }

        /// <summary>
        /// Update delivery option and recalculate pricing
        /// </summary>
        public static void UpdateDeliveryOption(string deliveryOption, List<CartVM> cartItems)
        {
            UpdatePricingData(cartItems, _appliedCoupon, deliveryOption);
            
            // Update checkout data if exists
            if (_checkoutData != null)
            {
                _checkoutData.SelectedDeliveryOption = deliveryOption;
                SaveCheckoutData(_checkoutData);
            }
        }

        /// <summary>
        /// Get state summary for debugging
        /// </summary>
        public static string GetStateSummary()
        {
            return $"Coupon: {(_appliedCoupon?.CouponCode ?? "None")}, " +
                   $"Pricing Valid: {(_pricingData?.IsPricingValid ?? false)}, " +
                   $"Checkout Address: {(_checkoutData?.SelectedAddressId ?? 0)}";
        }
    }

    // Supporting Models
    public class PricingDataModel
    {
        public decimal Subtotal { get; set; }
        public decimal ItemDiscounts { get; set; }
        public decimal CouponDiscount { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalSavings { get; set; }
        public int TotalItems { get; set; }
        public CouponVM? AppliedCoupon { get; set; }
        public string DeliveryOption { get; set; } = "";
        public DateTime CalculatedAt { get; set; } = DateTime.Now;
        public bool IsPricingValid { get; set; } = true;
    }

    public class CheckoutDataModel
    {
        public int? SelectedAddressId { get; set; }
        public string? SelectedDeliveryOption { get; set; }
        public string? UserNotes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // ✅ Payment information
        public string? PaymentMethod { get; set; }
        public string? DeliveryInstructions { get; set; }
        public bool TermsAccepted { get; set; }
        public bool OrderConfirmed { get; set; }
        
        // Helper properties
        public bool IsCompleteForOrder => 
            SelectedAddressId.HasValue && 
            !string.IsNullOrEmpty(SelectedDeliveryOption) && 
            !string.IsNullOrEmpty(PaymentMethod);
    }

    public class CompleteOrderDataModel
    {
        public CouponVM? AppliedCoupon { get; set; }
        public PricingDataModel PricingData { get; set; } = new();
        public CheckoutDataModel CheckoutData { get; set; } = new();
        
        public bool IsValid => 
            PricingData.IsPricingValid && 
            CheckoutData.SelectedAddressId.HasValue &&
            !string.IsNullOrEmpty(CheckoutData.SelectedDeliveryOption);
    }
}