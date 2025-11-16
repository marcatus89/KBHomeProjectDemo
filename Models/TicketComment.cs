using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel; // nếu cần

namespace DoAnTotNghiep.Models
{
    public class TicketComment
    {
        public int Id { get; set; }

        [Required]
        public string? Content { get; set; }

        public DateTime CreatedAt { get; set; }
        public bool IsInternalNote { get; set; }

        public string? AuthorId { get; set; }
        [ForeignKey("AuthorId")]
        public virtual IdentityUser? Author { get; set; }

        public int TicketId { get; set; }
        [ForeignKey("TicketId")]
        public virtual Ticket? Ticket { get; set; }

        public virtual ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();

        // ---- MỚI: Reply (self-referencing) ----

        /// <summary>
        /// Nếu comment này là 1 reply, ReplyToCommentId trỏ đến comment cha.
        /// Nullable vì comment gốc không có parent.
        /// </summary>
        public int? ReplyToCommentId { get; set; }

        /// <summary>
        /// Navigation đến comment được reply.
        /// Đánh dấu InverseProperty để EF biết mối quan hệ 1-n giữa ReplyToComment và Replies.
        /// </summary>
        [ForeignKey("ReplyToCommentId")]
        [InverseProperty("Replies")]
        public virtual TicketComment? ReplyToComment { get; set; }

        /// <summary>
        /// Các comment reply cho comment này.
        /// </summary>
        [InverseProperty("ReplyToComment")]
        public virtual ICollection<TicketComment> Replies { get; set; } = new List<TicketComment>();
    }
}
