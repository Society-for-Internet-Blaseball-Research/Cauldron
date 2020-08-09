using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cauldron
{
	public class TimestampConverter : JsonConverter<DateTime>
	{
		private static DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			long milliseconds = reader.GetInt64();
			return unixEpoch.AddMilliseconds(milliseconds);
		}

		public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
		{
			var diff = value - unixEpoch;
			writer.WriteNumberValue(diff.TotalMilliseconds);
		}
	}
}
