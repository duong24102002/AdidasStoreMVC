using Microsoft.AspNetCore.Mvc;
using AdidasStore.Models;
using Newtonsoft.Json;
using AdidasStoreMVC.Data;

namespace AdidasStore.Controllers
{
    public class OrdersController : Controller
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        private List<CartItem> GetCart()
        {
            var cart = HttpContext.Session.GetString("Cart");
            return string.IsNullOrEmpty(cart)
                ? new List<CartItem>()
                : JsonConvert.DeserializeObject<List<CartItem>>(cart)!;
        }

        [HttpGet]
        public IActionResult Checkout()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Checkout(Order order)
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                ModelState.AddModelError("", "Giỏ hàng trống.");
                return View(order);
            }

            if (!ModelState.IsValid)
            {
                return View(order);
            }

            // Lưu thông tin khách vào Session tạm để xác nhận sau
            HttpContext.Session.SetString("OrderTemp", JsonConvert.SerializeObject(order));
            return RedirectToAction("Confirm");
        }

        [HttpGet]
        public IActionResult Confirm()
        {
            var orderJson = HttpContext.Session.GetString("OrderTemp");
            var cart = GetCart();
            if (string.IsNullOrEmpty(orderJson) || !cart.Any()) return RedirectToAction("Checkout");

            var order = JsonConvert.DeserializeObject<Order>(orderJson)!;
            ViewBag.Cart = cart;
            return View(order);
        }

        [HttpPost]
        public IActionResult ConfirmOrder()
        {
            var orderJson = HttpContext.Session.GetString("OrderTemp");
            var cart = GetCart();
            if (string.IsNullOrEmpty(orderJson) || !cart.Any()) return RedirectToAction("Checkout");

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
            _context.Orders.Add(order);
            _context.SaveChanges();

            HttpContext.Session.Remove("Cart");
            HttpContext.Session.Remove("OrderTemp");

            return RedirectToAction("Success");
        }

        public IActionResult Success()
        {
            return View();
        }
    }
}
