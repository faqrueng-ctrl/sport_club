using System;
using System.Collections.Generic;
using SportClubApp.Data;
using SportClubApp.Models;

namespace SportClubApp.Services
{
    public sealed class OrderService
    {
        public void AddToCart(int userId, int productId)
        {
            using (var conn = Db.OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
IF EXISTS(SELECT 1 FROM dbo.CartItems WHERE UserId=@u AND ProductId=@p)
    UPDATE dbo.CartItems SET Quantity = Quantity + 1 WHERE UserId=@u AND ProductId=@p
ELSE
    INSERT INTO dbo.CartItems(UserId,ProductId,Quantity) VALUES(@u,@p,1);";
                cmd.Parameters.AddWithValue("@u", userId);
                cmd.Parameters.AddWithValue("@p", productId);
                cmd.ExecuteNonQuery();
            }
        }

        public List<CartItem> GetCart(int userId)
        {
            var rows = new List<CartItem>();
            using (var conn = Db.OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT c.Id,c.ProductId,p.Name,p.Price,c.Quantity
FROM dbo.CartItems c JOIN dbo.Products p ON p.Id=c.ProductId
WHERE c.UserId=@u";
                cmd.Parameters.AddWithValue("@u", userId);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        rows.Add(new CartItem
                        {
                            CartItemId = r.GetInt32(0),
                            ProductId = r.GetInt32(1),
                            Name = r.GetString(2),
                            UnitPrice = r.GetDecimal(3),
                            Quantity = r.GetInt32(4)
                        });
                    }
                }
            }
            return rows;
        }

        public void RemoveFromCart(int cartItemId)
        {
            using (var conn = Db.OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM dbo.CartItems WHERE Id=@id";
                cmd.Parameters.AddWithValue("@id", cartItemId);
                cmd.ExecuteNonQuery();
            }
        }

        public void Checkout(int userId)
        {
            using (var conn = Db.OpenConnection())
            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    var cart = new List<CartItem>();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = @"SELECT c.ProductId,p.Price,c.Quantity
FROM dbo.CartItems c JOIN dbo.Products p ON p.Id=c.ProductId WHERE c.UserId=@u";
                        cmd.Parameters.AddWithValue("@u", userId);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                cart.Add(new CartItem { ProductId = r.GetInt32(0), UnitPrice = r.GetDecimal(1), Quantity = r.GetInt32(2) });
                            }
                        }
                    }

                    if (cart.Count == 0) throw new InvalidOperationException("Корзина пуста.");

                    decimal total = 0;
                    foreach (var x in cart) total += x.Total;

                    int orderId;
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = @"INSERT INTO dbo.Orders(UserId,Total,Status)
OUTPUT INSERTED.Id VALUES(@u,@t,N'Оформлен');";
                        cmd.Parameters.AddWithValue("@u", userId);
                        cmd.Parameters.AddWithValue("@t", total);
                        orderId = (int)cmd.ExecuteScalar();
                    }

                    foreach (var x in cart)
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tx;
                            cmd.CommandText = @"INSERT INTO dbo.OrderItems(OrderId,ProductId,Price,Quantity)
VALUES(@o,@p,@pr,@q);";
                            cmd.Parameters.AddWithValue("@o", orderId);
                            cmd.Parameters.AddWithValue("@p", x.ProductId);
                            cmd.Parameters.AddWithValue("@pr", x.UnitPrice);
                            cmd.Parameters.AddWithValue("@q", x.Quantity);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = "DELETE FROM dbo.CartItems WHERE UserId=@u";
                        cmd.Parameters.AddWithValue("@u", userId);
                        cmd.ExecuteNonQuery();
                    }

                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public List<OrderHistoryItem> GetOrders(int userId)
        {
            var rows = new List<OrderHistoryItem>();
            using (var conn = Db.OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Id,CreatedAt,Total,Status FROM dbo.Orders WHERE UserId=@u ORDER BY CreatedAt DESC";
                cmd.Parameters.AddWithValue("@u", userId);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        rows.Add(new OrderHistoryItem
                        {
                            OrderId = r.GetInt32(0),
                            CreatedAt = r.GetDateTime(1),
                            Total = r.GetDecimal(2),
                            Status = r.GetString(3)
                        });
                    }
                }
            }
            return rows;
        }
    }
}
