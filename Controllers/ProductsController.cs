using AdidasStoreMVC.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdidasStoreMVC.Data;


namespace AdidasStore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            //  Ví dụ: Lấy tất cả sản phẩm, sắp xếp theo giá tăng dần, chỉ lấy tên và giá
            var products = await _context.Products
                .OrderBy(p => p.Price)
                .Select(p => new { p.Name, p.Price })
                .ToListAsync();

            return Ok(products); //  Trả về 200 OK với dữ liệu
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            //  Ví dụ: Lấy sản phẩm theo ID, bao gồm cả thông tin chi tiết
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }

        //  Các Action khác (Post, Put, Delete) cũng có thể sử dụng LINQ
    }
}
