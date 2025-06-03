using System.ComponentModel.DataAnnotations;

namespace AdidasStoreMVC.Models
{

    public class Admin
    {
        [Key]
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

    }
}