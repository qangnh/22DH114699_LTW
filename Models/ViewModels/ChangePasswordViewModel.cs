using System.ComponentModel.DataAnnotations;

namespace _22DH114699_LTW.Models.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nh?p m?t kh?u hi?n t?i")]
        [DataType(DataType.Password)]
        [Display(Name = "M?t kh?u hi?n t?i")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng nh?p m?t kh?u m?i")]
        [StringLength(100, ErrorMessage = "M?t kh?u ph?i có ít nh?t {2} ký t?", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "M?t kh?u m?i")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng xác nh?n m?t kh?u m?i")]
        [DataType(DataType.Password)]
        [Display(Name = "Xác nh?n m?t kh?u m?i")]
        [Compare("NewPassword", ErrorMessage = "M?t kh?u xác nh?n không kh?p")]
        public string ConfirmPassword { get; set; }
    }
}
