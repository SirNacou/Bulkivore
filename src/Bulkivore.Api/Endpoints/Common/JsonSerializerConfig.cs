using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bulkivore.Api.Endpoints.Common;

public static class JsonSerializerConfig
{
    private static readonly Lazy<JsonSerializerOptions> DefaultOptions = new(CreateDefaultOptions);

    public static JsonSerializerOptions Default => DefaultOptions.Value;

    public static JsonSerializerOptions CreateDefaultOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Configure();
        return options;
    }

    extension(JsonSerializerOptions options)
    {
        public void Configure()
        {
            options.PropertyNameCaseInsensitive = true;
            options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            options.PropertyNameCaseInsensitive = true;
            options.Converters.Add(new JsonStringEnumConverter());

            // Register custom converters here (e.g., Vogen, DateOnly, etc.)
        }
    }
}
