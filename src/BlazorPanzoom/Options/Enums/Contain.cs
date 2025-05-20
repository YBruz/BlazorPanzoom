using System.Text.Json.Serialization;
using BlazorPanzoom.Converters;

namespace BlazorPanzoom.Options.Enums;

[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum Contain
{
	[JsonPropertyName("inside")] Inside,
	[JsonPropertyName("outside")] Outside,
	[JsonPropertyName("none")] None
}