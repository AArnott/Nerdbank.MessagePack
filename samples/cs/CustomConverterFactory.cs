// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CustomConverterFactory
{
    #region NonGeneric
    internal class NonGenericCustomConverterFactory : IMessagePackConverterFactory
    {
        // special purpose factory knows exactly what it supports. No generic type parameter needed.
        public MessagePackConverter? CreateConverter(Type type, ITypeShape? shape, in ConverterContext context)
            => type == typeof(List<Guid>) ? new WrapInArrayConverter<List<Guid>>() : null;
    }
    #endregion

    #region Generic
    internal class GenericCustomConverterFactory : IMessagePackConverterFactory, ITypeShapeFunc
    {
        // perform type check, then defer to generic method.
        public MessagePackConverter? CreateConverter(Type type, ITypeShape? shape, in ConverterContext context)
            => shape?.Type == typeof(int) ? this.Invoke(shape) : null;

        // type check is already done, just create the converter.
        object? ITypeShapeFunc.Invoke<T>(ITypeShape<T> typeShape, object? state)
            => new WrapInArrayConverter<T>();
    }
    #endregion

    #region Visitor
    internal class VisitorCustomConverterFactory : IMessagePackConverterFactory
    {
        // perform type check, then defer to generic method.
        public MessagePackConverter? CreateConverter(Type type, ITypeShape? shape, in ConverterContext context)
            => shape is IEnumerableTypeShape enumShape && enumShape.Type.GetGenericTypeDefinition() == typeof(List<>) ?
                (MessagePackConverter?)shape.Accept(Visitor.Instance) : null;

        private class Visitor : TypeShapeVisitor
        {
            internal static readonly Visitor Instance = new();

            public override object? VisitEnumerable<TEnumerable, TElement>(IEnumerableTypeShape<TEnumerable, TElement> enumerableShape, object? state = null)
                => new CustomListConverter<TElement>();
        }
    }
    #endregion

    class WrapInArrayConverter<T> : MessagePackConverter<T>
    {
        public override T? Read(ref MessagePackReader reader, SerializationContext context)
        {
            throw new NotImplementedException();
        }

        public override void Write(ref MessagePackWriter writer, in T? value, SerializationContext context)
        {
            throw new NotImplementedException();
        }
    }

    class CustomListConverter<T> : MessagePackConverter<List<T>>
    {
        public override List<T>? Read(ref MessagePackReader reader, SerializationContext context)
        {
            throw new NotImplementedException();
        }

        public override void Write(ref MessagePackWriter writer, in List<T>? value, SerializationContext context)
        {
            throw new NotImplementedException();
        }
    }
}
