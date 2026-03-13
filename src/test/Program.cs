using DaJet.Http.Client;
using DaJet.TypeSystem;

namespace test
{
    internal class Program
    {
        static void Main(string[] args)
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
    }
}
