namespace DoAnTotNghiep.Models
{
    public enum TicketStatus
    {
        Open, //Ticket vừa được tạo (từ khách hàng hoặc hệ thống) và chưa có ai bắt tay xử lý.
        InProgress, //Ticket đang được xử lý bởi nhân viên.
        Resolved, //Ticket đã được xử lý và chờ các đối tượng liên quan xác nhận.
        Closed //Ticket đã đóng và không còn hiệu lực.
    }

    public enum TicketPriority
    {
        Low, //Ưu tiên thấp, có thể xử lý sau.
        Normal, //Ưu tiên bình thường, xử lý trong thời gian hợp lý.
        High, //Ưu tiên cao, cần xử lý ngay.
        Urgent //Ưu tiên khẩn cấp, cần xử lý ngay lập tức.
    }
}
