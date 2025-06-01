using Buoi6.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Buoi6.Repository
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product> GetProductByIdAsync(int id);
        Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(int id);
    }
}