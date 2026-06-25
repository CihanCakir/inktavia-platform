using MiniUow.Paging;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aizen.Core.Infrastructure.RemoteCall.Serialization;

/// <summary>
/// JsonConverterFactory that handles <see cref="Paginate{T}"/> deserialization for Refit clients.
/// <para>
/// MiniUow's <c>Paginate&lt;T&gt;</c> has only an internal parameterless constructor, which
/// System.Text.Json cannot use. This factory creates a typed converter per element type that
/// uses reflection to invoke the internal constructor and populate the settable properties
/// (From, Index, Size, Count, Pages, Items). HasPrevious and HasNext are computed properties.
/// </para>
/// </summary>
internal sealed class PaginateJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsGenericType
        && typeToConvert.GetGenericTypeDefinition() == typeof(Paginate<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var itemType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(PaginateJsonConverter<>).MakeGenericType(itemType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

/// <summary>
/// Typed converter that reads a JSON object into <see cref="Paginate{T}"/> via the internal
/// parameterless constructor and property setters.
/// </summary>
internal sealed class PaginateJsonConverter<T> : JsonConverter<Paginate<T>>
{
    private static readonly ConstructorInfo _ctor =
        typeof(Paginate<T>).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null)
        ?? throw new InvalidOperationException($"Could not find parameterless constructor on Paginate<{typeof(T).Name}>.");

    public override Paginate<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var instance = (Paginate<T>)_ctor.Invoke(null);

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected StartObject, got {reader.TokenType}.");

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return instance;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected PropertyName.");

            var propName = reader.GetString()!;
            reader.Read();

            switch (propName.ToLowerInvariant())
            {
                case "from":
                    instance.From = reader.GetInt32();
                    break;
                case "index":
                    instance.Index = reader.GetInt32();
                    break;
                case "size":
                    instance.Size = reader.GetInt32();
                    break;
                case "count":
                    instance.Count = reader.GetInt32();
                    break;
                case "pages":
                    instance.Pages = reader.GetInt32();
                    break;
                case "items":
                    instance.Items = JsonSerializer.Deserialize<IEnumerable<T>>(ref reader, options)!;
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        return instance;
    }

    public override void Write(Utf8JsonWriter writer, Paginate<T> value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, (object)value, options);
}
