using DaJet.Data;
using DaJet.Http.Client;
using DaJet.Http.Model;
using DaJet.Json;
using DaJet.Metadata;
using DaJet.Scripting;
using DaJet.TypeSystem;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace test
{
    internal class Program
    {
        private static readonly string MS_TEST = "Data Source=ZHICHKIN;Initial Catalog=dajet-metadata;Integrated Security=True;Encrypt=False;";
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };
        static void Main(string[] args)
        {
            JsonOptions.Converters.Add(new DictionaryJsonConverter());

            MetadataProvider.Add("MS_TEST", DataSourceType.SqlServer, in MS_TEST);

            //TestHttpClient();
            //ExecuteQuery();
            //PostExecuteQuery();
            PostExecuteScript();
        }
        private static void TestHttpClient()
        {
            DaJetHttpClient client = new(new HttpClient()
            {
                BaseAddress = new Uri("http://localhost:5000")
            });

            Task<RequestResult<EntityDefinition>> task = client.GetMetadataObject(
                "PG_UNF", "РегистрНакопления", "АвансовыеПлатежиИностранцевПоНДФЛ");

            task.Wait();

            RequestResult<EntityDefinition> response = task.Result;

            if (response.Success)
            {
                Console.WriteLine("Успех");
            }
            else
            {
                Console.WriteLine(response.Message);
            }

            PropertyDefinition property = response.Result.Properties
                .Where(p => p.Name == "Регистратор").FirstOrDefault();

            Task<RequestResult<List<string>>> task2 = client.ResolveReferences("PG_UNF", property.References);

            task2.Wait();

            RequestResult<List<string>> result = task2.Result;

            foreach (string name in result.Result)
            {
                Console.WriteLine(name);
            }
        }
        private static void ExecuteQuery()
        {
            Dictionary<string, object> parameters = new()
            {
                { "Булево", true },
                { "ЦелоеЧисло", 12345 },
                { "БольшоеЧисло", 12345L },
                { "ДесятичноеЧисло", 12.34M },
                { "ДатаВремя", DateTime.Now },
                { "Строка", "000000002" },
                { "ДвоичноеЧисло", Convert.FromBase64String("DEADBEEF") },
                { "Идентификатор", new Guid("41F517C5-BC81-45E6-A9E8-7A2C8F573117") },
                { "ПустаяСсылка", Entity.Undefined },
                { "Код", "000000001"}
            };

            string source =
                "DECLARE @Код string " +
                "DECLARE @Таблица array " +
                "USE 'MS_TEST' " +
                "SELECT Ссылка, Код, Наименование INTO @Таблица " +
                "FROM Справочник.Номенклатура WHERE Код = @Код " +
                "END " +
                "RETURN JSON(@Таблица)";

            Interpreter interpreter = new(in source);

            object value = interpreter.Execute(in parameters);

            string json;

            if (value is null)
            {
                json = "Value is NULL";
            }
            else if (value is string text)
            {
                json = text;
            }
            else
            {
                json = JsonSerializer.Serialize(value, value.GetType(), JsonOptions);
            }

            Console.WriteLine(json);
        }
        private static void PostExecuteQuery()
        {
            DaJetHttpClient client = new(new HttpClient()
            {
                BaseAddress = new Uri("http://localhost:5000")
            });

            QueryRequest request = new()
            {
                Database = "ms-test",
                Script =
                "SELECT Ссылка, Код, Наименование " +
                "FROM Справочник.Номенклатура " +
                "WHERE Код = @Код",
                Parameters = new Dictionary<string, object>()
                {
                    ["Код"] = "000000001"
                }
            };

            Task<QueryResponse> task = client.ExecuteQuery(request);

            task.Wait();

            QueryResponse response = task.Result;

            Console.WriteLine($"Success: {response.Success}");
            Console.WriteLine($"Message: {response.Message}");
            Console.WriteLine("Result:");
            Console.WriteLine(response.Result);
        }
        private static void PostExecuteScript()
        {
            DaJetHttpClient client = new(new HttpClient()
            {
                BaseAddress = new Uri("http://localhost:5000")
            });

            Dictionary<string, object> parameters = new()
            {
                ["Код"] = "000000001"
            };

            Task<QueryResponse> task = client.ExecuteScript("test1.djs", parameters);

            task.Wait();

            QueryResponse response = task.Result;

            Console.WriteLine($"Success: {response.Success}");
            Console.WriteLine($"Message: {response.Message}");
            Console.WriteLine("Result:");
            Console.WriteLine(response.Result);
        }
    }
}
