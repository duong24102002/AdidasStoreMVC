using AdidasStoreMVC.Data;
using Microsoft.AspNetCore.Mvc;
using AdidasStoreMVC.Models;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

public class ReportsController : Controller
{
    private readonly AppDbContext _context;
    public ReportsController(AppDbContext context) => _context = context;

    public IActionResult RevenueByMonth()
    {
        var year = DateTime.Now.Year; // hoặc cho phép chọn năm

        // Include OrderItems để đảm bảo truy vấn đầy đủ
        var data = _context.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.OrderDate.Year == year)
            .GroupBy(o => o.OrderDate.Month)
            .Select(g => new {
                Month = g.Key,
                Revenue = g.Sum(order => order.OrderItems.Sum(item => item.Quantity * item.UnitPrice))
            })
            .ToList();

        // Tạo mảng doanh thu 12 tháng, mặc định 0
        decimal[] revenues = new decimal[12];
        foreach (var item in data)
            revenues[item.Month - 1] = item.Revenue;

        ViewBag.Revenues = revenues;
        ViewBag.Year = year;
        return View();
    }
}