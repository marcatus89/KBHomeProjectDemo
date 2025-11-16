using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace DoAnTotNghiep.Models
{
    // Model cho Phiếu giao hàng
    public class Shipment
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }
        public virtual Order? Order { get; set; }

        public string? ShippingProvider { get; set; } // Đơn vị vận chuyển (VD: GHTK)
        public string? TrackingNumber { get; set; } // Mã vận đơn

        public DateTime DispatchedDate { get; set; } = DateTime.Now; // Ngày gửi hàng
        public DateTime? DeliveredDate { get; set; } // Ngày giao thành công
    }
}

