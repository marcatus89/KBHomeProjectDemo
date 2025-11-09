// Models/Dto/CreateOrderDto.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DoAnTotNghiep.Models.Dto
{
    public class CreateOrderDto
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng.")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [MinLength(1, ErrorMessage = "Giỏ hàng không thể rỗng.")]
        public List<CartItemDto> Items { get; set; } = new();
    }
}
