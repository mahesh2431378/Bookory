using System.ComponentModel.DataAnnotations;

namespace BookStoreMVC.Models
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public User? User { get; set; }

        [Required]
        public int BookId { get; set; }

        public Book? Book { get; set; }

        [Required]
        public int Quantity { get; set; } = 1;
    }
}