using WebGoatCore.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Threading;

namespace WebGoatCore.Data
{
    public class OrderRepository
    {
        private readonly NorthwindContext _context;
        private readonly CustomerRepository _customerRepository;

        public OrderRepository(NorthwindContext context, CustomerRepository customerRepository)
        {
            _context = context;
            _customerRepository = customerRepository;
        }

        public Order GetOrderById(int orderId)
        {
            return _context.Orders.Single(o => o.OrderId == orderId);
        }

        public int CreateOrder(Order order)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            string shippedDate = order.ShippedDate.HasValue ? "'" + string.Format("yyyy-MM-dd", order.ShippedDate.Value) + "'" : "NULL";
            var sql = "INSERT INTO Orders (" +
                "CustomerId, EmployeeId, OrderDate, RequiredDate, ShippedDate, ShipVia, Freight, ShipName, ShipAddress, " +
                "ShipCity, ShipRegion, ShipPostalCode, ShipCountry" +
                ") VALUES (" +
                "@CustomerId, @EmployeeId, @OrderDate, @RequiredDate, " +
                $"{shippedDate}, @ShipVia, @Freight, @ShipName, @ShipAddress, " +
                "@ShipCity, @ShipRegion, @ShipPostalCode, @ShipCountry)";
            sql += ";\nSELECT OrderID FROM Orders ORDER BY OrderID DESC LIMIT 1;";

            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = sql;
                command.Parameters.AddWithValue("@CustomerId", order.CustomerId);
                command.Parameters.AddWithValue("@EmployeeId", order.EmployeeId);
                command.Parameters.AddWithValue("@OrderDate", order.OrderDate);
                command.Parameters.AddWithValue("@RequiredDate", order.RequiredDate);
                command.Parameters.AddWithValue("@ShipVia", order.ShipVia);
                command.Parameters.AddWithValue("@Freight", order.Freight);
                command.Parameters.AddWithValue("@ShipName", order.ShipName);
                command.Parameters.AddWithValue("@ShipAddress", order.ShipAddress);
                command.Parameters.AddWithValue("@ShipCity", order.ShipCity);
                command.Parameters.AddWithValue("@ShipRegion", order.ShipRegion);
                command.Parameters.AddWithValue("@ShipPostalCode", order.ShipPostalCode);
                command.Parameters.AddWithValue("@ShipCountry", order.ShipCountry);

                _context.Database.OpenConnection();

                using var dataReader = command.ExecuteReader();
                dataReader.Read();
                order.OrderId = Convert.ToInt32(dataReader[0]);
            }

            sql = "INSERT INTO OrderDetails (" +
                "OrderId, ProductId, UnitPrice, Quantity, Discount" +
                ") VALUES (@OrderId, @ProductId, @UnitPrice, @Quantity, @Discount)";
            foreach (var (orderDetails, i) in order.OrderDetails.WithIndex())
            {
                orderDetails.OrderId = order.OrderId;
                sql += (i > 0 ? "," : "") +
                    $"(@OrderId, @ProductId{i}, @UnitPrice{i}, @Quantity{i}, @Discount{i})";

                command.CommandText = sql;
                command.Parameters.AddWithValue($"@ProductId{i}", orderDetails.ProductId);
                command.Parameters.AddWithValue($"@UnitPrice{i}", orderDetails.UnitPrice);
                command.Parameters.AddWithValue($"@Quantity{i}", orderDetails.Quantity);
                command.Parameters.AddWithValue($"@Discount{i}", orderDetails.Discount);
            }

            // Execute the final SQL statement with all the parameters
            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = sql;
                _context.Database.OpenConnection();
                command.ExecuteNonQuery();
            }

            return order.OrderId;
        }

            if (order.Shipment != null)
            {
                var shipment = order.Shipment;
                shipment.OrderId = order.OrderId;
                sql += ";\nINSERT INTO Shipments (" +
                    "OrderId, ShipperId, ShipmentDate, TrackingNumber" +
                    ") VALUES (" +
                    $"'{shipment.OrderId}','{shipment.ShipperId}','{shipment.ShipmentDate:yyyy-MM-dd}','{shipment.TrackingNumber}')";
            }

            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = sql;
                _context.Database.OpenConnection();
                command.ExecuteNonQuery();
            }

            return order.OrderId;
        }

        public void CreateOrderPayment(int orderId, decimal amountPaid, string creditCardNumber, DateTime expirationDate, string approvalCode)
        {
            var orderPayment = new OrderPayment()
            {
                AmountPaid = Convert.ToDouble(amountPaid),
                CreditCardNumber = creditCardNumber,
                ApprovalCode = approvalCode,
                ExpirationDate = expirationDate,
                OrderId = orderId,
                PaymentDate = DateTime.Now
            };
            _context.OrderPayments.Add(orderPayment);
            _context.SaveChanges();
        }

        public ICollection<Order> GetAllOrdersByCustomerId(string customerId)
        {
            return _context.Orders
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.OrderId)
                .ToList();
        }
    }
}
