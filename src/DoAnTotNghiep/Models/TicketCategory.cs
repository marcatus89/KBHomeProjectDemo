using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DoAnTotNghiep.Models
{
    public class TicketCategory
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public virtual ICollection<Ticket> Tickets { get; set; }
    }
}
