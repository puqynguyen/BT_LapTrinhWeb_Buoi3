using System.ComponentModel.DataAnnotations;

namespace Buoi6.Models
{
    public class Cart
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public ApplicationUser User { get; set; }
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}
