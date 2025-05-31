using EntitiesManager.Core.Entities;
using EntitiesManager.Core.Entities.Base;
using EntitiesManager.Infrastructure.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System.Text.Json;

namespace EntitiesManager.Infrastructure.MongoDB;

// Custom serializer for JsonElement to handle JSON objects in MongoDB
public class JsonElementSerializer : SerializerBase<JsonElement>
{
    public override JsonElement Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var bsonReader = context.Reader;
        var bsonType = bsonReader.GetCurrentBsonType();

        switch (bsonType)
        {
            case BsonType.Document:
                var document = BsonDocumentSerializer.Instance.Deserialize(context, args);
                var json = document.ToJson();
                return JsonDocument.Parse(json).RootElement;
            case BsonType.Array:
                var array = BsonArraySerializer.Instance.Deserialize(context, args);
                var arrayJson = array.ToJson();
                return JsonDocument.Parse(arrayJson).RootElement;
            case BsonType.String:
                var stringValue = bsonReader.ReadString();
                return JsonDocument.Parse($"\"{stringValue}\"").RootElement;
            case BsonType.Int32:
                var intValue = bsonReader.ReadInt32();
                return JsonDocument.Parse(intValue.ToString()).RootElement;
            case BsonType.Int64:
                var longValue = bsonReader.ReadInt64();
                return JsonDocument.Parse(longValue.ToString()).RootElement;
            case BsonType.Double:
                var doubleValue = bsonReader.ReadDouble();
                return JsonDocument.Parse(doubleValue.ToString()).RootElement;
            case BsonType.Boolean:
                var boolValue = bsonReader.ReadBoolean();
                return JsonDocument.Parse(boolValue.ToString().ToLower()).RootElement;
            case BsonType.Null:
                bsonReader.ReadNull();
                return JsonDocument.Parse("null").RootElement;
            default:
                throw new BsonSerializationException($"Cannot deserialize BsonType {bsonType} to JsonElement");
        }
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JsonElement value)
    {
        var bsonWriter = context.Writer;

        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                var document = BsonDocument.Parse(value.GetRawText());
                BsonDocumentSerializer.Instance.Serialize(context, document);
                break;
            case JsonValueKind.Array:
                var arrayDocument = BsonDocument.Parse($"{{\"array\":{value.GetRawText()}}}");
                var array = arrayDocument["array"].AsBsonArray;
                BsonArraySerializer.Instance.Serialize(context, array);
                break;
            case JsonValueKind.String:
                bsonWriter.WriteString(value.GetString());
                break;
            case JsonValueKind.Number:
                if (value.TryGetInt32(out var intVal))
                    bsonWriter.WriteInt32(intVal);
                else if (value.TryGetInt64(out var longVal))
                    bsonWriter.WriteInt64(longVal);
                else
                    bsonWriter.WriteDouble(value.GetDouble());
                break;
            case JsonValueKind.True:
                bsonWriter.WriteBoolean(true);
                break;
            case JsonValueKind.False:
                bsonWriter.WriteBoolean(false);
                break;
            case JsonValueKind.Null:
                bsonWriter.WriteNull();
                break;
            default:
                throw new BsonSerializationException($"Cannot serialize JsonValueKind {value.ValueKind} to BSON");
        }
    }
}

public static class BsonConfiguration
{
    private static bool _isConfigured = false;
    private static readonly object _lock = new object();

    public static void Configure()
    {
        if (_isConfigured) return;

        lock (_lock)
        {
            if (_isConfigured) return;

            // Register custom GUID generator for auto-generation
            BsonSerializer.RegisterIdGenerator(typeof(Guid), new GuidGenerator());

            // Configure GUID serialization as string
            BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));

            // Register JsonElement serializer for handling JSON objects in Configuration
            BsonSerializer.RegisterSerializer(typeof(JsonElement), new JsonElementSerializer());

            // Register object serializer that can handle JsonElement
            var objectSerializer = new ObjectSerializer(type => type == typeof(JsonElement) || ObjectSerializer.DefaultAllowedTypes(type));
            BsonSerializer.RegisterSerializer(typeof(object), objectSerializer);

        // Register class maps for entities to ensure proper ID generation
        if (!BsonClassMap.IsClassMapRegistered(typeof(BaseEntity)))
        {
            BsonClassMap.RegisterClassMap<BaseEntity>(cm =>
            {
                cm.AutoMap();
                cm.MapIdProperty(x => x.Id)
                  .SetIdGenerator(new GuidGenerator())
                  .SetSerializer(new GuidSerializer(BsonType.String));
                cm.SetIgnoreExtraElements(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(SourceEntity)))
        {
            BsonClassMap.RegisterClassMap<SourceEntity>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(StepEntity)))
        {
            BsonClassMap.RegisterClassMap<StepEntity>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(DestinationEntity)))
        {
            BsonClassMap.RegisterClassMap<DestinationEntity>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(ProtocolEntity)))
        {
            BsonClassMap.RegisterClassMap<ProtocolEntity>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(ImporterEntity)))
        {
            BsonClassMap.RegisterClassMap<ImporterEntity>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(ExporterEntity)))
        {
            BsonClassMap.RegisterClassMap<ExporterEntity>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(ProcessorEntity)))
        {
            BsonClassMap.RegisterClassMap<ProcessorEntity>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(FlowEntity)))
        {
            BsonClassMap.RegisterClassMap<FlowEntity>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(OrchestratedFlowEntity)))
        {
            BsonClassMap.RegisterClassMap<OrchestratedFlowEntity>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(AssignmentEntity)))
        {
            BsonClassMap.RegisterClassMap<AssignmentEntity>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }

        _isConfigured = true;
        }
    }
}
