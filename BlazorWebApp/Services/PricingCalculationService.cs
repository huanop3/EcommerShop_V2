// Services/PricingCalculationService.cs
using MainEcommerceService.Models.ViewModel;
using ProductService.Models.ViewModel;

namespace BlazorWebApp.Services
{
    public static class PricingCalculationService
    {
        // Constants for pricing rules
        public const decimal FREE_SHIPPING_THRESHOLD = 100m;
        public const decimal STANDARD_SHIPPING_COST = 5.99m;
        public const decimal EXPRESS_SHIPPING_COST = 12.99m;
        public const decimal OVERNIGHT_SHIPPING_COST = 24.99m;

        /// <summary>
        /// Calculate subtotal from cart items
        /// </summary>
        public static decimal CalculateSubtotal(List<CartVM> cartItems)
        {
            if (cartItems == null || !cartItems.Any())
                return 0m;

            return cartItems.Sum(item => item.UnitPrice * item.CartQuantity);
        }

        /// <summary>
        /// Calculate total item discounts
        /// </summary>
        public static decimal CalculateItemDiscounts(List<CartVM> cartItems)
        {
            if (cartItems == null || !cartItems.Any())
                return 0m;

            return cartItems
                .Where(item => item.HasDiscount)
                .Sum(item => (item.Price - item.UnitPrice) * item.CartQuantity);
        }

        /// <summary>
        /// Validate coupon based on actual CouponVM structure
        /// </summary>
        public static bool IsValidCoupon(CouponVM? coupon)
        {
            if (coupon == null) 
                return false;

            var now = DateTime.Now;

            // Check if coupon is active
            if (coupon.IsActive != true) 
                return false;

            // Check if coupon is not deleted
            if (coupon.IsDeleted == true)
                return false;

            // Check start date
            if (now < coupon.StartDate)
                return false;

            // Check end date
            if (now > coupon.EndDate)
                return false;

            // Check usage limit
            if (coupon.UsageLimit.HasValue && 
                coupon.UsageCount.HasValue && 
                coupon.UsageCount.Value >= coupon.UsageLimit.Value)
                return false;

            // Check if coupon has valid discount values
            bool hasValidPercent = coupon.DiscountPercent.HasValue && coupon.DiscountPercent.Value > 0;
            bool hasValidAmount = coupon.DiscountAmount.HasValue && coupon.DiscountAmount.Value > 0;
            
            if (!hasValidPercent && !hasValidAmount)
                return false;

            return true;
        }

        /// <summary>
        /// Get coupon validation message based on actual CouponVM structure
        /// </summary>
        public static string GetCouponValidationMessage(CouponVM? coupon)
        {
            if (coupon == null)
                return "Coupon not found";

            if (coupon.IsActive != true)
                return "This coupon is not active";

            if (coupon.IsDeleted == true)
                return "This coupon is no longer available";

            var now = DateTime.Now;

            if (now < coupon.StartDate)
                return $"This coupon is valid from {coupon.StartDate:MMM dd, yyyy}";

            if (now > coupon.EndDate)
                return "This coupon has expired";

            if (coupon.UsageLimit.HasValue && 
                coupon.UsageCount.HasValue && 
                coupon.UsageCount.Value >= coupon.UsageLimit.Value)
                return "This coupon has reached its usage limit";

            // Check if coupon has valid discount values with proper null checks
            bool hasValidPercent = coupon.DiscountPercent.HasValue && coupon.DiscountPercent.Value > 0;
            bool hasValidAmount = coupon.DiscountAmount.HasValue && coupon.DiscountAmount.Value > 0;
            
            if (!hasValidPercent && !hasValidAmount)
                return "This coupon has no discount value";

            // Check for invalid values only if they have values
            if (coupon.DiscountPercent.HasValue && coupon.DiscountPercent.Value <= 0)
                return "This coupon has invalid discount percentage";

            if (coupon.DiscountAmount.HasValue && coupon.DiscountAmount.Value <= 0)
                return "This coupon has invalid discount amount";

            return "Coupon is valid";
        }

