// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using PolyType.Roslyn;

namespace Nerdbank.MessagePack.Analyzers;

internal class PolyTypeShapeSynthesis : TypeDataModelGenerator
{
	private readonly ReferenceSymbols referenceSymbols;

	public PolyTypeShapeSynthesis(ISymbol generationScope, KnownSymbols knownSymbols, ReferenceSymbols referenceSymbols, CancellationToken cancellationToken)
		: base(generationScope, knownSymbols, cancellationToken)
	{
		this.referenceSymbols = referenceSymbols;
	}

	protected override bool IncludeField(IFieldSymbol field, out string? customName, out int order, out bool includeGetter, out bool includeSetter)
	{
		AttributeData? attribute = field.FindAttributes(this.referenceSymbols.PropertyShapeAttribute).FirstOrDefault();
		if (attribute is null)
		{
			return base.IncludeField(field, out customName, out order, out includeGetter, out includeSetter);
		}

		customName = null;
		order = 0;
		includeGetter = true;
		includeSetter = !field.IsReadOnly;
		return !IsIgnored(attribute);
	}

	protected override bool IncludeProperty(IPropertySymbol property, out string? customName, out int order, out bool includeGetter, out bool includeSetter)
	{
		AttributeData? attribute = property.FindAttributes(this.referenceSymbols.PropertyShapeAttribute).FirstOrDefault();
		if (attribute is null)
		{
			return base.IncludeProperty(property, out customName, out order, out includeGetter, out includeSetter);
		}

		customName = null;
		order = 0;
		includeGetter = property.GetMethod is not null;
		includeSetter = property.SetMethod is not null;
		return !IsIgnored(attribute);
	}

	private static bool IsIgnored(AttributeData attribute) =>
		attribute.NamedArguments.FirstOrDefault(a => a.Key == Constants.PropertyShapeAttribute.IgnoreProperty).Value.Value as bool? is true;
}
