namespace DoAnTotNghiep.Models.Dto
{
    public class PlaceOrderResponse
    {
        public bool Success { get; set; }
        public int? OrderId { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
