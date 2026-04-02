using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace SportClubApp.Data
{
    public sealed class SchemaHelper
    {
        private readonly SqlConnection _connection;
        private readonly Dictionary<string, List<string>> _cache = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        public SchemaHelper(SqlConnection connection)
        {
            _connection = connection;
        }

        public List<string> GetColumns(string table)
        {
            if (_cache.TryGetValue(table, out var found))
            {
                return found;
            }

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = @"
SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @table
ORDER BY ORDINAL_POSITION;";
                cmd.Parameters.AddWithValue("@table", table);

                var cols = new List<string>();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        cols.Add(reader.GetString(0));
                    }
                }

                _cache[table] = cols;
                return cols;
            }
        }

        public string FindColumn(string table, params string[] candidates)
        {
            var cols = GetColumns(table);
            foreach (var candidate in candidates)
            {
                var match = cols.FirstOrDefault(c => string.Equals(c, candidate, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        public string FindIdColumn(string table)
        {
            var cols = GetColumns(table);
            var exactId = cols.FirstOrDefault(c => string.Equals(c, "Id", StringComparison.OrdinalIgnoreCase));
            if (exactId != null) return exactId;

            var byTable = cols.FirstOrDefault(c => string.Equals(c, table + "Id", StringComparison.OrdinalIgnoreCase));
            if (byTable != null) return byTable;

            var endsWithId = cols.FirstOrDefault(c => c.EndsWith("Id", StringComparison.OrdinalIgnoreCase));
            return endsWithId ?? cols.FirstOrDefault();
        }

        public static string Q(string col) => "[" + col + "]";
    }
}
