using System;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using _22DH114699_LTW.Models;
using _22DH114699_LTW.Models.ViewModels;

namespace _22DH114699_LTW.Controllers
{
    public class AccountController : Controller
    {
        private MyStoreEntities db = new MyStoreEntities();

        // GET: Account/Register
        [HttpGet]
        public ActionResult Register()
        {
            if (Session["Username"] != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = db.Users.SingleOrDefault(u => u.Username == model.Username);
                    if (existingUser != null)
                    {
                        ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
                        return View(model);
                    }

                    var user = new User
                    {
                        Username = model.Username,
                        Password = model.Password, 
                        UserRole = "C" 
                    };
                    db.Users.Add(user);
                    db.SaveChanges();

                    var customer = new Customer
                    {
                        Username = model.Username,
                        CustomerName = model.CustomerName,
                        CustomerPhone = model.CustomerPhone,
                        CustomerEmail = model.CustomerEmail,
                        CustomerAddress = model.CustomerAddress
                    };
                    db.Customers.Add(customer);
                    db.SaveChanges();

                    TempData["Success"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
                    return RedirectToAction("Login");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi đăng ký: " + ex.Message);
                }
            }
            return View(model);
        }

        // GET: Account/Login
        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            if (Session["Username"] != null)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Sửa điều kiện: thay "Customer" thành "C" để khớp với giá trị trong database
                    var user = db.Users.SingleOrDefault(u => u.Username == model.Username && u.Password == model.Password && u.UserRole == "C");

                    if (user != null)
                    {
                        Session["Username"] = user.Username;
                        Session["UserRole"] = user.UserRole;

                        if (user.UserRole == "C")
                        {
                            var customer = db.Customers.SingleOrDefault(c => c.Username == user.Username);
                            if (customer != null)
                            {
                                Session["CustomerID"] = customer.CustomerID;
                                Session["CustomerName"] = customer.CustomerName;

                                FormsAuthentication.SetAuthCookie(user.Username, false);
                                
                                return RedirectToAction("Index", "Home");
                            }
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi đăng nhập: " + ex.Message);
                }
            }
            
            return View(model);
        }

        // GET: Account/Logout
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            FormsAuthentication.SignOut();

            TempData["Success"] = "Đăng xuất thành công!";
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/ProfileInfo - Hiển thị và chỉnh sửa thông tin cá nhân
        [HttpGet]
        public ActionResult ProfileInfo()
        {
            Response.ContentEncoding = Encoding.UTF8;
            Response.Charset = "utf-8";
            
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login", new { returnUrl = Url.Action("ProfileInfo") });
            }

            try
            {
                var username = Session["Username"].ToString();
                var customer = db.Customers.SingleOrDefault(c => c.Username == username);
                
                if (customer == null)
                {
                    return HttpNotFound();
                }

                return View(customer);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi tải thông tin: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: Account/UpdateProfile - Cập nhật thông tin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(Customer model)
        {
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                ModelState.Remove("Username");
                ModelState.Remove("User");
                ModelState.Remove("Orders");

                if (ModelState.IsValid)
                {
                    var customer = db.Customers.Find(model.CustomerID);
                    if (customer != null && customer.Username == Session["Username"].ToString())
                    {
                        customer.CustomerName = model.CustomerName;
                        customer.CustomerPhone = model.CustomerPhone;
                        customer.CustomerEmail = model.CustomerEmail;
                        customer.CustomerAddress = model.CustomerAddress;

                        db.SaveChanges();

                        Session["CustomerName"] = customer.CustomerName;

                        TempData["Success"] = "Cập nhật thông tin thành công!";
                        return RedirectToAction("ProfileInfo");
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi khi cập nhật: " + ex.Message);
            }

            return View("ProfileInfo", model);
        }

        // GET: Account/ChangePassword - Trang đổi mật khẩu
        [HttpGet]
        public ActionResult ChangePassword()
        {
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login", new { returnUrl = Url.Action("ChangePassword") });
            }

            return View();
        }

        // POST: Account/ChangePassword - Xử lý đổi mật khẩu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var username = Session["Username"].ToString();
                    var user = db.Users.SingleOrDefault(u => u.Username == username);

                    if (user == null)
                    {
                        ModelState.AddModelError("", "Không tìm thấy tài khoản");
                        return View(model);
                    }

                    // Kiểm tra mật khẩu hiện tại
                    if (user.Password != model.CurrentPassword)
                    {
                        ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng");
                        return View(model);
                    }

                    // Cập nhật mật khẩu mới
                    user.Password = model.NewPassword;
                    db.SaveChanges();

                    TempData["Success"] = "Đổi mật khẩu thành công!";
                    return RedirectToAction("ProfileInfo");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi đổi mật khẩu: " + ex.Message);
                }
            }

            return View(model);
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
