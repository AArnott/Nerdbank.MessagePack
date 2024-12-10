// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace CustomConverter
{
    #region YourOwnConverter
    using Nerdbank.MessagePack;

    public record Foo(int MyProperty1, string? MyProperty2);

    class FooConverter : MessagePackConverter<Foo?>
    {
        public override Foo? Read(ref MessagePackReader reader, SerializationContext context)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            context.DepthStep();
            int property1 = 0;
            string? property2 = null;

            int count = reader.ReadMapHeader();
            for (int i = 0; i < count; i++)
            {
                string? key = reader.ReadString();
                switch (key)
                {
                    case "MyProperty":
                        property1 = reader.ReadInt32();
                        break;
                    case "MyProperty2":
                        property2 = reader.ReadString();
                        break;
                    default:
                        // Skip the value, as we don't know where to put it.
                        reader.Skip(context);
                        break;
                }
            }

            return new Foo(property1, property2);
        }

        public override void Write(ref MessagePackWriter writer, in Foo? value, SerializationContext context)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            context.DepthStep();
            writer.WriteMapHeader(2);

            writer.Write("MyProperty");
            writer.Write(value.MyProperty1);

            writer.Write("MyProperty2");
            writer.Write(value.MyProperty2);
        }
    }
    #endregion

    class VersionSafeConverter : MessagePackConverter<Foo>
    {
        public override Foo? Read(ref MessagePackReader reader, SerializationContext context)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            #region ReadWholeArray
            context.DepthStep();
            int property1 = 0;
            string? property2 = null;
            int count = reader.ReadArrayHeader();
            for (int i = 0; i < count; i++)
            {
                switch (i)
                {
                    case 0:
                        property1 = reader.ReadInt32();
                        break;
                    case 1:
                        property2 = reader.ReadString();
                        break;
                    default:
                        // Skip the value, as we don't know where to put it.
                        reader.Skip(context);
                        break;
                }
            }

            return new Foo(property1, property2);
            #endregion
        }

        public override void Write(ref MessagePackWriter writer, in Foo? value, SerializationContext context)
        {
            throw new NotImplementedException();
        }
    }
}

namespace SubValues
{
    using Nerdbank.MessagePack;

    public record Foo(SomeOtherType? MyProperty1, string? MyProperty2);

    [GenerateShape]
    public partial record SomeOtherType;

    class FooConverter : MessagePackConverter<Foo?>
    {
        public override Foo? Read(ref MessagePackReader reader, SerializationContext context)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            context.DepthStep();
            SomeOtherType? property1 = null;
            string? property2 = null;

            int count = reader.ReadMapHeader();
            for (int i = 0; i < count; i++)
            {
                string? key = reader.ReadString();
                switch (key)
                {
                    case "MyProperty":
#if NET
                        property1 = context.GetConverter<SomeOtherType>().Read(ref reader, context);
#else
                        property1 = context.GetConverter<SomeOtherType>(context.TypeShapeProvider).Read(ref reader, context);
#endif
                        break;
                    case "MyProperty2":
                        property2 = reader.ReadString();
                        break;
                    default:
                        // Skip the value, as we don't know where to put it.
                        reader.Skip(context);
                        break;
                }
            }

            return new Foo(property1, property2);
        }

#if NET
        #region DelegateSubValuesNET
        public override void Write(ref MessagePackWriter writer, in Foo? value, SerializationContext context)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            context.DepthStep();
            writer.WriteMapHeader(2);

            writer.Write("MyProperty");
            SomeOtherType? propertyValue = value.MyProperty1;
            context.GetConverter<SomeOtherType>().Write(ref writer, propertyValue, context);
            writer.Write("MyProperty2");
            writer.Write(value.MyProperty2);
        }

        #endregion
#else
        #region DelegateSubValuesNETFX
        public override void Write(ref MessagePackWriter writer, in Foo? value, SerializationContext context)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            context.DepthStep();
            writer.WriteMapHeader(2);

            writer.Write("MyProperty");
            SomeOtherType? propertyValue = value.MyProperty1;
            context.GetConverter<SomeOtherType>(context.TypeShapeProvider).Write(ref writer, propertyValue, context);
            writer.Write("MyProperty2");
            writer.Write(value.MyProperty2);
        }
        #endregion
#endif
    }
}

namespace SubValuesWithWitness
{
    using Nerdbank.MessagePack;

    public record Foo(SomeOtherType? MyProperty1, string? MyProperty2);

#if NET
    #region WitnessOnFormatterNET
    // SomeOtherType is outside your assembly and not attributed.
    public partial record SomeOtherType;

