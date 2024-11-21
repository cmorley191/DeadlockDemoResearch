using System.Text.Json;
using System.Text.Json.Serialization;
using System.Numerics;

namespace DeadlockDemoResearch
{
  public class CustomJsonSerializerVector3Converter : JsonConverter<Vector3>
  {
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      throw new NotImplementedException("Deserialization is not needed.");
    }

    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
      writer.WriteStartObject();
      writer.WriteNumber("X", value.X);
      writer.WriteNumber("Y", value.Y);
      writer.WriteNumber("Z", value.Z);
      writer.WriteEndObject();
    }
  }
}
