using System.Text.Json;

namespace qenem.Services
{
    public class JsonDataService
    {
        private readonly string _basePath;

        public JsonDataService(string basePath)
        {
            _basePath = basePath;
        }
        public List<T> LoadJson<T>(string relativePath)
        {
            var fullPath = Path.Combine(_basePath, relativePath);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Arquivo não encontrado: {fullPath}");

            var json = File.ReadAllText(fullPath);
            return JsonSerializer.Deserialize<List<T>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<T>();
        }
    }
}
