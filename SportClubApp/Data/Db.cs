using System.Configuration;
using System.Data.SqlClient;

namespace SportClubApp.Data
{
    public static class Db
    {
        public static SqlConnection OpenConnection()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["SportClubDb"].ConnectionString;
            var connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        }
    }
}
