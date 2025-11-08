    using System.ComponentModel.DataAnnotations;
    using System.Collections.Generic;

    namespace DoAnTotNghiep.Models
    {
        public class CreateUserModel
        {
            [Required(ErrorMessage = "Email là bắt buộc.")]
            [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
            [DataType(DataType.Password)]
            [StringLength(100, ErrorMessage = "Mật khẩu phải có ít nhất {2} ký tự.", MinimumLength = 6)]
            public string Password { get; set; } = string.Empty;
            
            // Dùng để lưu các vai trò được chọn
            public List<string> SelectedRoles { get; set; } = new List<string>();
        }
    }
    
