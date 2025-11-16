// Models/ReturnReceiptItem.cs
using System;

namespace DoAnTotNghiep.Models
{
    public class ReturnReceiptItem
    {
        public int Id { get; set; }

        public int ReturnReceiptId { get; set; }
        public virtual ReturnReceipt? ReturnReceipt { get; set; }

        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }

        // Số lượng đề nghị thu hồi (mặc định = số lượng trên OrderDetail khi auto-create)
        public int Quantity { get; set; }

        // Số lượng thực tế kho nhập (kho thay đổi khi thao tác)
        public int ReceivedQuantity { get; set; }

        // Đơn giá (không bắt buộc nhưng thuận tiện cho báo cáo)
        public decimal UnitPrice { get; set; }
    }
}
