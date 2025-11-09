// Services/CartService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using DoAnTotNghiep.Models;
using Microsoft.Extensions.Logging;

namespace DoAnTotNghiep.Services
{
    public class CartService
    {
        public event Action? OnChange;
        private readonly List<CartItem> _items = new();
        private readonly ILogger<CartService> _logger;

        public IReadOnlyList<CartItem> Items => _items.AsReadOnly();
        public decimal Total => _items.Sum(i => i.Price * i.Quantity);

        public CartService(ILogger<CartService> logger)
        {
            _logger = logger;
        }

        public void AddToCart(Product product, int quantity)
        {
            _logger.LogDebug("[CartService] InstanceHash={Hash} AddToCart called (productId={ProductId}, qty={Qty})",
                this.GetHashCode(), product?.Id, quantity);

            if (product == null) throw new ArgumentNullException(nameof(product));
            if (quantity <= 0) return;

            var existing = _items.FirstOrDefault(i => i.ProductId == product.Id);
            if (existing != null)
            {
                existing.Quantity += quantity;
                _logger.LogDebug("[CartService] Incremented product {Pid} -> {Qty}", product.Id, existing.Quantity);
            }
            else
            {
                var item = new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = quantity
                };
                _items.Add(item);
                _logger.LogDebug("[CartService] Added new item product {Pid} qty={Qty}", product.Id, quantity);
            }

            _logger.LogDebug("[CartService] totalQty={TotalQty}, distinctItems={Distinct}", _items.Sum(i => i.Quantity), _items.Count);
            NotifyStateChanged();
        }

        public void AddToCart(Product product) => AddToCart(product, 1);

        public void UpdateQuantity(int productId, int quantity)
        {
            var item = _items.FirstOrDefault(i => i.ProductId == productId);
            if (item == null) return;
            if (quantity <= 0) _items.Remove(item);
            else item.Quantity = quantity;
            NotifyStateChanged();
        }

        public void RemoveFromCart(int productId)
        {
            var item = _items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                _items.Remove(item);
                NotifyStateChanged();
            }
        }

        public void ClearCart()
        {
            _items.Clear();
            NotifyStateChanged();
        }

        private void NotifyStateChanged()
        {
            _logger.LogDebug("[CartService] InstanceHash={Hash} NotifyStateChanged()", this.GetHashCode());
            OnChange?.Invoke();
        }
    }
}