        /// <summary>
        /// Get coupon discount type for display
        /// </summary>
        public static string GetCouponDiscountType(CouponVM coupon)
        {
            if (coupon.DiscountPercent.HasValue && coupon.DiscountPercent.Value > 0)
                return "percentage";
            
            if (coupon.DiscountAmount.HasValue && coupon.DiscountAmount.Value > 0)
                return "fixed";
            
            return "unknown";
        }

        /// <summary>
        /// Get coupon discount value for display
        /// </summary>
        public static decimal GetCouponDiscountValue(CouponVM coupon)
        {
            if (coupon.DiscountPercent.HasValue && coupon.DiscountPercent.Value > 0)
                return coupon.DiscountPercent.Value;
            
            if (coupon.DiscountAmount.HasValue && coupon.DiscountAmount.Value > 0)
                return coupon.DiscountAmount.Value;
            
            return 0m;
        }

        /// <summary>
        /// Format coupon display text
        /// </summary>
        public static string FormatCouponDisplay(CouponVM coupon)
        {
            if (coupon.DiscountPercent.HasValue && coupon.DiscountPercent.Value > 0)
                return $"{coupon.DiscountPercent.Value}% OFF";
            
            if (coupon.DiscountAmount.HasValue && coupon.DiscountAmount.Value > 0)
                return $"${coupon.DiscountAmount.Value:F2} OFF";
            
            return "No discount";
        }

        /// <summary>
        /// Calculate coupon discount with proper null checks
        /// </summary>
        public static decimal CalculateCouponDiscount(List<CartVM> cartItems, CouponVM? coupon)
        {
            if (coupon == null || cartItems == null || !cartItems.Any())
                return 0m;

            if (!IsValidCoupon(coupon))
                return 0m;

            var subtotal = CalculateSubtotal(cartItems);
            decimal discount = 0m;

            // Based on your CouponVM structure with proper null checks
            if (coupon.DiscountPercent.HasValue && coupon.DiscountPercent.Value > 0)
            {
                // Percentage discount
                discount = subtotal * (coupon.DiscountPercent.Value / 100m);
            }
            else if (coupon.DiscountAmount.HasValue && coupon.DiscountAmount.Value > 0)
            {
                // Fixed amount discount
                discount = coupon.DiscountAmount.Value;
            }
            else
            {
                return 0m;
            }

            // Discount cannot exceed subtotal
            return Math.Min(discount, subtotal);
        }

        /// <summary>
        /// Calculate shipping cost based on delivery option and subtotal
        /// </summary>
        public static decimal CalculateShippingCost(string deliveryOption, decimal subtotal)
        {
            if (string.IsNullOrEmpty(deliveryOption))
                deliveryOption = "standard";

            // Free standard shipping over threshold
            if (deliveryOption == "standard" && subtotal >= FREE_SHIPPING_THRESHOLD)
                return 0m;

            return deliveryOption.ToLower() switch
            {
                "express" => EXPRESS_SHIPPING_COST,
                "overnight" => OVERNIGHT_SHIPPING_COST,
                "standard" => subtotal >= FREE_SHIPPING_THRESHOLD ? 0m : STANDARD_SHIPPING_COST,
                _ => STANDARD_SHIPPING_COST
            };
        }

        /// <summary>
        /// Calculate tax amount (if applicable)
        /// </summary>
        public static decimal CalculateTax(decimal subtotal, decimal shippingCost, string? region = null)
        {
            // Implement tax calculation logic based on region if needed
            // For now, returning 0 as no tax logic was in original code
            return 0m;
        }

        /// <summary>
        /// Calculate final order total
        /// </summary>
        public static decimal CalculateOrderTotal(List<CartVM> cartItems, CouponVM? coupon, string deliveryOption)
        {
            var subtotal = CalculateSubtotal(cartItems);
            var itemDiscounts = CalculateItemDiscounts(cartItems);
            var couponDiscount = CalculateCouponDiscount(cartItems, coupon);
            var shippingCost = CalculateShippingCost(deliveryOption, subtotal);
            var taxAmount = CalculateTax(subtotal, shippingCost);

            return subtotal - itemDiscounts - couponDiscount + shippingCost + taxAmount;
        }

