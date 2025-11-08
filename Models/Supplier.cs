using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace DoAnTotNghiep.Models
{
    // Model lưu thông tin nhà cung cấp
    public class Supplier
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên nhà cung cấp là bắt buộc.")]
        public string Name { get; set; } = string.Empty;

        public string? ContactPerson { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }

        public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    }
}
