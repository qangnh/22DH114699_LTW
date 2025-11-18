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
    /// Controller quản lý đơn hàng
    /// </summary>
    public class OrderController : Controller
    {
        private readonly MyStoreEntities db = new MyStoreEntities();

        /// <summary>
        /// Lấy dịch vụ giỏ hàng
        /// </summary>
        private CartService GetCartService()
        {
            return new CartService(Session);
        }

        // GET: Order/Checkout
        /// <summary>
        /// Hiển thị trang thanh toán
        /// </summary>
        public ActionResult Checkout()
        {
            // Kiểm tra đăng nhập
            if (Session["Username"] == null)
            {
                TempData["Error"] = "Vui lòng đăng nhập để tiếp tục thanh toán.";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Checkout", "Order") });
            }

            var cartService = GetCartService();
            var cart = cartService.GetCart();

            // Kiểm tra giỏ hàng trống
            if (!cart.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Cart");
            }

            // Lấy thông tin khách hàng
            var username = Session["Username"].ToString();
            var customer = db.Customers.SingleOrDefault(c => c.Username == username);

            if (customer == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin khách hàng.";
                return RedirectToAction("Index", "Cart");
            }

            // Truyền thông tin vào ViewBag
            ViewBag.Customer = customer;
            ViewBag.Cart = cart;
            ViewBag.TotalAmount = cartService.GetTotalAmount();

            return View();
        }

        // POST: Order/Checkout
        /// <summary>
        /// Xử lý thanh toán đơn hàng
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(string shippingAddress, string paymentMethod, string shippingMethod)
        {
            try
            {
                // Kiểm tra đăng nhập
                if (Session["Username"] == null)
                {
                    TempData["Error"] = "Vui lòng đăng nhập để tiếp tục.";
                    return RedirectToAction("Login", "Account");
                }

                var cartService = GetCartService();
                var cart = cartService.GetCart();

                // Ki?m tra gi? hàng tr?ng
                if (!cart.Any())
                {
                    TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                    return RedirectToAction("Index", "Cart");
                }

                // Kiểm tra địa chỉ giao hàng
                if (string.IsNullOrWhiteSpace(shippingAddress))
                {
                    TempData["Error"] = "Vui lòng nhập địa chỉ giao hàng.";
                    return RedirectToAction("Checkout");
                }

                // Lấy thông tin khách hàng
                var username = Session["Username"].ToString();
                var customer = db.Customers.SingleOrDefault(c => c.Username == username);

                if (customer == null)
                {
                    TempData["Error"] = "Không tìm thấy thông tin khách hàng.";
                    return RedirectToAction("Index", "Cart");
                }

                // Phí vận chuyển miễn phí cho tất cả phương thức
                decimal shippingFee = 0;

                // Tạo đơn hàng
                var order = new Order
                {
                    CustomerID = customer.CustomerID,
                    OrderDate = DateTime.Now,
                    TotalAmount = cartService.GetTotalAmount() + shippingFee,
                    PaymentStatus = paymentMethod ?? "COD", // Cash On Delivery mặc định
                    ShippingAddress = shippingAddress,
                    DeliveryMethod = shippingMethod,
                    PaymentMethod = paymentMethod
                };

                db.Orders.Add(order);
                db.SaveChanges();

                // Tạo chi tiết đơn hàng
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

                TempData["Success"] = "Đặt hàng thành công!";
                TempData["ShippingMethod"] = shippingMethod == "Express" ? "Giao hàng nhanh" : "Giao hàng tiết kiệm";
                TempData["ShippingFee"] = shippingFee;
                return RedirectToAction("OrderSuccess", new { id = order.OrderID });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi đặt hàng: " + ex.Message;
                return RedirectToAction("Checkout");
            }
        }

        // GET: Order/OrderSuccess
        /// <summary>
        /// Xác nhận đơn hàng sau khi thanh toán
        /// </summary>
        public ActionResult OrderSuccess(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Kiểm tra đăng nhập
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
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index", "Home");
            }

            // Kiểm tra xem đơn hàng có thuộc về khách hàng hiện tại không
            var username = Session["Username"].ToString();
            if (order.Customer.Username != username)
            {
                TempData["Error"] = "Bạn không có quyền xem đơn hàng này.";
                return RedirectToAction("Index", "Home");
            }

            return View(order);
        }

        // GET: Order/MyOrder
        /// <summary>
        /// Hiển thị danh sách các đơn hàng đã đặt
        /// </summary>
        public ActionResult MyOrder()
        {
            // Kiểm tra đăng nhập
            if (Session["Username"] == null)
            {
                TempData["Error"] = "Vui lòng đăng nhập để xem đơn hàng của bạn.";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("MyOrder", "Order") });
            }

            var username = Session["Username"].ToString();
            var customer = db.Customers.SingleOrDefault(c => c.Username == username);

            if (customer == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin khách hàng.";
                return RedirectToAction("Index", "Home");
            }

            // Lấy danh sách đơn hàng của khách hàng, sắp xếp theo ngày mới nhất
            var orders = db.Orders
                .Include(o => o.OrderDetails.Select(od => od.Product))
                .Where(o => o.CustomerID == customer.CustomerID)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }

        // GET: Order/Details/5
        /// <summary>
        /// Hiển thị chi tiết đơn hàng
        /// </summary>
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("MyOrder");
            }

            // Kiểm tra đăng nhập
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
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("MyOrder");
            }

            // Kiểm tra xem đơn hàng có thuộc về khách hàng hiện tại không
            var username = Session["Username"].ToString();
            if (order.Customer.Username != username)
            {
                TempData["Error"] = "Bạn không có quyền xem đơn hàng này.";
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
