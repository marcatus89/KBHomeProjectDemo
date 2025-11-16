using System.Collections.Generic;

namespace DoAnTotNghiep.Models.Dto
{
    public class PlaceOrderRequest
    {
        public string? CustomerName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ShippingAddress { get; set; }
        public List<CartItemDto> Items { get; set; } = new();
    }
}
