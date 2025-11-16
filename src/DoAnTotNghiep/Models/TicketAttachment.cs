using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace DoAnTotNghiep.Models
{
    public class TicketAttachment
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required, StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        public string? UploaderId { get; set; }
        [ForeignKey(nameof(UploaderId))]
        public virtual IdentityUser? Uploader { get; set; }

        public int TicketId { get; set; }
        [ForeignKey(nameof(TicketId))]
        public virtual Ticket Ticket { get; set; } = default!;

        public int? CommentId { get; set; }
        [ForeignKey(nameof(CommentId))]
        public virtual TicketComment? Comment { get; set; }
    }
}
