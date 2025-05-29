using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Buoi6.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên sản phẩm không được vượt quá 100 ký tự")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Giá sản phẩm không được để trống, tối thiểu 1.000 đến 1.000.000")]
        public decimal Price { get; set; }

        public string Description { get; set; }

        public string? ImageUrl { get; set; }

        public List<ProductImage> Images { get; set; }

        [DisplayName("Danh mục")]
        public int CategoryId { get; set; }

        [DisplayName("Danh mục")]
        public Category? Category { get; set; }
    }
}
