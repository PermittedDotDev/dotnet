using System.Text.Json.Serialization;

namespace Permitted.SDK.Models;

/// <summary>
/// Remote configuration result.
/// </summary>
public sealed class ConfigResult
{
    /// <summary>Configuration variables as key-value pairs.</summary>
    [JsonPropertyName("variables")]
    public required Dictionary<string, object?> Variables { get; init; }

    /// <summary>
    /// Gets a variable value as a specific type.
    /// </summary>
    /// <typeparam name="T">The expected type.</typeparam>
    /// <param name="key">Variable key.</param>
    /// <param name="defaultValue">Default value if not found or wrong type.</param>
    /// <returns>The variable value or default.</returns>
    public T Get<T>(string key, T defaultValue = default!)
    {
        if (!Variables.TryGetValue(key, out var value) || value is null)
        {
            return defaultValue;
        }

        try
        {
            if (value is T typedValue)
            {
                return typedValue;
            }

            // Handle JSON element conversion
            if (value is System.Text.Json.JsonElement element)
            {
                return element.Deserialize<T>() ?? defaultValue;
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Gets a string variable.
    /// </summary>
    public string? GetString(string key) => Get<string?>(key, null);

    /// <summary>
    /// Gets an integer variable.
    /// </summary>
    public int GetInt(string key, int defaultValue = 0) => Get(key, defaultValue);

    /// <summary>
    /// Gets a boolean variable.
    /// </summary>
    public bool GetBool(string key, bool defaultValue = false) => Get(key, defaultValue);

    /// <summary>
    /// Gets a double variable.
    /// </summary>
    public double GetDouble(string key, double defaultValue = 0.0) => Get(key, defaultValue);
}
