// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using System.Collections.Frozen;

namespace CSharpUnionAliases;

internal static partial class Examples
{
    #region PrimitiveUnionCaseAliases
    [GenerateShape]
    public partial union TextOrNumber(string, int);

    internal static void CustomizePrimitiveAliases()
    {
        UnionCaseMapping mapping = new(typeof(TextOrNumber));
        mapping.Add("text", typeof(string));
        mapping.Add("number", typeof(int));

        MessagePackSerializer serializer = new()
        {
            UnionTypeAliasProvider = new UnionCaseMappingCollection([mapping]),
        };

        byte[] text = serializer.Serialize(new TextOrNumber("hello"));
        byte[] number = serializer.Serialize(new TextOrNumber(42));

        // Prints: ["text","hello"]
        Console.WriteLine(serializer.ConvertToJson(text));

        // Prints: ["number",42]
        Console.WriteLine(serializer.ConvertToJson(number));
    }
    #endregion

    #region UnionCaseAliasTypes
    public interface IError
    {
        static abstract string Name { get; }
    }

    public sealed record InvalidOtpError(int RemainingAttempts) : IError
    {
        public static string Name => "Sample.InvalidOtpError";
    }

    public sealed record UserNotFoundError : IError
    {
        public static string Name => "Sample.UserNotFoundError";
    }

    [GenerateShape]
    public partial union SignInError(InvalidOtpError, UserNotFoundError);
    #endregion

    #region UnionCaseAliasMapping
    internal static MessagePackSerializer ConfigureMappings()
    {
        UnionCaseMapping mapping = new(typeof(SignInError));
        Register<InvalidOtpError>();
        Register<UserNotFoundError>();

        return new MessagePackSerializer
        {
            UnionTypeAliasProvider = new UnionCaseMappingCollection([mapping]),
        };

        void Register<TError>()
            where TError : IError
        {
            mapping.Add(TError.Name, typeof(TError));
        }
    }
    #endregion

    #region UnionCaseAliasRegistry
    internal static MessagePackSerializer ConfigureRegistry()
    {
        Dictionary<Type, string> names = [];
        Register<InvalidOtpError>();
        Register<UserNotFoundError>();
        var frozenNames = names.ToFrozenDictionary();

        return new MessagePackSerializer
        {
            UnionTypeAliasProvider = new DelegateUnionTypeAliasProvider(
                (unionType, caseType) => frozenNames.TryGetValue(caseType, out string? name)
                    ? new DerivedTypeIdentifier(name)
                    : (DerivedTypeIdentifier?)null),
        };

        void Register<TError>()
            where TError : IError
        {
            names.Add(typeof(TError), TError.Name);
        }
    }
    #endregion
}
#endif
