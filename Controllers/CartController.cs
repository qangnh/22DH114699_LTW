using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using _22DH114699_LTW.Models;
using _22DH114699_LTW.Services;

namespace _22DH114699_LTW.Controllers
{
    /// <summary>
    /// Controller qu?n lý gi? hàng
    /// </summary>
    public class CartController : Controller
    {
        private readonly MyStoreEntities db = new MyStoreEntities();

        /// <summary>
        /// L?y d?ch v? gi? hàng
        /// </summary>
        private CartService GetCartService()
        {
            return new CartService(Session);
        }

        // GET: Cart/Index
        /// <summary>
        /// Hi?n th? gi? hàng
        /// </summary>
        public ActionResult Index()
        {
            var cartService = GetCartService();
            var cart = cartService.GetCart();

            // L?y danh sách s?n ph?m g?i ý
            List<Product> suggestedProducts = new List<Product>();

            if (cart.Any())
            {
                // L?y danh sách ProductID trong gi? hàng
                var cartProductIds = cart.Select(c => c.ProductID).ToList();

                // L?y danh m?c c?a các s?n ph?m trong gi? hàng
                var categoryIds = db.Products
                    .Where(p => cartProductIds.Contains(p.ProductID))
                    .Select(p => p.CategoryID)
                    .Distinct()
                    .ToList();

                // L?y s?n ph?m t??ng t? (cùng danh m?c, không có trong gi? hàng)
                suggestedProducts = db.Products
                    .Include(p => p.Category)
                    .Where(p => categoryIds.Contains(p.CategoryID) && !cartProductIds.Contains(p.ProductID))
                    .OrderByDescending(p => p.ProductID)
                    .Take(12)
                    .ToList();
            }
            else
            {
                // N?u gi? hàng tr?ng, hi?n th? s?n ph?m m?i nh?t
                suggestedProducts = db.Products
                    .Include(p => p.Category)
                    .OrderByDescending(p => p.ProductID)
                    .Take(12)
                    .ToList();
            }

            ViewBag.SuggestedProducts = suggestedProducts;
            ViewBag.TotalItems = cartService.GetTotalItems();
            ViewBag.TotalAmount = cartService.GetTotalAmount();

            return View(cart);
        }

        // POST: Cart/AddItem
        /// <summary>
        /// Thêm s?n ph?m vào gi? hàng
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddItem(int productId, int quantity = 1)
        {
            try
            {
                if (quantity <= 0)
                {
                    TempData["Error"] = "S? l??ng ph?i l?n h?n 0.";
                    return RedirectToAction("Index", "Home");
                }

                var product = db.Products.Find(productId);
                if (product == null)
                {
                    TempData["Error"] = "Không tìm th?y s?n ph?m.";
                    return RedirectToAction("Index", "Home");
                }

                var cartService = GetCartService();
                cartService.AddItem(product, quantity);

                TempData["Success"] = $"?ã thêm {quantity} s?n ph?m '{product.ProductName}' vào gi? hàng.";
                
                // Ki?m tra n?u có tham s? returnUrl
                var returnUrl = Request.UrlReferrer?.ToString();
                if (!string.IsNullOrEmpty(returnUrl) && returnUrl.Contains("/ProductDetail/"))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có l?i x?y ra: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: Cart/RemoveItem
        /// <summary>
        /// Xóa s?n ph?m kh?i gi? hàng
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveItem(int productId)
        {
            try
            {
                var cartService = GetCartService();
                var cart = cartService.GetCart();
                var item = cart.FirstOrDefault(c => c.ProductID == productId);

                if (cartService.RemoveItem(productId))
                {
                    TempData["Success"] = $"?ã xóa s?n ph?m '{item?.ProductName}' kh?i gi? hàng.";
                }
                else
                {
                    TempData["Error"] = "Không tìm th?y s?n ph?m trong gi? hàng.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có l?i x?y ra: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // POST: Cart/UpdateQuantity
        /// <summary>
        /// C?p nh?t s? l??ng s?n ph?m trong gi? hàng
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateQuantity(int productId, int quantity)
        {
            try
            {
                if (quantity <= 0)
                {
                    TempData["Error"] = "S? l??ng ph?i l?n h?n 0.";
                    return RedirectToAction("Index");
                }

                var cartService = GetCartService();
                
                if (cartService.UpdateQuantity(productId, quantity))
                {
                    TempData["Success"] = "?ã c?p nh?t s? l??ng s?n ph?m.";
                }
                else
                {
                    TempData["Error"] = "Không tìm th?y s?n ph?m trong gi? hàng.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có l?i x?y ra: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // POST: Cart/ClearCart
        /// <summary>
        /// Xóa toàn b? gi? hàng
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ClearCart()
        {
            try
            {
                var cartService = GetCartService();
                cartService.ClearCart();
                TempData["Success"] = "?ã xóa toàn b? gi? hàng.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có l?i x?y ra: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // GET: Cart/GetCartCount
        /// <summary>
        /// L?y s? l??ng s?n ph?m trong gi? hàng (dùng cho AJAX)
        /// </summary>
        public JsonResult GetCartCount()
        {
            var cartService = GetCartService();
            return Json(new
            {
                count = cartService.GetTotalItems(),
                amount = cartService.GetTotalAmount()
            }, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
