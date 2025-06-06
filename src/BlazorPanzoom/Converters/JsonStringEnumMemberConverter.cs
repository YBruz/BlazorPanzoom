﻿// Licensed to Macross Software under the MIT license.
// https://github.com/Macross-Software/core/tree/develop/ClassLibraries/Macross.Json.Extensions
//
// Copyright (c) 2020 Macross Software
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// 	of this software and associated documentation files (the "Software"), to deal
// 	in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// 	furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// 	copies or substantial portions of the Software.
//
// 	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// 	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// 	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// 	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// 	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlazorPanzoom.Converters;

/// <summary>
/// <see cref="JsonConverterFactory" /> to convert enums to and from strings, respecting <see cref="EnumMemberAttribute" /> decorations. Supports nullable enums.
/// </summary>
public class JsonStringEnumMemberConverter : JsonConverterFactory
{
	private readonly HashSet<Type>? _EnumTypes;
	private readonly JsonStringEnumMemberConverterOptions? _Options;

	/// <summary>
	/// Initializes a new instance of the <see cref="JsonStringEnumMemberConverter" /> class.
	/// </summary>
	public JsonStringEnumMemberConverter()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="JsonStringEnumMemberConverter" /> class.
	/// </summary>
	/// <param name="namingPolicy">
	/// Optional naming policy for writing enum values.
	/// </param>
	/// <param name="allowIntegerValues">
	/// True to allow undefined enum values. When true, if an enum value isn't
	///  defined it will output as a number rather than a string.
	/// </param>
	public JsonStringEnumMemberConverter(JsonNamingPolicy? namingPolicy = null, bool allowIntegerValues = true)
		: this(new JsonStringEnumMemberConverterOptions
			{ NamingPolicy = namingPolicy, AllowIntegerValues = allowIntegerValues })
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="JsonStringEnumMemberConverter" /> class.
	/// </summary>
	/// <param name="options"><see cref="JsonStringEnumMemberConverterOptions" />.</param>
	/// <param name="targetEnumTypes">Optional list of supported enum types to be converted. Specify <see langword="null" /> or empty to convert all enums.</param>
	public JsonStringEnumMemberConverter(JsonStringEnumMemberConverterOptions options,
		params Type[] targetEnumTypes)
	{
		_Options = options ?? throw new ArgumentNullException(nameof(options));

		if (targetEnumTypes != null && targetEnumTypes.Length > 0)
		{
#if NETSTANDARD2_0
				_EnumTypes = new HashSet<Type>();
#else
			_EnumTypes = new HashSet<Type>(targetEnumTypes.Length);
#endif
			foreach (var enumType in targetEnumTypes)
			{
				if (enumType.IsEnum)
				{
					_EnumTypes.Add(enumType);
					_EnumTypes.Add(typeof(Nullable<>).MakeGenericType(enumType));
					continue;
				}

				if (enumType.IsGenericType)
				{
					var (IsNullableEnum, UnderlyingType) = TestNullableEnum(enumType);
					if (IsNullableEnum)
					{
						_EnumTypes.Add(UnderlyingType!);
						_EnumTypes.Add(enumType);
						continue;
					}
				}

				throw new NotSupportedException(
					$"Type {enumType} is not supported by JsonStringEnumMemberConverter. Only enum types can be converted.");
			}
		}
	}

#pragma warning disable CA1062 // Validate arguments of public methods
	/// <inheritdoc />
	public override bool CanConvert(Type typeToConvert)
	{
		// Don't perform a typeToConvert == null check for performance. Trust our callers will be nice.
		return _EnumTypes != null
			? _EnumTypes.Contains(typeToConvert)
			: typeToConvert.IsEnum
			  || (typeToConvert.IsGenericType && TestNullableEnum(typeToConvert).IsNullableEnum);
	}
#pragma warning restore CA1062 // Validate arguments of public methods

	/// <inheritdoc />
	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		var (IsNullableEnum, UnderlyingType) = TestNullableEnum(typeToConvert);

		try
		{
			return IsNullableEnum
				? (JsonConverter)Activator.CreateInstance(
					typeof(NullableEnumMemberConverter<>).MakeGenericType(UnderlyingType),
					BindingFlags.Instance | BindingFlags.Public,
					null,
					[_Options],
					null)
				: (JsonConverter)Activator.CreateInstance(
					typeof(EnumMemberConverter<>).MakeGenericType(typeToConvert),
					BindingFlags.Instance | BindingFlags.Public,
					null,
					[_Options],
					null);
		}
		catch (TargetInvocationException targetInvocationEx)
		{
			if (targetInvocationEx.InnerException != null)
				throw targetInvocationEx.InnerException;
			throw;
		}
	}

	private static (bool IsNullableEnum, Type? UnderlyingType) TestNullableEnum(Type typeToConvert)
	{
		var UnderlyingType = Nullable.GetUnderlyingType(typeToConvert);

		return (UnderlyingType?.IsEnum ?? false, UnderlyingType);
	}

	internal static ulong GetEnumValue(TypeCode enumTypeCode, object value)
	{
		return enumTypeCode switch
		{
			TypeCode.Int32 => (ulong)(int)value,
			TypeCode.Int64 => (ulong)(long)value,
			TypeCode.Int16 => (ulong)(short)value,
			TypeCode.Byte => (byte)value,
			TypeCode.UInt32 => (uint)value,
			TypeCode.UInt64 => (ulong)value,
			TypeCode.UInt16 => (ushort)value,
			TypeCode.SByte => (ulong)(sbyte)value,
			_ => throw new NotSupportedException($"Enum '{value}' of {enumTypeCode} type is not supported.")
		};
	}

#pragma warning disable CA1812 // Remove class never instantiated
	private class EnumMemberConverter<TEnum> : JsonConverter<TEnum>
		where TEnum : struct, Enum
#pragma warning restore CA1812 // Remove class never instantiated
	{
		private readonly JsonStringEnumMemberConverterHelper<TEnum> _JsonStringEnumMemberConverterHelper;

		public EnumMemberConverter(JsonStringEnumMemberConverterOptions? options)
		{
			_JsonStringEnumMemberConverterHelper = new JsonStringEnumMemberConverterHelper<TEnum>(options);
		}

		public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			return _JsonStringEnumMemberConverterHelper.Read(ref reader);
		}

		public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
		{
			_JsonStringEnumMemberConverterHelper.Write(writer, value);
		}
	}

#pragma warning disable CA1812 // Remove class never instantiated
	private class NullableEnumMemberConverter<TEnum> : JsonConverter<TEnum?>
		where TEnum : struct, Enum
#pragma warning restore CA1812 // Remove class never instantiated
	{
		private readonly JsonStringEnumMemberConverterHelper<TEnum> _JsonStringEnumMemberConverterHelper;

		public NullableEnumMemberConverter(JsonStringEnumMemberConverterOptions? options)
		{
			_JsonStringEnumMemberConverterHelper = new JsonStringEnumMemberConverterHelper<TEnum>(options);
		}

		public override TEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			return _JsonStringEnumMemberConverterHelper.Read(ref reader);
		}

		public override void Write(Utf8JsonWriter writer, TEnum? value, JsonSerializerOptions options)
		{
			_JsonStringEnumMemberConverterHelper.Write(writer, value!.Value);
		}
	}
}