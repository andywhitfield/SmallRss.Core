using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SmallRss
{
    public static class SerializeExtensions
    {
        public static bool TryParseJson<T>(this string jsonString, out T result, ILogger logger = null)
        {
            try
            {
                result = JsonSerializer.Deserialize<T>(jsonString);
                if (result != null)
                    return true;

                logger?.LogWarning("Cannot deserialize json: " + jsonString);
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, $"Error attempting to deserialize: {jsonString}");
            }
            result = default(T);
            return false;
        }
    }
}