    [GenerateShape<SomeOtherType>] // allow FooConverter to provide the shape for SomeOtherType
    partial class FooConverter : MessagePackConverter<Foo?>
    {
        public override Foo? Read(ref MessagePackReader reader, SerializationContext context)
        {
            // ...
            context.GetConverter<SomeOtherType, FooConverter>().Read(ref reader, context);
            // ...
            #endregion

            throw new NotImplementedException();
        }

        public override void Write(ref MessagePackWriter writer, in Foo? value, SerializationContext context)
        {
            throw new NotImplementedException();
        }
    }
#else
    #region WitnessOnFormatterNETFX
    // SomeOtherType is outside your assembly and not attributed.
    public partial record SomeOtherType;

    [GenerateShape<SomeOtherType>] // allow FooConverter to provide the shape for SomeOtherType
    partial class FooConverter : MessagePackConverter<Foo?>
    {
        public override Foo? Read(ref MessagePackReader reader, SerializationContext context)
        {
            // ...
            context.GetConverter<SomeOtherType>(ShapeProvider).Read(ref reader, context);
            // ...
            #endregion

            throw new NotImplementedException();
        }

        public override void Write(ref MessagePackWriter writer, in Foo? value, SerializationContext context)
        {
            throw new NotImplementedException();
        }
    }
#endif
}

namespace WitnessForArray
{
    using Nerdbank.MessagePack;

    public record Foo(SomeOtherType? MyProperty1, string? MyProperty2);

#if NET
    #region ArrayWitnessOnFormatterNET
    // SomeOtherType is outside your assembly and not attributed.
    public partial record SomeOtherType;

    [GenerateShape<SomeOtherType[]>]
    partial class FooConverter : MessagePackConverter<Foo?>
    {
        public override Foo? Read(ref MessagePackReader reader, SerializationContext context)
        {
            // ...
            context.GetConverter<SomeOtherType[], FooConverter>().Read(ref reader, context);
            // ...
            #endregion
#else
    #region ArrayWitnessOnFormatterNETFX
    // SomeOtherType is outside your assembly and not attributed.
    public partial record SomeOtherType;

    [GenerateShape<SomeOtherType[]>]
    partial class FooConverter : MessagePackConverter<Foo?>
    {
        public override Foo? Read(ref MessagePackReader reader, SerializationContext context)
        {
            // ...
            context.GetConverter<SomeOtherType[]>(ShapeProvider).Read(ref reader, context);
            // ...
            #endregion
#endif
            throw new NotImplementedException();
        }

        public override void Write(ref MessagePackWriter writer, in Foo? value, SerializationContext context)
        {
            throw new NotImplementedException();
        }
    }
}

namespace CustomConverterRegistration
{
    #region CustomConverterByAttribute
    [MessagePackConverter(typeof(MyCustomTypeConverter))]
    public class MyCustomType { }
    #endregion

    public class MyCustomTypeConverter : MessagePackConverter<MyCustomType>
    {
        public override MyCustomType? Read(ref MessagePackReader reader, SerializationContext context)
        {
            throw new NotImplementedException();
        }

        public override void Write(ref MessagePackWriter writer, in MyCustomType? value, SerializationContext context)
        {
            throw new NotImplementedException();
        }
    }

    class CustomConverterByRegister
    {
        void Main()
        {
            #region CustomConverterByRegister
            MessagePackSerializer serializer = new();
            serializer.RegisterConverter(new MyCustomTypeConverter());
            #endregion
        }
    }
}

namespace AsyncConverters
{
    [MessagePackConverter(typeof(MyCustomTypeConverter))]
    public class MyCustomType { }

    public class MyCustomTypeConverter : MessagePackConverter<MyCustomType>
    {
        public override MyCustomType? Read(ref MessagePackReader reader, SerializationContext context)
        {
            throw new NotImplementedException();
        }

        public override void Write(ref MessagePackWriter writer, in MyCustomType? value, SerializationContext context)
        {
            throw new NotImplementedException();
        }

        [Experimental("NBMsgPack")]
        public override async ValueTask<MyCustomType?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
        {
            MessagePackStreamingReader streamingReader = reader.CreateStreamingReader();

            #region GetMoreBytesPattern
            int count;
            while (streamingReader.TryReadArrayHeader(out count).NeedsMoreBytes())
            {
                streamingReader = new(await streamingReader.ReadMoreBytes());
            }
            #endregion

            for (int i = 0; i < count; i++)
            {
                while (streamingReader.TrySkip(context).NeedsMoreBytes())
                {
                    streamingReader = new(await streamingReader.ReadMoreBytes());
                }
            }

            reader.ReturnReader(ref streamingReader);

            return new MyCustomType();
        }
    }
}
