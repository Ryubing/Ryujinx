using Ryujinx.Common.Utilities;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ryujinx.Common.Configuration.Hid.Controller.Motion
{
    class JsonMotionConfigControllerConverter : JsonConverter<MotionConfigController>
    {
        private static readonly MotionConfigJsonSerializerContext _serializerContext = new(JsonHelper.GetDefaultSerializerOptions());

        private static MotionInputBackendType GetMotionInputBackendType(ref Utf8JsonReader reader)
        {
            // Temporary reader to get the backend type
            Utf8JsonReader tempReader = reader;

            MotionInputBackendType result = MotionInputBackendType.Invalid;

            while (tempReader.Read())
            {
                // NOTE: We scan all properties ignoring the depth entirely on purpose.
                // The reason behind this is that we cannot track in a reliable way the depth of the object because Utf8JsonReader never emit the first TokenType == StartObject if the json start with an object.
                // As such, this code will try to parse very field named "motion_backend" to the correct enum.
                if (tempReader.TokenType == JsonTokenType.PropertyName)
                {
                    string propertyName = tempReader.GetString();

                    if (propertyName.Equals("motion_backend"))
                    {
                        tempReader.Read();

                        if (tempReader.TokenType == JsonTokenType.String)
                        {
                            string backendTypeRaw = tempReader.GetString();

                            if (!Enum.TryParse(backendTypeRaw, out result))
                            {
                                result = MotionInputBackendType.Invalid;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }

            return result;
        }

        public override MotionConfigController Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            MotionInputBackendType motionBackendType = GetMotionInputBackendType(ref reader);

            return motionBackendType switch
            {
                MotionInputBackendType.GamepadDriver => JsonSerializer.Deserialize(ref reader, _serializerContext.StandardMotionConfigController),
                MotionInputBackendType.Handheld => JsonSerializer.Deserialize(ref reader, _serializerContext.StandardMotionConfigController),
                MotionInputBackendType.CemuHook => JsonSerializer.Deserialize(ref reader, _serializerContext.CemuHookMotionConfigController),
                _ => throw new InvalidOperationException($"Unknown backend type {motionBackendType}"),
            };
        }

        public override void Write(Utf8JsonWriter writer, MotionConfigController value, JsonSerializerOptions options)
        {
            switch (value.MotionBackend)
            {
                case MotionInputBackendType.GamepadDriver:
                case MotionInputBackendType.Handheld:
                    JsonSerializer.Serialize(writer, value as StandardMotionConfigController, _serializerContext.StandardMotionConfigController);
                    break;
                case MotionInputBackendType.CemuHook:
                    JsonSerializer.Serialize(writer, value as CemuHookMotionConfigController, _serializerContext.CemuHookMotionConfigController);
                    break;
                default:
                    throw new ArgumentException($"Unknown motion backend type {value.MotionBackend}");
            }
        }
    }
}
