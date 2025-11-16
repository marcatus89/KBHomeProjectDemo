using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnTotNghiep.Models
{
    public class TicketWatcher
    {
        public int Id { get; set; }

        public int TicketId { get; set; }
        [ForeignKey(nameof(TicketId))]
        public virtual Ticket Ticket { get; set; } = default!;

        public string UserId { get; set; } = string.Empty;
        [ForeignKey(nameof(UserId))]
        public virtual IdentityUser User { get; set; } = default!;

        public bool IsRead { get; set; } = false;
    }
}
