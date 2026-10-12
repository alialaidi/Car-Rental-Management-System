using System.Text.Json;
using System.Text.Json.Nodes;

namespace _CarRental.Infrastructure.Auditing;

public static class AuditValueSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private const string RedactedValue = "[REDACTED]";

    public static string? Serialize(object? value)
    {
        if (value is null)
        {
            return null;
        }

        var node = JsonSerializer.SerializeToNode(value, value.GetType(), SerializerOptions);
        return Redact(node)?.ToJsonString(SerializerOptions);
    }

    private static JsonNode? Redact(JsonNode? node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var propertyName in jsonObject.Select(property => property.Key).ToArray())
            {
                if (IsSensitive(propertyName))
                {
                    jsonObject[propertyName] = JsonValue.Create(RedactedValue);
                }
                else
                {
                    Redact(jsonObject[propertyName]);
                }
            }
        }
        else if (node is JsonArray jsonArray)
        {
            foreach (var item in jsonArray)
            {
                Redact(item);
            }
        }

        return node;
    }

    private static bool IsSensitive(string propertyName) =>
        propertyName.Contains("password", StringComparison.OrdinalIgnoreCase)
        || propertyName.Contains("token", StringComparison.OrdinalIgnoreCase);


}
