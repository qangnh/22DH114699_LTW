using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using _22DH114699_LTW.Models;

namespace _22DH114699_LTW.Services
{
    /// <summary>
    /// D?ch v? qu?n lý gi? hàng
    /// </summary>
    public class CartService
    {
        private readonly HttpSessionStateBase _session;
        private const string CART_SESSION_KEY = "CART";

        public CartService(HttpSessionStateBase session)
        {
            _session = session;
        }

        /// <summary>
        /// L?y gi? hàng hi?n t?i t? Session
        /// </summary>
        public List<CartItem> GetCart()
        {
            var cart = _session[CART_SESSION_KEY] as List<CartItem>;
            if (cart == null)
            {
                cart = new List<CartItem>();
                _session[CART_SESSION_KEY] = cart;
            }
            return cart;
        }

        /// <summary>
        /// Thêm s?n ph?m vào gi? hàng
        /// </summary>
        public void AddItem(int productId, string productName, decimal unitPrice, int quantity, string image)
        {
            var cart = GetCart();
            var existingItem = cart.FirstOrDefault(c => c.ProductID == productId);

            if (existingItem != null)
            {
                // N?u s?n ph?m ?ã có trong gi?, t?ng s? l??ng
                existingItem.Quantity += quantity;
            }
            else
            {
                // N?u ch?a có, thêm m?i
                cart.Add(new CartItem
                {
                    ProductID = productId,
                    ProductName = productName,
                    UnitPrice = unitPrice,
                    Quantity = quantity,
                    Image = image
                });
            }

            _session[CART_SESSION_KEY] = cart;
        }

        /// <summary>
        /// Thêm s?n ph?m vào gi? hàng t? ??i t??ng Product
        /// </summary>
        public void AddItem(Product product, int quantity = 1)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            AddItem(product.ProductID, product.ProductName, product.ProductPrice, quantity, product.ProductImage);
        }

        /// <summary>
        /// Xóa s?n ph?m kh?i gi? hàng
        /// </summary>
        public bool RemoveItem(int productId)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.ProductID == productId);
            
            if (item != null)
            {
                cart.Remove(item);
                _session[CART_SESSION_KEY] = cart;
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// C?p nh?t s? l??ng s?n ph?m trong gi? hàng
        /// </summary>
        public bool UpdateQuantity(int productId, int quantity)
        {
            if (quantity <= 0)
                return false;

            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.ProductID == productId);
            
            if (item != null)
            {
                item.Quantity = quantity;
                _session[CART_SESSION_KEY] = cart;
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Xóa toàn b? gi? hàng
        /// </summary>
        public void ClearCart()
        {
            _session[CART_SESSION_KEY] = new List<CartItem>();
        }

        /// <summary>
        /// L?y t?ng s? l??ng s?n ph?m trong gi? hàng
        /// </summary>
        public int GetTotalItems()
        {
            var cart = GetCart();
            return cart.Sum(c => c.Quantity);
        }

        /// <summary>
        /// L?y t?ng giá tr? gi? hàng
        /// </summary>
        public decimal GetTotalAmount()
        {
            var cart = GetCart();
            return cart.Sum(c => c.UnitPrice * c.Quantity);
        }

        /// <summary>
        /// Ki?m tra s?n ph?m có trong gi? hàng không
        /// </summary>
        public bool IsProductInCart(int productId)
        {
            var cart = GetCart();
            return cart.Any(c => c.ProductID == productId);
        }

        /// <summary>
        /// L?y s? l??ng c?a m?t s?n ph?m trong gi? hàng
        /// </summary>
        public int GetProductQuantity(int productId)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.ProductID == productId);
            return item?.Quantity ?? 0;
        }
    }

    /// <summary>
    /// Class ??i di?n cho m?t s?n ph?m trong gi? hàng
    /// </summary>
    public class CartItem
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string Image { get; set; }

        /// <summary>
        /// T?ng giá tr? c?a s?n ph?m này (??n giá * s? l??ng)
        /// </summary>
        public decimal TotalPrice
        {
            get { return UnitPrice * Quantity; }
        }
    }
}
