using System.ComponentModel.DataAnnotations;

namespace DoAnTotNghiep.Models
{
    // Model lưu chi tiết một sản phẩm trong đơn nhập hàng
    public class PurchaseOrderItem
    {
        public int Id { get; set; }
        
        [Required]
        public int PurchaseOrderId { get; set; }
        public virtual PurchaseOrder? PurchaseOrder { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(1, int.MaxValue)]
        public int ReceivedQuantity { get; set; }
        
        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }
    }
}
