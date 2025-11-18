using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;
using PayPal.Api;
using _22DH114699_LTW.Services;
using System.Data.Entity;
using DbOrder = _22DH114699_LTW.Models.Order;
using DbOrderDetail = _22DH114699_LTW.Models.OrderDetail;
using DbCustomer = _22DH114699_LTW.Models.Customer;
using DbContext = _22DH114699_LTW.Models.MyStoreEntities;

namespace _22DH114699_LTW.Controllers
{
    /// <summary>
    /// Controller x? lý thanh toán PayPal
    /// </summary>
    public class PayPalController : Controller
    {
        private readonly DbContext db = new DbContext();

        /// <summary>
        /// L?y d?ch v? gi? hàng
        /// </summary>
        private CartService GetCartService()
        {
            return new CartService(Session);
        }

        /// <summary>
        /// L?y APIContext c?a PayPal
        /// </summary>
        private APIContext GetAPIContext()
        {
            try
            {
                var clientId = ConfigurationManager.AppSettings["PayPalClientId"];
                var clientSecret = ConfigurationManager.AppSettings["PayPalClientSecret"];
                var mode = ConfigurationManager.AppSettings["PayPalMode"];

                // Ki?m tra config
                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                {
                    throw new Exception("PayPal credentials ch?a ???c c?u hình trong Web.config");
                }

                var config = new Dictionary<string, string>
                {
                    { "mode", mode ?? "sandbox" }
                };

                var accessToken = new OAuthTokenCredential(clientId, clientSecret, config).GetAccessToken();
                var apiContext = new APIContext(accessToken)
                {
                    Config = config
                };

                return apiContext;
            }
            catch (Exception ex)
            {
                throw new Exception("Không th? k?t n?i PayPal: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// T?o thanh toán PayPal
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreatePayment(string shippingAddress, string shippingMethod)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== CreatePayment START ===");
                System.Diagnostics.Debug.WriteLine("ShippingAddress: " + shippingAddress);
                System.Diagnostics.Debug.WriteLine("ShippingMethod: " + shippingMethod);

                // Ki?m tra ??ng nh?p
                if (Session["Username"] == null)
                {
                    TempData["Error"] = "Vui lòng ??ng nh?p ?? ti?p t?c thanh toán.";
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
                if (string.IsNullOrWhiteSpace(shippingAddress))
                {
                    TempData["Error"] = "Vui lòng nh?p ??a ch? giao hàng.";
                    return RedirectToAction("Checkout", "Order");
                }

                // L?y thông tin khách hàng
                var username = Session["Username"].ToString();
                var customer = db.Customers.SingleOrDefault(c => c.Username == username);

                if (customer == null)
                {
                    TempData["Error"] = "Không tìm th?y thông tin khách hàng.";
                    return RedirectToAction("Index", "Cart");
                }

                // L?u thông tin vào Session ?? s? d?ng sau khi PayPal redirect v?
                Session["ShippingAddress"] = shippingAddress;
                Session["ShippingMethod"] = shippingMethod;

                System.Diagnostics.Debug.WriteLine("Getting PayPal API Context...");
                var apiContext = GetAPIContext();

                // Tính t?ng ti?n (VND)
                decimal totalAmountVND = cartService.GetTotalAmount();
                
                // Chuy?n ??i VND sang USD (t? giá ??c tính 1 USD = 24,000 VND)
                decimal exchangeRate = 24000m;
                decimal totalAmountUSD = Math.Round(totalAmountVND / exchangeRate, 2);

                System.Diagnostics.Debug.WriteLine("Total VND: " + totalAmountVND);
                System.Diagnostics.Debug.WriteLine("Total USD: " + totalAmountUSD);

                // T?o danh sách items cho PayPal
                var itemList = new ItemList
                {
                    items = new List<Item>()
                };

                foreach (var item in cart)
                {
                    decimal itemPriceUSD = Math.Round(item.UnitPrice / exchangeRate, 2);
                    
                    itemList.items.Add(new Item
                    {
                        name = item.ProductName,
                        currency = "USD",
                        price = itemPriceUSD.ToString("F2"),
                        quantity = item.Quantity.ToString(),
                        sku = item.ProductID.ToString()
                    });
                }

                // T?o payment details
                var amount = new Amount
                {
                    currency = "USD",
                    total = totalAmountUSD.ToString("F2"),
                    details = new Details
                    {
                        subtotal = totalAmountUSD.ToString("F2"),
                        shipping = "0.00",
                        tax = "0.00"
                    }
                };

                var transactionList = new List<Transaction>
                {
                    new Transaction
                    {
                        description = $"MyStore Order - {customer.CustomerName}",
                        invoice_number = Guid.NewGuid().ToString(),
                        amount = amount,
                        item_list = itemList
                    }
                };

                // T?o URLs redirect
                var baseUrl = Request.Url.Scheme + "://" + Request.Url.Authority;
                var payer = new Payer { payment_method = "paypal" };

                var redirUrls = new RedirectUrls
                {
                    cancel_url = baseUrl + "/PayPal/CancelPayment",
                    return_url = baseUrl + "/PayPal/ExecutePayment"
                };

                System.Diagnostics.Debug.WriteLine("Cancel URL: " + redirUrls.cancel_url);
                System.Diagnostics.Debug.WriteLine("Return URL: " + redirUrls.return_url);

                // T?o payment
                var payment = new Payment
                {
                    intent = "sale",
                    payer = payer,
                    transactions = transactionList,
                    redirect_urls = redirUrls
                };

                System.Diagnostics.Debug.WriteLine("Creating PayPal payment...");
                var createdPayment = payment.Create(apiContext);
                System.Diagnostics.Debug.WriteLine("PayPal payment created: " + createdPayment.id);

                // L?y approval URL
                var approvalUrl = createdPayment.links.FirstOrDefault(x => x.rel.Equals("approval_url", StringComparison.OrdinalIgnoreCase));

                if (approvalUrl != null)
                {
                    // L?u payment ID vào session
                    Session["PaymentID"] = createdPayment.id;
                    
                    System.Diagnostics.Debug.WriteLine("Redirecting to: " + approvalUrl.href);
                    System.Diagnostics.Debug.WriteLine("=== CreatePayment END ===");
                    
                    // Redirect ??n PayPal ?? thanh toán
                    return Redirect(approvalUrl.href);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: No approval URL found");
                    TempData["Error"] = "Không th? t?o thanh toán PayPal. Vui lòng th? l?i.";
                    return RedirectToAction("Checkout", "Order");
                }
            }
            catch (PayPal.PayPalException ex)
            {
                System.Diagnostics.Debug.WriteLine("PayPal Exception: " + ex.Message);
                TempData["Error"] = "L?i PayPal: " + ex.Message;
                return RedirectToAction("Checkout", "Order");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("General Exception: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack Trace: " + ex.StackTrace);
                TempData["Error"] = "Có l?i x?y ra: " + ex.Message;
                return RedirectToAction("Checkout", "Order");
            }
        }

        /// <summary>
        /// X? lý sau khi PayPal redirect v? (success)
        /// </summary>
        public ActionResult ExecutePayment(string paymentId, string token, string PayerID)
        {
            try
            {
                var apiContext = GetAPIContext();

                // L?y payment t? PayPal
                var paymentExecution = new PaymentExecution { payer_id = PayerID };
                var payment = new Payment { id = paymentId };

                // Th?c thi payment
                var executedPayment = payment.Execute(apiContext, paymentExecution);

                // Ki?m tra tr?ng thái thanh toán
                if (executedPayment.state.ToLower() != "approved")
                {
                    TempData["Error"] = "Thanh toán không ???c phê duy?t.";
                    return RedirectToAction("Checkout", "Order");
                }

                // L?y thông tin t? Session
                var username = Session["Username"]?.ToString();
                var shippingAddress = Session["ShippingAddress"]?.ToString();
                var shippingMethod = Session["ShippingMethod"]?.ToString();

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(shippingAddress))
                {
                    TempData["Error"] = "Phiên làm vi?c ?ã h?t h?n. Vui lòng th? l?i.";
                    return RedirectToAction("Index", "Cart");
                }

                var cartService = GetCartService();
                var cart = cartService.GetCart();

                if (!cart.Any())
                {
                    TempData["Error"] = "Gi? hàng c?a b?n ?ang tr?ng.";
                    return RedirectToAction("Index", "Cart");
                }

                var customer = db.Customers.SingleOrDefault(c => c.Username == username);
                if (customer == null)
                {
                    TempData["Error"] = "Không tìm th?y thông tin khách hàng.";
                    return RedirectToAction("Index", "Cart");
                }

                // T?o ??n hàng trong database
                var order = new DbOrder
                {
                    CustomerID = customer.CustomerID,
                    OrderDate = DateTime.Now,
                    TotalAmount = cartService.GetTotalAmount(),
                    PaymentStatus = "PayPal - ?ã thanh toán",
                    ShippingAddress = shippingAddress,
                    DeliveryMethod = shippingMethod,
                    PaymentMethod = "PayPal"
                };

                db.Orders.Add(order);
                db.SaveChanges();

                // T?o chi ti?t ??n hàng
                foreach (var item in cart)
                {
                    var orderDetail = new DbOrderDetail
                    {
                        OrderID = order.OrderID,
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    };
                    db.OrderDetails.Add(orderDetail);
                }

                db.SaveChanges();

                // Xóa gi? hàng
                cartService.ClearCart();

                // Xóa thông tin trong Session
                Session.Remove("PaymentID");
                Session.Remove("ShippingAddress");
                Session.Remove("ShippingMethod");

                TempData["Success"] = "Thanh toán PayPal thành công!";
                TempData["PayPalTransactionId"] = executedPayment.id;
                
                return RedirectToAction("OrderSuccess", "Order", new { id = order.OrderID });
            }
            catch (PayPal.PayPalException ex)
            {
                TempData["Error"] = "L?i PayPal: " + ex.Message;
                return RedirectToAction("Checkout", "Order");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có l?i x?y ra: " + ex.Message;
                return RedirectToAction("Checkout", "Order");
            }
        }

        /// <summary>
        /// X? lý khi ng??i dùng h?y thanh toán
        /// </summary>
        public ActionResult CancelPayment()
        {
            // Xóa thông tin trong Session
            Session.Remove("PaymentID");
            Session.Remove("ShippingAddress");
            Session.Remove("ShippingMethod");

            TempData["Error"] = "B?n ?ã h?y thanh toán PayPal.";
            return RedirectToAction("Checkout", "Order");
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
