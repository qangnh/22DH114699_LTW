using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace _22DH114699_LTW.Models.ViewModel
{
    public class CategoryMetadata
    {
        [HiddenInput]
        public int CategoryID { get; set; }
        [Required]
        [StringLength(50,MinimumLength =5 ,ErrorMessage = "Category name cannot exceed 50 characters")]
        public string CategoryName { get; set; }
    }
    
    public class UserMetadata
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(30,MinimumLength =5 ,ErrorMessage = "Username cannot exceed 50 characters")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Username can only contain letters, numbers, and underscores")]
        public string Username { get; set; }
        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        [Required(ErrorMessage = "User role is required")]
        public string UserRole { get; set; }
    }
    public class ProductMetadata
    {
        [Display(Name = "Product ID")]
        public int ProductID { get; set; }

        [Display(Name = "Category ID")]
        public int CategoryID { get; set; }

        [StringLength(50)]
        [Required(ErrorMessage = "Phải nhập tên sản phẩm")]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; }

        [Display(Name = "Mô tả sản phẩm")]
        public string ProductDescription { get; set; }
        public decimal ProductPrice { get; set; }

        [Display(Name = "Đường dẫn ảnh sản phẩm")]
        [DefaultValue("~/Content/Images/no-image.png")]

        public string ProductImage { get; set; }
    }
}