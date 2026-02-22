using DaJet.Data;
using DaJet.Http.Model;
using Microsoft.AspNetCore.Cors.Infrastructure;
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
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSystemd();
            builder.Host.UseWindowsService();

            builder.Services.AddSingleton(new RepositoryFactory(in CONNECTION_STRING));
            builder.Services.AddControllers();
            builder.Services.AddCors(ConfigureCors);

            WebApplication app = builder.Build();

            InitializeMetadataCache(app.Services);

            app.UseHttpsRedirection();
            app.UseCors();
            //app.UseAuthentication();
            //app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
        private static void ConfigureCors(CorsOptions options)
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            });
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
                throw new InvalidOperationException("Обязательный сервис 'DataSourceRepository' не найден.");
            }

            if (!repository.TryGet(out List<DataSourceRecord> list, out string error))
            {
                throw new InvalidOperationException(error);
            }

            foreach (DataSourceRecord record in list)
            {
                if (Enum.TryParse(record.Type, out DataSourceType dataSource))
                {
                    try
                    {
                        MetadataCache.Add(record.Name, dataSource, record.Path);
                    }
                    catch (Exception exception)
                    {
                        string message = $"Ошибка регистрации источника данных '{record.Name}': {exception.Message}";

                        FileLogger.Default.Write(message);
                    }
                }
            }
        }
    }
}