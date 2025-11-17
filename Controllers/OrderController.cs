using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using _22DH114699_LTW.Models;
using _22DH114699_LTW.Services;

namespace _22DH114699_LTW.Controllers
{
    /// <summary>
    /// Controller qu?n lý ??n hàng
    /// </summary>
    public class OrderController : Controller
    {
        private readonly MyStoreEntities db = new MyStoreEntities();

        /// <summary>
        /// L?y d?ch v? gi? hàng
        /// </summary>
        private CartService GetCartService()
        {
            return new CartService(Session);
        }

        // GET: Order/Checkout
        /// <summary>
        /// Hi?n th? trang thanh toán
        /// </summary>
        public ActionResult Checkout()
        {
            // Ki?m tra ??ng nh?p
            if (Session["Username"] == null)
            {
                TempData["Error"] = "Vui lòng ??ng nh?p ?? ti?p t?c thanh toán.";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Checkout", "Order") });
            }

            var cartService = GetCartService();
            var cart = cartService.GetCart();

            // Ki?m tra gi? hàng tr?ng
            if (!cart.Any())
            {
                TempData["Error"] = "Gi? hàng c?a b?n ?ang tr?ng.";
                return RedirectToAction("Index", "Cart");
            }

            // L?y thông tin khách hàng
            var username = Session["Username"].ToString();
            var customer = db.Customers.SingleOrDefault(c => c.Username == username);

            if (customer == null)
            {
                TempData["Error"] = "Không tìm th?y thông tin khách hàng.";
                return RedirectToAction("Index", "Cart");
            }

            // Truy?n thông tin vào ViewBag
            ViewBag.Customer = customer;
            ViewBag.Cart = cart;
            ViewBag.TotalAmount = cartService.GetTotalAmount();

            return View();
        }

        // POST: Order/Checkout
        /// <summary>
        /// X? lý thanh toán và t?o ??n hàng
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(string addressDelivery, string paymentMethod)
        {
            try
            {
                // Ki?m tra ??ng nh?p
                if (Session["Username"] == null)
                {
                    TempData["Error"] = "Vui lòng ??ng nh?p ?? ti?p t?c.";
                    return RedirectToAction("Login", "Account");
                }

                var cartService = GetCartService();
                var cart = cartService.GetCart();

                // Ki?m tra gi? hàng tr?ng
                if (!cart.Any())
                {
                    TempData["Error"] = "Gi? hàng c?a b?n ?ang tr?ng.";
                    return RedirectToAction("Index", "Cart");
                }

                // Ki?m tra ??a ch? giao hàng
                if (string.IsNullOrWhiteSpace(addressDelivery))
                {
                    TempData["Error"] = "Vui lòng nh?p ??a ch? giao hàng.";
                    return RedirectToAction("Checkout");
                }

                // L?y thông tin khách hàng
                var username = Session["Username"].ToString();
                var customer = db.Customers.SingleOrDefault(c => c.Username == username);

                if (customer == null)
                {
                    TempData["Error"] = "Không tìm th?y thông tin khách hàng.";
                    return RedirectToAction("Index", "Cart");
                }

                // T?o ??n hàng
                var order = new Order
                {
                    CustomerID = customer.CustomerID,
                    OrderDate = DateTime.Now,
                    TotalAmount = cartService.GetTotalAmount(),
                    PaymentStatus = paymentMethod ?? "COD", // Cash On Delivery m?c ??nh
                    AddressDelivery = addressDelivery
                };

                db.Orders.Add(order);
                db.SaveChanges();

                // T?o chi ti?t ??n hàng
                foreach (var item in cart)
                {
                    var orderDetail = new OrderDetail
                    {
                        OrderID = order.OrderID,
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    };
                    db.OrderDetails.Add(orderDetail);
                }

                db.SaveChanges();

                // Xóa gi? hàng sau khi ??t hàng thành công
                cartService.ClearCart();

                TempData["Success"] = "??t hàng thành công!";
                return RedirectToAction("OrderSuccess", new { id = order.OrderID });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có l?i x?y ra khi ??t hàng: " + ex.Message;
                return RedirectToAction("Checkout");
            }
        }

        // GET: Order/OrderSuccess
        /// <summary>
        /// Xác nh?n ??n hàng sau khi thanh toán
        /// </summary>
        public ActionResult OrderSuccess(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Ki?m tra ??ng nh?p
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = db.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails.Select(od => od.Product))
                .FirstOrDefault(o => o.OrderID == id.Value);

            if (order == null)
            {
                TempData["Error"] = "Không tìm th?y ??n hàng.";
                return RedirectToAction("Index", "Home");
            }

            // Ki?m tra xem ??n hàng có thu?c v? khách hàng hi?n t?i không
            var username = Session["Username"].ToString();
            if (order.Customer.Username != username)
            {
                TempData["Error"] = "B?n không có quy?n xem ??n hàng này.";
                return RedirectToAction("Index", "Home");
            }

            return View(order);
        }

        // GET: Order/MyOrder
        /// <summary>
        /// Hi?n th? danh sách các ??n hàng ?ã ??t
        /// </summary>
        public ActionResult MyOrder()
        {
            // Ki?m tra ??ng nh?p
            if (Session["Username"] == null)
            {
                TempData["Error"] = "Vui lòng ??ng nh?p ?? xem ??n hàng c?a b?n.";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("MyOrder", "Order") });
            }

            var username = Session["Username"].ToString();
            var customer = db.Customers.SingleOrDefault(c => c.Username == username);

            if (customer == null)
            {
                TempData["Error"] = "Không tìm th?y thông tin khách hàng.";
                return RedirectToAction("Index", "Home");
            }

            // L?y danh sách ??n hàng c?a khách hàng, s?p x?p theo ngày m?i nh?t
            var orders = db.Orders
                .Include(o => o.OrderDetails.Select(od => od.Product))
                .Where(o => o.CustomerID == customer.CustomerID)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }

        // GET: Order/Details/5
        /// <summary>
        /// Hi?n th? chi ti?t ??n hàng
        /// </summary>
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Không tìm th?y ??n hàng.";
                return RedirectToAction("MyOrder");
            }

            // Ki?m tra ??ng nh?p
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = db.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails.Select(od => od.Product))
                .FirstOrDefault(o => o.OrderID == id.Value);

            if (order == null)
            {
                TempData["Error"] = "Không tìm th?y ??n hàng.";
                return RedirectToAction("MyOrder");
            }

            // Ki?m tra xem ??n hàng có thu?c v? khách hàng hi?n t?i không
            var username = Session["Username"].ToString();
            if (order.Customer.Username != username)
            {
                TempData["Error"] = "B?n không có quy?n xem ??n hàng này.";
                return RedirectToAction("MyOrder");
            }

            return View(order);
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
