using DaJet.Data;
using DaJet.Http.Model;
using Microsoft.Data.Sqlite;
using MetadataCache = DaJet.Metadata.MetadataProvider;

namespace DaJet.Http.Server
{
    public class Program
    {
        private static readonly string APP_DATABASE_NAME = "dajet.db";
        private static readonly string CONNECTION_STRING = BuildAppDatabaseConnectionString();

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddSingleton(new RepositoryFactory(in CONNECTION_STRING));

            builder.Services.AddControllers();

            var app = builder.Build();

            InitializeMetadataCache(app.Services);

            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
        private static string BuildAppDatabaseConnectionString()
        {
            string fullPath = Path.Combine(AppContext.BaseDirectory, APP_DATABASE_NAME);

            return new SqliteConnectionStringBuilder()
            {
                DataSource = fullPath, Mode = SqliteOpenMode.ReadWriteCreate
            }
            .ToString();
        }
        private static void InitializeMetadataCache(in IServiceProvider services)
        {
            RepositoryFactory factory = services.GetRequiredService<RepositoryFactory>();

            DataSourceRepository repository = factory.Get<DataSourceRepository>();

            if (repository is null)
            {
                throw new InvalidOperationException("Required DataSourceRepository is not found.");
            }

            if (!repository.TryGet(out List<DataSourceRecord> list, out string error))
            {
                throw new InvalidOperationException(error);
            }

            foreach (DataSourceRecord record in list)
            {
                if (Enum.TryParse(record.Type, out DataSourceType dataSource))
                {
                    MetadataCache.Add(record.Name, dataSource, record.Path);
                }
            }
        }
    }
}