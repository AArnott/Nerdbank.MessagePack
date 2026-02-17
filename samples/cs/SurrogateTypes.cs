// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace OnlyOriginalType
{
    #region OnlyOriginalType
    [GenerateShape]
    public partial class OriginalType
    {
        private int a;
        private int b;

        public OriginalType(int a, int b)
        {
            this.a = a;
            this.b = b;
        }

        public int Sum => this.a + this.b;
    }
    #endregion
}

namespace FocusOnAddedTypes
{
    [GenerateShape]
    [TypeShape(Marshaler = typeof(MyTypeMarshaler))]
    public partial class OriginalType
    {
        private int a;
        private int b;

        public OriginalType(int a, int b)
        {
            this.a = a;
            this.b = b;
        }

        public int Sum => this.a + this.b;

        #region SurrogateType
        internal record struct MarshaledType(int A, int B);
        #endregion

        #region Marshaler
        internal class MyTypeMarshaler : IMarshaler<OriginalType, MarshaledType?>
        {
            public MarshaledType? Marshal(OriginalType? value)
                => value is null ? null : new(value.a, value.b);

            public OriginalType? Unmarshal(MarshaledType? surrogate)
                => surrogate.HasValue ? new(surrogate.Value.A, surrogate.Value.B) : null;
        }
        #endregion
    }
}

partial class CompleteSample
{
    #region CompleteSample
    [GenerateShape]
    [TypeShape(Marshaler = typeof(MyTypeMarshaler))]
    public partial class OriginalType
    {
        private int a;
        private int b;

        public OriginalType(int a, int b)
        {
            this.a = a;
            this.b = b;
        }

        public int Sum => this.a + this.b;

        internal record struct MarshaledType(int A, int B);

        internal class MyTypeMarshaler : IMarshaler<OriginalType, MarshaledType?>
        {
            public MarshaledType? Marshal(OriginalType? value)
                => value is null ? null : new(value.a, value.b);

            public OriginalType? Unmarshal(MarshaledType? surrogate)
                => surrogate.HasValue ? new(surrogate.Value.A, surrogate.Value.B) : null;
        }
    }
    #endregion

    #region OpenGeneric
    [TypeShape(Marshaler = typeof(OpenGenericDataType<>.Marshaler))]
    internal class OpenGenericDataType<T>
    {
        public T? Value { get; set; }

        internal record struct MarshaledType(T? Value);

        internal class Marshaler : IMarshaler<OpenGenericDataType<T>, MarshaledType?>
        {
            public OpenGenericDataType<T>? Unmarshal(MarshaledType? surrogate)
                => surrogate.HasValue ? new() { Value = surrogate.Value.Value } : null;

            public MarshaledType? Marshal(OpenGenericDataType<T>? value)
                => value is null ? null : new(value.Value);
        }
    }
    #endregion

    #region ClosedGenericViaWitness
    [GenerateShapeFor<OpenGenericDataType<int>>]
    internal partial class Witness;

    void SerializeByWitness(OpenGenericDataType<int> value) => Serializer.Serialize<OpenGenericDataType<int>, Witness>(value);

    private static readonly MessagePackSerializer Serializer = new();
    #endregion
}

namespace MixingMarshaledAndUnion
{
    #region MixingMarshaledAndUnion
    [GenerateShape]
    [TypeShape(Marshaler = typeof(MarshaledBaseTypeMarshaler))]
    internal partial class MarshaledBaseType
    {
        private readonly int value;
        private readonly string name;

        public MarshaledBaseType(int value, string name)
        {
            this.value = value;
            this.name = name;
        }

        public int Value => this.value;

        public string Name => this.name;

        [DerivedTypeShape(typeof(MarshaledDerivedType.MarshaledDerivedData), Tag = 1)]
        internal record class MarshaledData(int Value, string Name);

        internal class MarshaledBaseTypeMarshaler : IMarshaler<MarshaledBaseType, MarshaledData?>
        {
            public MarshaledData? Marshal(MarshaledBaseType? value)
                => value switch
                {
                    null => null,
                    MarshaledDerivedType d => MarshaledDerivedType.MarshaledDerivedTypeMarshaler.Instance.Marshal(d),
                    _ => new MarshaledData(value.Value, value.Name),
                };

            public MarshaledBaseType? Unmarshal(MarshaledData? surrogate)
                => surrogate switch
                {
                    null => null,
                    MarshaledDerivedType.MarshaledDerivedData d => MarshaledDerivedType.MarshaledDerivedTypeMarshaler.Instance.Unmarshal(d),
                    _ => new MarshaledBaseType(surrogate.Value, surrogate.Name),
                };
        }
    }

    [TypeShape(Marshaler = typeof(MarshaledDerivedTypeMarshaler))]
    internal partial class MarshaledDerivedType : MarshaledBaseType
    {
        private readonly double extraProperty;

        public MarshaledDerivedType(int value, string name, double extraProperty)
            : base(value, name)
        {
            this.extraProperty = extraProperty;
        }

        public double ExtraProperty => this.extraProperty;

        internal record class MarshaledDerivedData(int Value, string Name, double ExtraProperty) : MarshaledData(Value, Name);

        internal class MarshaledDerivedTypeMarshaler : IMarshaler<MarshaledDerivedType, MarshaledDerivedData?>
        {
            internal static readonly MarshaledDerivedTypeMarshaler Instance = new();

            public MarshaledDerivedData? Marshal(MarshaledDerivedType? value)
                => value is null ? null : new(value.Value, value.Name, value.extraProperty);

            public MarshaledDerivedType? Unmarshal(MarshaledDerivedData? surrogate)
                => surrogate is null ? null : new MarshaledDerivedType(surrogate.Value, surrogate.Name, surrogate.ExtraProperty);
        }
    }
    #endregion
}
