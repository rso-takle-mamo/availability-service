using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;

namespace AvailabilityService.Api.Configuration;

public static class KafkaConstants
{
    public static JsonSerializerOptions JsonSerializerOptions => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static AutoOffsetReset ParseAutoOffsetReset(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "earliest" => AutoOffsetReset.Earliest,
            "latest" => AutoOffsetReset.Latest,
            "none" or "error" => AutoOffsetReset.Error,
            _ => AutoOffsetReset.Earliest
        };
    }
}
