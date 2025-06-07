using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MainEcommerceService.Models.ViewModel
{

    public class CategoryVM
    { 
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public int? ParentCategoryId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }
    }
}