// Models/ReturnReceipt.cs
using System;
using System.Collections.Generic;

namespace DoAnTotNghiep.Models
{
    public class ReturnReceipt
    {
        public int Id { get; set; }

        // Mã phiếu (sinh sau khi lưu DB, ví dụ "PH-00001")
        public string ReturnReceiptNumber { get; set; } = string.Empty;

        // Liên kết tới Order nếu có
        public int? RelatedOrderId { get; set; }
        public virtual Order? RelatedOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Trạng thái: "Đã đặt hàng", "Chờ phê duyệt", "Đã nhận hàng", ...
        public string Status { get; set; } = "Đã đặt hàng";

        // Thông tin phê duyệt/từ chối (giống PurchaseOrder)
        public string? RejectionReason { get; set; }
        public string? RejectedByEmail { get; set; }
        public DateTime? RejectedAt { get; set; }

        // Ai tạo phiếu (audit)
        public string? CreatedBy { get; set; }

        public virtual ICollection<ReturnReceiptItem> Items { get; set; } = new List<ReturnReceiptItem>();
    }
}
