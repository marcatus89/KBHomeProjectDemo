using System;

namespace DoAnTotNghiep.Models
{
    /// <summary>
    /// Yêu cầu điều chỉnh tồn kho do Warehouse gửi — Admin sẽ Approve / Reject.
    /// </summary>
    public class InventoryAdjustmentRequest
    {
        public int Id { get; set; }

        // Thông tin sản phẩm
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;

        // Quantities
        public int OldQuantity { get; set; }
        public int RequestedQuantity { get; set; }

        // Trạng thái: Pending / Approved / Rejected
        public string Status { get; set; } = "Pending";

        // Ai gửi yêu cầu
        public string? RequestedByUserId { get; set; }
        public string? RequestedByEmail { get; set; }
        public DateTime RequestedAt { get; set; }

        // Ai duyệt / từ chối
        public string? ReviewedByUserId { get; set; }
        public string? ReviewedByEmail { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewComment { get; set; }

        // Thêm trường nếu cần (ví dụ: lý do warehouse)
        public string? RequestReason { get; set; }
    }
}
