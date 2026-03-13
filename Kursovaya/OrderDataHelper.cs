using MySql.Data.MySqlClient;
using System.Data;

namespace Kursovaya
{
    public class OrderDataHelper
    {
        private string connectionString = ConnectionString.GetConnectionString();

        public DataTable GetOrdersData()
        {
            DataTable dataTable = new DataTable();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
  
                string query = @"
                    SELECT 
                        o.ID,
                        o.OrderDate,
                        o.StartDate,
                        o.EndDate,
                        o.TotalPrice,
                        c.ClientName,
                        b.Nam as BoatName,
                        bc.CategoryName,
                        os.StatusName,
                        u.FullName as UserName
                    FROM Orders o
                    INNER JOIN Clients c ON o.ClientID = c.ID
                    INNER JOIN Boat b ON o.BoatID = b.ID
                    INNER JOIN BoatCategories bc ON b.CategoryID = bc.ID
                    INNER JOIN OrderStatuses os ON o.StatusID = os.ID
                    INNER JOIN Users u ON o.UserID = u.ID
                    ORDER BY o.OrderDate DESC";

                MySqlCommand command = new MySqlCommand(query, connection);
                MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                adapter.Fill(dataTable);
            }

            return dataTable;
        }
    }
}