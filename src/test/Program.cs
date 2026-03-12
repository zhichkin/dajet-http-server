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
                "pg-test", "Справочник", "Номенклатура");

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
        }
    }
}
