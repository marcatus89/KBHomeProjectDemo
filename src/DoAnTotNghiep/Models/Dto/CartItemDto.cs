// Models/Dto/CartItemDto.cs
using System.ComponentModel.DataAnnotations;

namespace DoAnTotNghiep.Models.Dto
{
    public class CartItemDto
    {
        [Required]
        public int ProductId { get; set; }

        public string? ProductName { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}
