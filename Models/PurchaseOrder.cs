using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DoAnTotNghiep.Models
{
    public class PurchaseOrder
    {
        public int Id { get; set; }
        public string PurchaseOrderNumber { get; set; } = string.Empty;

        [Required]
        public int SupplierId { get; set; }
        public virtual Supplier? Supplier { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string Status { get; set; } = "Đã đặt hàng";

        public string? RejectionReason { get; set; }
        public string? RejectedByEmail { get; set; }
        public DateTime? RejectedAt { get; set; }
        public virtual ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
    }
}

