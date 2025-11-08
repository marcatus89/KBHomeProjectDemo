using System.ComponentModel.DataAnnotations;

namespace DoAnTotNghiep.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc.")]
        public string? Name { get; set; }

        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn một danh mục.")]
        public int CategoryId { get; set; }
        
        public Category? Category { get; set; }

        
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho không thể là số âm.")]
        public int StockQuantity { get; set; }
        public bool IsVisible { get; set; } = true;

    }
}

