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
    [TypeShape(Marshaller = typeof(MyTypeMarshaller))]
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
        internal class MyTypeMarshaller : IMarshaller<OriginalType, MarshaledType?>
        {
            public MarshaledType? ToSurrogate(OriginalType? value)
                => value is null ? null : new(value.a, value.b);

            public OriginalType? FromSurrogate(MarshaledType? surrogate)
                => surrogate.HasValue ? new(surrogate.Value.A, surrogate.Value.B) : null;
        }
        #endregion
    }
}

partial class CompleteSample
{
    #region CompleteSample
    [GenerateShape]
    [TypeShape(Marshaller = typeof(MyTypeMarshaller))]
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

        internal class MyTypeMarshaller : IMarshaller<OriginalType, MarshaledType?>
        {
            public MarshaledType? ToSurrogate(OriginalType? value)
                => value is null ? null : new(value.a, value.b);

            public OriginalType? FromSurrogate(MarshaledType? surrogate)
                => surrogate.HasValue ? new(surrogate.Value.A, surrogate.Value.B) : null;
        }
    }
    #endregion

    #region OpenGeneric
    [TypeShape(Marshaller = typeof(OpenGenericDataType<>.Marshaller))]
    internal class OpenGenericDataType<T>
    {
        public T? Value { get; set; }

        internal record struct MarshaledType(T? Value);

        internal class Marshaller : IMarshaller<OpenGenericDataType<T>, MarshaledType?>
        {
            public OpenGenericDataType<T>? FromSurrogate(MarshaledType? surrogate)
                => surrogate.HasValue ? new() { Value = surrogate.Value.Value } : null;

            public MarshaledType? ToSurrogate(OpenGenericDataType<T>? value)
                => value is null ? null : new(value.Value);
        }
    }
    #endregion

#if NET
    #region ClosedGenericViaWitnessNET
    [GenerateShapeFor<OpenGenericDataType<int>>]
    internal partial class Witness;

    void SerializeByWitness(OpenGenericDataType<int> value) => Serializer.Serialize<OpenGenericDataType<int>, Witness>(value);

    private static readonly MessagePackSerializer Serializer = new();
    #endregion
#else
    #region ClosedGenericViaWitnessNETFX
    [GenerateShapeFor<OpenGenericDataType<int>>]
    internal partial class Witness;

    void SerializeByWitness(OpenGenericDataType<int> value) => Serializer.Serialize(value, Witness.ShapeProvider);

    private static readonly MessagePackSerializer Serializer = new();
    #endregion
#endif
}
