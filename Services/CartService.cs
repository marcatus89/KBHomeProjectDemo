using System;
using System.Collections.Generic;
using System.Linq;
using DoAnTotNghiep.Models;

namespace DoAnTotNghiep.Services
{
    public class CartService
    {
        public event Action? OnChange;
        private readonly List<CartItem> _items = new();

        public IReadOnlyList<CartItem> Items => _items.AsReadOnly();
        public decimal Total => _items.Sum(i => i.Price * i.Quantity);

        public void AddToCart(Product product, int quantity)
        {
            Console.WriteLine($"[CartService] InstanceHash={this.GetHashCode()} AddToCart called (productId={product?.Id}, qty={quantity})");

            if (product == null) throw new ArgumentNullException(nameof(product));
            if (quantity <= 0) return;

            var existing = _items.FirstOrDefault(i => i.ProductId == product.Id);
            if (existing != null)
            {
                existing.Quantity += quantity;
                Console.WriteLine($"[CartService] Incremented product {product.Id} -> {existing.Quantity}");
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
                Console.WriteLine($"[CartService] Added new item product {product.Id} qty={quantity}");
            }

            Console.WriteLine($"[CartService] totalQty={_items.Sum(i => i.Quantity)}, distinctItems={_items.Count}");
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
            Console.WriteLine($"[CartService] InstanceHash={this.GetHashCode()} NotifyStateChanged()");
            OnChange?.Invoke();
        }
    }
}
