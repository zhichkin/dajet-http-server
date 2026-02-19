using DaJet.Http.Model;
using Microsoft.Data.Sqlite;

namespace DaJet.Http.Server
{
    public sealed class DataSourceRepository : RepositoryBase<DataSourceRecord>
    {
        private static string CONNECTION_STRING;
        private const string CREATE_TABLE_COMMAND = "CREATE TABLE IF NOT EXISTS datasources (name TEXT NOT NULL, type TEXT NOT NULL, path TEXT NOT NULL, PRIMARY KEY (name)) WITHOUT ROWID;";
        private static void CreateDatabaseTableIfNotExists()
        {
            using (SqliteConnection connection = new(CONNECTION_STRING))
            {
                connection.Open();

                using (SqliteCommand command = connection.CreateCommand())
                {
                    command.CommandText = CREATE_TABLE_COMMAND;

                    _ = command.ExecuteNonQuery();
                }
            }
        }
        public DataSourceRepository(in string connectionString)
        {
            CONNECTION_STRING = connectionString;

            CreateDatabaseTableIfNotExists();
        }

        private const string SELECT_ALL_COMMAND = "SELECT name, type, path FROM datasources ORDER BY name ASC;";
        public override bool TryGet(out List<DataSourceRecord> table, out string error)
        {
            error = null;
            table = new List<DataSourceRecord>();

            try
            {
                using (SqliteConnection connection = new(CONNECTION_STRING))
                {
                    connection.Open();

                    using (SqliteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = SELECT_ALL_COMMAND;

                        using (SqliteDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DataSourceRecord record = new()
                                {
                                    Name = reader.GetString(0),
                                    Type = reader.GetString(1),
                                    Path = reader.GetString(2)
                                };

                                table.Add(record);
                            }

                            reader.Close();
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                error = exception.Message;
            }

            return error is null;
        }

        private const string UPSERT_RECORD_COMMAND = "INSERT INTO datasources (name, type, path) VALUES (@name, @type, @path) ON CONFLICT (name) DO UPDATE SET type = @type, path = @path;";
        public override bool TrySave(in DataSourceRecord record, out string error)
        {
            error = null;

            try
            {
                using (SqliteConnection connection = new(CONNECTION_STRING))
                {
                    connection.Open();

                    using (SqliteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = UPSERT_RECORD_COMMAND;

                        command.Parameters.AddWithValue("name", record.Name);
                        command.Parameters.AddWithValue("type", record.Type);
                        command.Parameters.AddWithValue("path", record.Path);

                        int rowsAffected = command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception exception)
            {
                error = exception.Message;
            }

            return error is null;
        }

        private const string SELECT_BY_NAME_COMMAND = "SELECT name, type, path FROM datasources WHERE name = @name;";
        public override bool TryGet(in string key, out DataSourceRecord record, out string error)
        {
            error = null;
            record = null;

            try
            {
                using (SqliteConnection connection = new(CONNECTION_STRING))
                {
                    connection.Open();

                    using (SqliteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = SELECT_BY_NAME_COMMAND;

                        command.Parameters.AddWithValue("name", key);

                        using (SqliteDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                record = new DataSourceRecord()
                                {
                                    Name = reader.GetString(0),
                                    Type = reader.GetString(1),
                                    Path = reader.GetString(2)
                                };
                            }

                            reader.Close();
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                error = exception.Message;
            }

            return error is null;
        }

        private const string DELETE_BY_NAME_COMMAND = "DELETE FROM datasources WHERE name = @name;";
        public override bool TryDelete(in string key, out string error)
        {
            error = null;

            try
            {
                using (SqliteConnection connection = new(CONNECTION_STRING))
                {
                    connection.Open();

                    using (SqliteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = DELETE_BY_NAME_COMMAND;

                        command.Parameters.AddWithValue("name", key);

                        int rowsAffected = command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception exception)
            {
                error = exception.Message;
            }

            return error is null;
        }
    }
}