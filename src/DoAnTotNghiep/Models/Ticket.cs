using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace DoAnTotNghiep.Models
{
    public class Ticket
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tiêu đề không được để trống.")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập nội dung mô tả.")]
        public string Description { get; set; } = string.Empty;

        public TicketStatus Status { get; set; } = TicketStatus.Open;
        public TicketPriority Priority { get; set; } = TicketPriority.Normal;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastUpdatedAt { get; set; } = DateTime.Now;

        public string RequesterId { get; set; } = string.Empty;
        [ForeignKey(nameof(RequesterId))]
        public virtual IdentityUser? Requester { get; set; }

        public string? AssigneeId { get; set; }
        [ForeignKey(nameof(AssigneeId))]
        public virtual IdentityUser? Assignee { get; set; }

        public int? TicketCategoryId { get; set; }
        [ForeignKey(nameof(TicketCategoryId))]
        public virtual TicketCategory? TicketCategory { get; set; }

        public virtual ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
        public virtual ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
        public virtual ICollection<TicketWatcher> Watchers { get; set; } = new List<TicketWatcher>();

        [NotMapped]
        public bool IsRead { get; set; } = true;
    }
}