        /// <summary>
        /// Calculate total savings (discounts)
        /// </summary>
        public static decimal CalculateTotalSavings(List<CartVM> cartItems, CouponVM? coupon)
        {
            var itemDiscounts = CalculateItemDiscounts(cartItems);
            var couponDiscount = CalculateCouponDiscount(cartItems, coupon);
            return itemDiscounts + couponDiscount;
        }

        /// <summary>
        /// Get complete pricing breakdown
        /// </summary>
        public static PricingBreakdown GetPricingBreakdown(List<CartVM> cartItems, CouponVM? coupon, string deliveryOption)
        {
            var subtotal = CalculateSubtotal(cartItems);
            var itemDiscounts = CalculateItemDiscounts(cartItems);
            var couponDiscount = CalculateCouponDiscount(cartItems, coupon);
            var shippingCost = CalculateShippingCost(deliveryOption, subtotal);
            var taxAmount = CalculateTax(subtotal, shippingCost);
            var totalAmount = subtotal - couponDiscount + shippingCost + taxAmount;

            return new PricingBreakdown
            {
                Subtotal = subtotal,
                ItemDiscounts = itemDiscounts,
                CouponDiscount = couponDiscount,
                ShippingCost = shippingCost,
                TaxAmount = taxAmount,
                TotalAmount = totalAmount,
                TotalSavings = itemDiscounts + couponDiscount,
                IsFreeShipping = shippingCost == 0m && deliveryOption == "standard",
                FreeShippingEligible = subtotal >= FREE_SHIPPING_THRESHOLD,
                AmountToFreeShipping = Math.Max(0, FREE_SHIPPING_THRESHOLD - subtotal),
                ItemCount = cartItems?.Count ?? 0,
                TotalQuantity = cartItems?.Sum(x => x.CartQuantity) ?? 0,
                AppliedCoupon = coupon,
                DeliveryOption = deliveryOption,
                CalculatedAt = DateTime.Now
            };
        }

        /// <summary>
        /// Check if order qualifies for free shipping
        /// </summary>
        public static bool QualifiesForFreeShipping(decimal subtotal, string deliveryOption)
        {
            return deliveryOption == "standard" && subtotal >= FREE_SHIPPING_THRESHOLD;
        }

        /// <summary>
        /// Get shipping options with costs
        /// </summary>
        public static List<ShippingOption> GetShippingOptions(decimal subtotal)
        {
            return new List<ShippingOption>
            {
                new ShippingOption
                {
                    Code = "standard",
                    Name = "Standard Delivery",
                    Description = "5-7 business days",
                    Cost = subtotal >= FREE_SHIPPING_THRESHOLD ? 0m : STANDARD_SHIPPING_COST,
                    IsFree = subtotal >= FREE_SHIPPING_THRESHOLD,
                    EstimatedDays = "5-7"
                },
                new ShippingOption
                {
                    Code = "express",
                    Name = "Express Delivery",
                    Description = "2-3 business days",
                    Cost = EXPRESS_SHIPPING_COST,
                    IsFree = false,
                    EstimatedDays = "2-3"
                },
                new ShippingOption
                {
                    Code = "overnight",
                    Name = "Overnight Delivery",
                    Description = "Next business day",
                    Cost = OVERNIGHT_SHIPPING_COST,
                    IsFree = false,
                    EstimatedDays = "1"
                }
            };
        }
    }

    // Supporting Models (simplified based on actual needs)
    public class PricingBreakdown
    {
        public decimal Subtotal { get; set; }
        public decimal ItemDiscounts { get; set; }
        public decimal CouponDiscount { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalSavings { get; set; }
        public bool IsFreeShipping { get; set; }
        public bool FreeShippingEligible { get; set; }
        public decimal AmountToFreeShipping { get; set; }
        public int ItemCount { get; set; }
        public int TotalQuantity { get; set; }
        public CouponVM? AppliedCoupon { get; set; }
        public string DeliveryOption { get; set; } = "";
        public DateTime CalculatedAt { get; set; }
    }

    public class ShippingOption
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Cost { get; set; }
        public bool IsFree { get; set; }
        public string EstimatedDays { get; set; } = "";
    }
}