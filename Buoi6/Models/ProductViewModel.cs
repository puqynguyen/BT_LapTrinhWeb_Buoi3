using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Buoi6.Models
{
    public class ProductViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên sản phẩm không được vượt quá 100 ký tự")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Giá sản phẩm không được để trống")]
        [Range(1000, 1000000, ErrorMessage = "Giá sản phẩm phải từ 1.000 đến 1.000.000")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Mô tả sản phẩm là bắt buộc")]
        public string Description { get; set; }

        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        public int CategoryId { get; set; }

        public IFormFile? MainImage { get; set; }

        public IList<IFormFile>? AdditionalImages { get; set; }

        public List<ProductImage>? ExistingImages { get; set; }
    }
}