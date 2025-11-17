using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using PagedList;
using _22DH114699_LTW.Models;
using _22DH114699_LTW.Services;

namespace _22DH114699_LTW.Controllers
{
    public class HomeController : Controller
    {
        private readonly MyStoreEntities db = new MyStoreEntities();

        // Trang chủ - hiển thị sản phẩm ngay cả khi không có đơn hàng
        public ActionResult Index(string searchTerm, int? categoryId, int? featuredPage, int? newPage)
        {
            var categories = db.Categories.OrderBy(c => c.CategoryName).ToList();

            // Query base theo tìm kiếm
            var productsQuery = db.Products.Include(p => p.Category).AsQueryable();
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                productsQuery = productsQuery.Where(p => p.ProductName.Contains(term) || 
                                                          p.ProductDescription.Contains(term) || 
                                                          p.Category.CategoryName.Contains(term));
            }
            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryID == categoryId.Value);
            }

            // Sản phẩm nổi bật: Ưu tiên sản phẩm mới, sau đó mới đến bán chạy
            int featuredPageNum = featuredPage ?? 1;
            IPagedList<Product> topProductsPaged;
            
            if (db.OrderDetails.Any())
            {
                // Có đơn hàng: Kết hợp sản phẩm mới + bán chạy
                var recentProducts = productsQuery.OrderByDescending(p => p.ProductID).Take(10); // 10 sản phẩm mới nhất
                var topSelling = db.OrderDetails
                                .GroupBy(od => od.ProductID)
                                .Select(g => new { ProductID = g.Key, Count = g.Sum(x => x.Quantity) })
                                .OrderByDescending(x => x.Count)
                                .Take(20)
                                .Join(db.Products.Include(p => p.Category), x => x.ProductID, p => p.ProductID, (x, p) => p);
                
                // Gộp 2 danh sách và loại bỏ trùng lặp
                var combined = recentProducts.Union(topSelling).Distinct().OrderByDescending(p => p.ProductID);
                topProductsPaged = combined.ToPagedList(featuredPageNum, 6);
            }
            else
            {
                // Không có đơn hàng → Lấy sản phẩm mới nhất
                topProductsPaged = productsQuery.OrderByDescending(p => p.ProductID).ToPagedList(featuredPageNum, 6);
            }

            // Sản phẩm mới: lấy theo ProductID giảm dần (sản phẩm mới nhất), phân trang 6/trang
            int newPageNum = newPage ?? 1;
            var newProductsQuery = productsQuery.OrderByDescending(p => p.ProductID);
            var newProductsPaged = newProductsQuery.ToPagedList(newPageNum, 6);

            // Truyền qua ViewBag
            ViewBag.SearchTerm = searchTerm;
            ViewBag.CategoryId = categoryId;
            ViewBag.Categories = categories;
            ViewBag.TopProductsPaged = topProductsPaged;
            ViewBag.NewProductsPaged = newProductsPaged;

            return View();
        }

        // Danh sách sản phẩm theo danh mục
        public ActionResult ProductList(int? categoryId, decimal? minPrice, decimal? maxPrice, string searchTerm, string sortOrder)
        {
            var categories = db.Categories.OrderBy(c => c.CategoryName).ToList();
            var category = categoryId.HasValue ? db.Categories.Find(categoryId.Value) : null;
            
            // Query base
            var productsQuery = db.Products.Include(p => p.Category).AsQueryable();
            
            // Lọc theo danh mục
            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryID == categoryId.Value);
            }
            
            // Lọc theo khoảng giá
            if (minPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.ProductPrice >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.ProductPrice <= maxPrice.Value);
            }
            
            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                productsQuery = productsQuery.Where(p => p.ProductName.Contains(term) || 
                                                      p.ProductDescription.Contains(term));
            }
            
            // Sắp xếp
            switch (sortOrder)
            {
                case "name_asc":
                    productsQuery = productsQuery.OrderBy(p => p.ProductName);
                    break;
                case "price_asc":
                    productsQuery = productsQuery.OrderBy(p => p.ProductPrice);
                    break;
                case "price_desc":
                    productsQuery = productsQuery.OrderByDescending(p => p.ProductPrice);
                    break;
                case "newest":
                    productsQuery = productsQuery.OrderByDescending(p => p.ProductID);
                    break;
                default:
                    productsQuery = productsQuery.OrderBy(p => p.ProductName);
                    break;
            }
            
            var products = productsQuery.ToList();
            
            // Đếm tổng số sản phẩm
            var totalProducts = db.Products.Count();
            
            // Truyền dữ liệu qua ViewBag
            ViewBag.Category = category;
            ViewBag.Categories = categories;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.SortOrder = sortOrder;
            
            return View(products);
        }

        [HttpGet]
        public ActionResult ProductDetail(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest, "Product ID is required");
            }

            var product = db.Products.Include(p => p.Category).FirstOrDefault(p => p.ProductID == id.Value);
            if (product == null) return HttpNotFound();

            // Lấy số lượng đã bán của sản phẩm hiện tại
            var soldQuantity = db.OrderDetails
                .Where(od => od.ProductID == id.Value)
                .Sum(od => (int?)od.Quantity) ?? 0;
            ViewBag.SoldQuantity = soldQuantity;

            // Lấy sản phẩm tương tự (cùng danh mục, khác ID)
            var similarProducts = db.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryID == product.CategoryID && p.ProductID != id.Value)
                .OrderByDescending(p => p.ProductID)
                .Take(12)
                .ToList();
            ViewBag.SimilarProducts = similarProducts;

            // Lấy top deals (sản phẩm bán chạy nhất)
            var topDeals = db.OrderDetails
                .GroupBy(od => od.ProductID)
                .Select(g => new { ProductID = g.Key, TotalSold = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.TotalSold)
                .Take(9)
                .Join(db.Products.Include(p => p.Category), 
                      x => x.ProductID, 
                      p => p.ProductID, 
                      (x, p) => new { Product = p, TotalSold = x.TotalSold })
                .ToList();
            ViewBag.TopDeals = topDeals;

            return View(product);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}