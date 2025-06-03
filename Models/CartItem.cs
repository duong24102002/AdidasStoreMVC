using AdidasStoreMVC.Models;

namespace AdidasStoreMVC.Models
{
	public class CartItem
	{
		public int ProductId { get; set; }
		public Product? Product { get; set; }
		public int Quantity { get; set; }
	}
}
