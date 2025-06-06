using AdidasStoreMVC.Data;
using AdidasStoreMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace AdidasStoreMVC.Controllers
{
    public class OrdersController : Controller
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        // Lấy giỏ hàng từ session
        private List<CartItem> GetCart()
        {
            var cart = HttpContext.Session.GetString("Cart");
            return string.IsNullOrEmpty(cart)
                ? new List<CartItem>()
                : JsonConvert.DeserializeObject<List<CartItem>>(cart)!;
        }

        // ====== PHẦN THANH TOÁN CHO CLIENT ======

        // GET: Orders/Checkout
        [Authorize(Roles = "Client")]
        [HttpGet]
        public IActionResult Checkout()
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                TempData["Error"] = "Giỏ hàng trống!";
                return RedirectToAction("Index", "Cart");
            }
            return View(new Order());
        }

        // POST: Orders/Checkout
        [Authorize(Roles = "Client")]
        [HttpPost]
        public IActionResult Checkout(Order order)
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                TempData["Error"] = "Giỏ hàng trống!";
                return RedirectToAction("Index", "Cart");
            }

            if (!ModelState.IsValid)
            {
                return View(order);
            }

            // Lưu thông tin khách vào Session tạm (nếu cần xác nhận lại)
            HttpContext.Session.SetString("OrderTemp", JsonConvert.SerializeObject(order));
            return RedirectToAction("Confirm");
        }

        // Xác nhận đơn hàng (hiển thị lại thông tin, kiểm tra lần cuối)
        [Authorize(Roles = "Client")]
        [HttpGet]
        public IActionResult Confirm()
        {
            var orderJson = HttpContext.Session.GetString("OrderTemp");
            var cart = GetCart();
            if (string.IsNullOrEmpty(orderJson) || !cart.Any())
                return RedirectToAction("Checkout");

            var order = JsonConvert.DeserializeObject<Order>(orderJson)!;
            ViewBag.Cart = cart;
            return View(order);
        }

        // POST xác nhận đặt hàng, lưu vào database
        [Authorize(Roles = "Client")]
        [HttpPost]
        public IActionResult ConfirmOrder()
        {
            var orderJson = HttpContext.Session.GetString("OrderTemp");
            var cart = GetCart();
            if (string.IsNullOrEmpty(orderJson) || !cart.Any())
                return RedirectToAction("Checkout");

            var order = JsonConvert.DeserializeObject<Order>(orderJson)!;

            foreach (var item in cart)
            {
                order.OrderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product!.Price
                });
            }

            order.OrderDate = DateTime.Now;
            order.Status = OrderStatus.Pending;

            _context.Orders.Add(order);
            _context.SaveChanges();

            HttpContext.Session.Remove("Cart");
            HttpContext.Session.Remove("OrderTemp");

            return RedirectToAction("Success");
        }

        // Hiển thị trang đặt hàng thành công
        public IActionResult Success()
        {
            return View();
        }

        // ====== PHẦN QUẢN LÝ ĐƠN HÀNG CHO ADMIN ======

        // Hiển thị danh sách đơn hàng cho admin
        [Authorize(Roles = "Admin")]
        public IActionResult Manage()
        {
            var orders = _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .ToList();
            return View(orders);
        }

        // Duyệt đơn
        [Authorize(Roles = "Admin")]
        public IActionResult Approve(int id)
        {
            var order = _context.Orders.Find(id);
            if (order == null) return NotFound();
            order.Status = OrderStatus.Approved;
            _context.SaveChanges();
            return RedirectToAction("Manage");
        }

        // Từ chối đơn
        [Authorize(Roles = "Admin")]
        public IActionResult Reject(int id)
        {
            var order = _context.Orders.Find(id);
            if (order == null) return NotFound();
            order.Status = OrderStatus.Rejected;
            _context.SaveChanges();
            return RedirectToAction("Manage");
        }
        [Authorize(Roles = "Admin")]
        public IActionResult Details(int id)
        {
            var order = _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
                return NotFound();

            return View(order);
        }
        // ====== BÁO CÁO THỐNG KÊ CHO ADMIN ======
        [Authorize(Roles = "Admin")]
        public IActionResult Report()
        {
            // Tổng đơn hàng
            int totalOrders = _context.Orders.Count();

            // Tổng đơn hàng theo trạng thái
            int pendingOrders = _context.Orders.Count(o => o.Status == OrderStatus.Pending);
            int approvedOrders = _context.Orders.Count(o => o.Status == OrderStatus.Approved);
            int rejectedOrders = _context.Orders.Count(o => o.Status == OrderStatus.Rejected);

            var year = DateTime.Now.Year;
            var data = _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.OrderDate.Year == year && o.Status == OrderStatus.Approved)
                .GroupBy(o => o.OrderDate.Month)
                .Select(g => new {
                    Month = g.Key,
                    Revenue = g.Sum(order => order.OrderItems.Sum(item => item.Quantity * item.UnitPrice))
                })
                .ToList();

            decimal[] revenues = new decimal[12];
            foreach (var item in data)
                revenues[item.Month - 1] = item.Revenue;

            ViewBag.Revenues = revenues;
            ViewBag.Year = year;

            // Tổng doanh thu (chỉ tính đơn đã duyệt)
            decimal totalRevenue = _context.Orders
                .Where(o => o.Status == OrderStatus.Approved)
                .SelectMany(o => o.OrderItems)
                .Sum(oi => oi.UnitPrice * oi.Quantity);

            // 10 đơn mới nhất
            var latestOrders = _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .ToList();

            ViewBag.TotalOrders = totalOrders;
            ViewBag.PendingOrders = pendingOrders;
            ViewBag.ApprovedOrders = approvedOrders;
            ViewBag.RejectedOrders = rejectedOrders;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.LatestOrders = latestOrders;

            return View();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult ExportOrdersToExcel()
        {
            var orders = _context.Orders
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Orders");
                worksheet.Cells[1, 1].Value = "Mã đơn";
                worksheet.Cells[1, 2].Value = "Khách hàng";
                worksheet.Cells[1, 3].Value = "SĐT";
                worksheet.Cells[1, 4].Value = "Địa chỉ";
                worksheet.Cells[1, 5].Value = "Ngày đặt";
                worksheet.Cells[1, 6].Value = "Trạng thái";
                worksheet.Cells[1, 7].Value = "Tổng tiền";
                int row = 2;
                foreach (var order in orders)
                {
                    worksheet.Cells[row, 1].Value = order.Id;
                    worksheet.Cells[row, 2].Value = order.CustomerName;
                    worksheet.Cells[row, 3].Value = order.Phone;
                    worksheet.Cells[row, 4].Value = order.Address;
                    worksheet.Cells[row, 5].Value = order.OrderDate.ToString("dd/MM/yyyy HH:mm");
                    worksheet.Cells[row, 6].Value = order.Status.ToString();
                    worksheet.Cells[row, 7].Value = order.OrderItems.Sum(i => i.UnitPrice * i.Quantity);
                    row++;
                }
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                using (var stream = new MemoryStream())
                {
                    package.SaveAs(stream);
                    stream.Position = 0;
                    var fileName = $"DanhSachDonHang_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }

        // ====== IMPORT ĐƠN HÀNG TỪ EXCEL ======
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult ImportOrdersFromExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn file Excel.";
                return RedirectToAction("Report");
            }

            int imported = 0;
            try
            {
                using (var stream = new MemoryStream())
                {
                    file.CopyTo(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        int rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++)
                        {
                            // Giả sử file Excel có các cột giống export: 
                            // Mã đơn | Khách hàng | SĐT | Địa chỉ | Ngày đặt | Trạng thái | Tổng tiền
                            // Bạn có thể tuỳ chỉnh mapping theo file thực tế

                            string customer = worksheet.Cells[row, 2].Value?.ToString();
                            string phone = worksheet.Cells[row, 3].Value?.ToString();
                            string address = worksheet.Cells[row, 4].Value?.ToString();
                            string orderDateStr = worksheet.Cells[row, 5].Value?.ToString();
                            string statusStr = worksheet.Cells[row, 6].Value?.ToString();
                            string totalAmountStr = worksheet.Cells[row, 7].Value?.ToString();
                            decimal totalAmount = 0;
                            decimal.TryParse(totalAmountStr, out totalAmount);

                            if (string.IsNullOrWhiteSpace(customer)) continue;

                            DateTime orderDate = DateTime.Now;
                            if (!DateTime.TryParse(orderDateStr, out orderDate))
                                orderDate = DateTime.Now;

                            OrderStatus status = OrderStatus.Pending;
                            Enum.TryParse(statusStr, out status);

                            var order = new Order
                            {
                                CustomerName = customer,
                                Phone = phone,
                                Address = address,
                                OrderDate = orderDate,
                                Status = status,
                                TotalAmount = totalAmount,
                                OrderItems = new List<OrderItem>() // Bạn nên import chi tiết đơn hàng nếu có cột
                            };

                            // TODO: Nếu file Excel có dữ liệu chi tiết OrderItems, hãy đọc thêm các cột và add vào order.OrderItems ở đây

                            _context.Orders.Add(order);
                            imported++;
                        }
                        _context.SaveChanges();
                    }
                }
                TempData["Success"] = $"Import thành công {imported} đơn hàng!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi import: {ex.Message}";
            }
            return RedirectToAction("Report");
        }
    }
}
