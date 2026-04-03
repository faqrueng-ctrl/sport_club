using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using SportClubApp.Data;
using SportClubApp.Models;

namespace SportClubApp.Services
{
    public sealed class CatalogService
    {
        public List<ProductItem> GetProducts(string search = "", string sort = "Name")
        {
            var rows = new List<ProductItem>();
            using (var conn = Db.OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                var orderBy = sort == "Price" ? "Price" : "Name";
                cmd.CommandText = $@"SELECT Id,Name,Category,Price,OldPrice,DiscountPercent,ImagePath,StockQty
FROM dbo.Products
WHERE (@s = '' OR Name LIKE '%' + @s + '%' OR Category LIKE '%' + @s + '%')
ORDER BY {orderBy};";
                cmd.Parameters.AddWithValue("@s", search ?? string.Empty);

                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        rows.Add(new ProductItem
                        {
                            ProductId = r.GetInt32(0),
                            Name = r.GetString(1),
                            Category = r.GetString(2),
                            Price = r.GetDecimal(3),
                            OldPrice = r.IsDBNull(4) ? (decimal?)null : r.GetDecimal(4),
                            DiscountPercent = r.IsDBNull(5) ? (int?)null : r.GetInt32(5),
                            ImagePath = r.IsDBNull(6) ? null : r.GetString(6),
                            StockQty = r.GetInt32(7)
                        });
                    }
                }
            }
            return rows;
        }

        public void SaveProduct(ProductItem p)
        {
            if (string.IsNullOrWhiteSpace(p.Name)) throw new InvalidOperationException("Название обязательно.");
            if (p.Price < 0 || p.StockQty < 0) throw new InvalidOperationException("Некорректные числовые значения.");

            using (var conn = Db.OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                if (p.ProductId == 0)
                {
                    cmd.CommandText = @"INSERT INTO dbo.Products(Name,Category,Price,OldPrice,DiscountPercent,ImagePath,StockQty)
VALUES(@n,@c,@p,@o,@d,@i,@s);";
                }
                else
                {
                    cmd.CommandText = @"UPDATE dbo.Products
SET Name=@n,Category=@c,Price=@p,OldPrice=@o,DiscountPercent=@d,ImagePath=@i,StockQty=@s
WHERE Id=@id";
                    cmd.Parameters.AddWithValue("@id", p.ProductId);
                }

                cmd.Parameters.AddWithValue("@n", p.Name);
                cmd.Parameters.AddWithValue("@c", p.Category ?? "Прочее");
                cmd.Parameters.AddWithValue("@p", p.Price);
                cmd.Parameters.AddWithValue("@o", (object)p.OldPrice ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@d", (object)p.DiscountPercent ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@i", (object)p.ImagePath ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@s", p.StockQty);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteProduct(int productId)
        {
            using (var conn = Db.OpenConnection())
            using (var check = conn.CreateCommand())
            {
                check.CommandText = "SELECT COUNT(*) FROM dbo.OrderItems WHERE ProductId=@id";
                check.Parameters.AddWithValue("@id", productId);
                var count = (int)check.ExecuteScalar();
                if (count > 0)
                {
                    throw new InvalidOperationException("Невозможно удалить товар, так как он присутствует в одном или нескольких заказах.");
                }
            }

            using (var conn = Db.OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM dbo.Products WHERE Id=@id";
                cmd.Parameters.AddWithValue("@id", productId);
                cmd.ExecuteNonQuery();
            }
        }

        public string CopyImageToProject(string sourceFile)
        {
            var projectDir = AppDomain.CurrentDomain.BaseDirectory;
            var imageDir = Path.Combine(projectDir, "ProductImages");
            Directory.CreateDirectory(imageDir);
            var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(sourceFile);
            var target = Path.Combine(imageDir, fileName);
            File.Copy(sourceFile, target, true);
            return Path.Combine("ProductImages", fileName);
        }
    }
}
