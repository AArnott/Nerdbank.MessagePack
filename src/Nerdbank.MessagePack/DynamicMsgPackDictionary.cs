// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

namespace Nerdbank.MessagePack;

/// <summary>
/// A dictionary of untyped, deserialized objects and values.
/// </summary>
/// <param name="underlying"><inheritdoc cref="IntegerStretchingDictionary" path="/param[@name='underlying']"/></param>
/// <remarks>
/// <para>
/// This dictionary may be used with the C# <c>dynamic</c> keyword to expose its members as properties as well as through its indexer.
/// For example, if the dictionary contains a key "Foo", then <c>d.Foo</c> will return the value associated with that key as well as <c>d["Foo"]</c>.
/// Non-string keys are not supported as properties, but they can still be accessed through the indexer.
/// </para>
/// </remarks>
internal class DynamicMsgPackDictionary(IReadOnlyDictionary<object, object?> underlying) : IntegerStretchingDictionary(underlying), IDynamicMetaObjectProvider
{
	/// <inheritdoc/>
	DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new ReadOnlyMetaExpando(parameter, this);

	private class ReadOnlyMetaExpando(Expression expression, DynamicMsgPackDictionary dictionary) : DynamicMetaObject(expression, BindingRestrictions.Empty, dictionary)
	{
		private static readonly MethodInfo TryGetMemberMethodInfo = typeof(ReadOnlyMetaExpando).GetMethod("TryGetMember", BindingFlags.Static | BindingFlags.NonPublic)!;
		private static readonly MethodInfo TryGetIndexMethodInfo = typeof(ReadOnlyMetaExpando).GetMethod("TryGetIndex", BindingFlags.Static | BindingFlags.NonPublic)!;
		private static readonly ConstructorInfo KeyNotFoundExceptionCtor = typeof(KeyNotFoundException).GetConstructor([typeof(string)])!;

		public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
		{
			// Support: d[42] or d["foo"]
			ParameterExpression value = Expression.Parameter(typeof(object), "value");

			if (indexes.Length != 1)
			{
				// If there are multiple indexes, we cannot handle this with a single call to TryGetIndex.
				// Fallback to the default binder behavior.
				return binder.FallbackGetIndex(this, indexes);
			}

			Expression tryGetIndex = Expression.Call(
				TryGetIndexMethodInfo,
				this.GetLimitedSelf(),
				Expression.Convert(indexes[0].Expression, typeof(object)),
				value);

			DynamicMetaObject fallback = new(
				Expression.Throw(Expression.New(KeyNotFoundExceptionCtor, Expression.Constant($"This msgpack deserialized object does not have a \"{indexes[0].Value}\" member.")), typeof(object)),
				binder.FallbackGetIndex(this, indexes).Restrictions);
			DynamicMetaObject result = new(value, BindingRestrictions.Empty);

			return new DynamicMetaObject(
				Expression.Block(
					[value],
					Expression.Condition(tryGetIndex, result.Expression, fallback.Expression, typeof(object))),
				result.Restrictions.Merge(fallback.Restrictions));
		}

		public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
		{
			// Support: d.Foo
			ParameterExpression value = Expression.Parameter(typeof(object), "value");

			Expression tryGetValue = Expression.Call(
				TryGetMemberMethodInfo,
				this.GetLimitedSelf(),
				Expression.Constant(binder.Name),
				value);

			DynamicMetaObject fallback = new(
				Expression.Throw(Expression.New(KeyNotFoundExceptionCtor, Expression.Constant($"This msgpack deserialized object does not have a \"{binder.Name}\" member.")), typeof(object)),
				binder.FallbackGetMember(this).Restrictions);
			DynamicMetaObject result = new(value, BindingRestrictions.Empty);

			return new DynamicMetaObject(
				Expression.Block(
					[value],
					Expression.Condition(tryGetValue, result.Expression, fallback.Expression, typeof(object))),
				result.Restrictions.Merge(fallback.Restrictions));
		}

		public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
		{
			// This dictionary is not callable, so always fallback.
			return binder.FallbackInvokeMember(this, args);
		}

		private static bool TryGetMember(DynamicMsgPackDictionary self, string name, out object? result) => self.TryGetValue(name, out result);

		private static bool TryGetIndex(DynamicMsgPackDictionary dictionary, object index, out object? result) => dictionary.TryGetValue(index, out result);

		private static bool AreEquivalent(Type? t1, Type? t2) => t1 != null && t1.IsEquivalentTo(t2);

		/// <summary>
		/// Returns our Expression converted to our known LimitType.
		/// </summary>
		private Expression GetLimitedSelf()
		{
			if (AreEquivalent(this.Expression.Type, this.LimitType))
			{
				return this.Expression;
			}

			return Expression.Convert(this.Expression, this.LimitType);
		}
	}
}
