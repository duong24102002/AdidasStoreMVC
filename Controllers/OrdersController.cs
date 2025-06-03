using AdidasStoreMVC.Data;
using AdidasStoreMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

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
    }
}