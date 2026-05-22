using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebSecurityAuditor.Api;

public static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };
}
