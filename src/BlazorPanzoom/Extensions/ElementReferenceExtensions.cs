using Microsoft.AspNetCore.Components;

namespace BlazorPanzoom.Extensions;

public static class ElementReferenceExtensions
{
	private static ElementReference DefaultElementReference => default;

	public static bool IsDefault(this ElementReference elementReference)
	{
		return elementReference.Equals(DefaultElementReference);
	}
}