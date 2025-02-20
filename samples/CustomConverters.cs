// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace CustomConverter
{
    #region YourOwnConverter
    public record Foo(int MyProperty1, string? MyProperty2);

    class FooConverter : Converter<Foo?>
    {
        public override Foo? Read(ref Reader reader, SerializationContext context)
        {
            if (reader.TryReadNull())
            {
                return null;
            }

            context.DepthStep();
            int property1 = 0;
            string? property2 = null;

            int? count = reader.ReadStartMap();
            bool isFirstElement = true;
            for (int i = 0; i < count || (count is null && reader.TryAdvanceToNextElement(ref isFirstElement)); i++)
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

        public override void Write(ref Writer writer, in Foo? value, SerializationContext context)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }

            context.DepthStep();
            writer.WriteStartMap(2);

            writer.Write("MyProperty");
            writer.Write(value.MyProperty1);

            writer.Write("MyProperty2");
            writer.Write(value.MyProperty2);
        }
    }
    #endregion

    class VersionSafeConverter : Converter<Foo>
    {
        public override Foo? Read(ref Reader reader, SerializationContext context)
        {
            if (reader.TryReadNull())
            {
                return null;
            }

            #region ReadWholeArray
            context.DepthStep();
            int property1 = 0;
            string? property2 = null;
            int? count = reader.ReadStartVector();
            bool isFirstElement = true;
            for (int i = 0; i < count || (count is null && reader.TryAdvanceToNextElement(ref isFirstElement)); i++)
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

        public override void Write(ref Writer writer, in Foo? value, SerializationContext context)
        {
            throw new NotImplementedException();
        }
    }
}

namespace SubValues
{
    public record Foo(SomeOtherType? MyProperty1, string? MyProperty2);

    [GenerateShape]
    public partial record SomeOtherType;

    class FooConverter : Converter<Foo?>
    {
        public override Foo? Read(ref Reader reader, SerializationContext context)
        {
            if (reader.TryReadNull())
            {
                return null;
            }

            context.DepthStep();
            SomeOtherType? property1 = null;
            string? property2 = null;

            int? count = reader.ReadStartMap();
            bool isFirstElement = true;
            for (int i = 0; i < count || (count is null && reader.TryAdvanceToNextElement(ref isFirstElement)); i++)
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
        public override void Write(ref Writer writer, in Foo? value, SerializationContext context)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }

            context.DepthStep();
            writer.WriteStartMap(2);

            writer.Write("MyProperty");
            SomeOtherType? propertyValue = value.MyProperty1;
            context.GetConverter<SomeOtherType>().Write(ref writer, propertyValue, context);
            writer.Write("MyProperty2");
            writer.Write(value.MyProperty2);
        }

        #endregion
#else
        #region DelegateSubValuesNETFX
        public override void Write(ref Writer writer, in Foo? value, SerializationContext context)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }

            context.DepthStep();
            writer.WriteStartMap(2);

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
    public record Foo(SomeOtherType? MyProperty1, string? MyProperty2);

#if NET
    #region WitnessOnFormatterNET
    // SomeOtherType is outside your assembly and not attributed.
    public partial record SomeOtherType;

    [GenerateShape<SomeOtherType>] // allow FooConverter to provide the shape for SomeOtherType
    partial class FooConverter : Converter<Foo?>
    {
        public override Foo? Read(ref Reader reader, SerializationContext context)
        {
            // ...
            context.GetConverter<SomeOtherType, FooConverter>().Read(ref reader, context);
            // ...
            #endregion

            throw new NotImplementedException();
        }

        public override void Write(ref Writer writer, in Foo? value, SerializationContext context)
        {
            throw new NotImplementedException();
        }
    }
#else
    #region WitnessOnFormatterNETFX
    // SomeOtherType is outside your assembly and not attributed.
    public partial record SomeOtherType;

    [GenerateShape<SomeOtherType>] // allow FooConverter to provide the shape for SomeOtherType
    partial class FooConverter : Converter<Foo?>
    {
        public override Foo? Read(ref Reader reader, SerializationContext context)
        {
            // ...
            context.GetConverter<SomeOtherType>(ShapeProvider).Read(ref reader, context);
            // ...
            #endregion

            throw new NotImplementedException();
        }

        public override void Write(ref Writer writer, in Foo? value, SerializationContext context)
        {
            throw new NotImplementedException();
        }
    }
#endif
}

namespace WitnessForArray
{
    public record Foo(SomeOtherType? MyProperty1, string? MyProperty2);

#if NET
    #region ArrayWitnessOnFormatterNET
    // SomeOtherType is outside your assembly and not attributed.
    public partial record SomeOtherType;

    [GenerateShape<SomeOtherType[]>]
    partial class FooConverter : Converter<Foo?>
    {
        public override Foo? Read(ref Reader reader, SerializationContext context)
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
    partial class FooConverter : Converter<Foo?>
    {
        public override Foo? Read(ref Reader reader, SerializationContext context)
        {
            // ...
            context.GetConverter<SomeOtherType[]>(ShapeProvider).Read(ref reader, context);
            // ...
            #endregion
#endif
            throw new NotImplementedException();
        }

        public override void Write(ref Writer writer, in Foo? value, SerializationContext context)
        {
            throw new NotImplementedException();
        }
    }
}

namespace CustomConverterRegistration
{
    #region CustomConverterByAttribute
    [Converter(typeof(MyCustomTypeConverter))]
    public class MyCustomType { }
    #endregion

    public class MyCustomTypeConverter : Converter<MyCustomType>
    {
        public override MyCustomType? Read(ref Reader reader, SerializationContext context)
        {
            throw new NotImplementedException();
        }

        public override void Write(ref Writer writer, in MyCustomType? value, SerializationContext context)
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
    [Converter(typeof(MyCustomTypeConverter))]
    public class MyCustomType { }

    public class MyCustomTypeConverter : Converter<MyCustomType>
    {
        public override bool PreferAsyncSerialization => true;

        public override MyCustomType? Read(ref Reader reader, SerializationContext context)
        {
            throw new NotImplementedException();
        }

        public override void Write(ref Writer writer, in MyCustomType? value, SerializationContext context)
        {
            throw new NotImplementedException();
        }

        [Experimental("NBMsgPack")]
        public override async ValueTask<MyCustomType?> ReadAsync(AsyncReader reader, SerializationContext context)
        {
            StreamingReader streamingReader = reader.CreateStreamingReader();

            #region GetMoreBytesPattern
            int? count;
            while (streamingReader.TryReadArrayHeader(out count).NeedsMoreBytes())
            {
                streamingReader = new(await streamingReader.FetchMoreBytesAsync());
            }
            #endregion

            if (count is null)
            {
                throw new NotImplementedException(); // TODO: implement this.
            }

            for (int i = 0; i < count; i++)
            {
                while (streamingReader.TrySkip(ref context).NeedsMoreBytes())
                {
                    streamingReader = new(await streamingReader.FetchMoreBytesAsync());
                }
            }

            reader.ReturnReader(ref streamingReader);

            return new MyCustomType();
        }
    }
}

namespace PerformanceConverters
{
    #region PreformattedStringUser
    [Converter(typeof(MyCustomTypeConverter))]
    public class MyCustomType
    {
        public string? Message1 { get; set; }

        public string? Message2 { get; set; }
    }

    public class MyCustomTypeConverter : Converter<MyCustomType>
    {
        private static readonly PreformattedString Message1 = new(nameof(MyCustomType.Message1), MessagePackFormatter.Default);
        private static readonly PreformattedString Message2 = new(nameof(MyCustomType.Message2), MessagePackFormatter.Default);

        public override MyCustomType? Read(ref Reader reader, SerializationContext context)
        {
            if (reader.TryReadNull())
            {
                return null;
            }

            string? message1 = null;
            string? message2 = null;

            int? count = reader.ReadStartMap();

            // It is critical that we read or skip every element of the map, even if we don't recognize the key.
            bool isFirstElement = true;
            for (int i = 0; i < count || (count is null && reader.TryAdvanceToNextElement(ref isFirstElement)); i++)
            {
                // Compare the key to those we recognize such that we don't decode or allocate strings unnecessarily.
                if (Message1.TryRead(ref reader))
                {
                    message1 = reader.ReadString();
                }
                else if (Message2.TryRead(ref reader))
                {
                    message2 = reader.ReadString();
                }
                else
                {
                    // We don't recognize the key, so skip both the key and the value.
                    reader.Skip(context);
                    reader.Skip(context);
                }
            }

            return new MyCustomType
            {
                Message1 = message1,
                Message2 = message2,
            };
        }

        public override void Write(ref Writer writer, in MyCustomType? value, SerializationContext context)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartMap(2);

            // Write the pre-encoded msgpack for the property names to avoid repeatedly paying encoding costs.
            writer.Write(Message1);
            writer.Write(value.Message1);

            writer.Write(Message2);
            writer.Write(value.Message2);
        }
    }
    #endregion
}

namespace Stateful
{
#if NET
    #region StatefulNET
    class Program
    {
        static void Main()
        {
            MessagePackSerializer serializer = new()
            {
                StartingContext = new SerializationContext
                {
                    ["ValueMultiplier"] = 3,
                },
            };
            SpecialType original = new(5);
            Console.WriteLine($"Original value: {original}");
            byte[] msgpack = serializer.Serialize(original);
            Console.WriteLine(new JsonExporter(serializer).ConvertToJson(msgpack));
            SpecialType deserialized = serializer.Deserialize<SpecialType>(msgpack);
            Console.WriteLine($"Deserialized value: {deserialized}");
        }
    }

    class StatefulConverter : Converter<SpecialType>
    {
        public override SpecialType Read(ref Reader reader, SerializationContext context)
        {
            int multiplier = (int)context["ValueMultiplier"]!;
            int serializedValue = reader.ReadInt32();
            return new SpecialType(serializedValue / multiplier);
        }

        public override void Write(ref Writer writer, in SpecialType value, SerializationContext context)
        {
            int multiplier = (int)context["ValueMultiplier"]!;
            writer.Write(value.Value * multiplier);
        }
    }

    [GenerateShape]
    [Converter(typeof(StatefulConverter))]
    partial record struct SpecialType(int Value);
    #endregion
#else
    #region StatefulNETFX
    class Program
    {
        static void Main()
        {
            MessagePackSerializer serializer = new()
            {
                StartingContext = new SerializationContext
                {
                    ["ValueMultiplier"] = 3,
                },
            };
            SpecialType original = new(5);
            Console.WriteLine($"Original value: {original}");
            byte[] msgpack = serializer.Serialize(original, Witness.ShapeProvider);
            Console.WriteLine(new JsonExporter(serializer).ConvertToJson(msgpack));
            SpecialType deserialized = serializer.Deserialize<SpecialType>(msgpack, Witness.ShapeProvider);
            Console.WriteLine($"Deserialized value: {deserialized}");
        }
    }

    class StatefulConverter : Converter<SpecialType>
    {
        public override SpecialType Read(ref Reader reader, SerializationContext context)
        {
            int multiplier = (int)context["ValueMultiplier"]!;
            int serializedValue = reader.ReadInt32();
            return new SpecialType(serializedValue / multiplier);
        }

        public override void Write(ref Writer writer, in SpecialType value, SerializationContext context)
        {
            int multiplier = (int)context["ValueMultiplier"]!;
            writer.Write(value.Value * multiplier);
        }
    }

    [Converter(typeof(StatefulConverter))]
    partial record struct SpecialType(int Value);

    [GenerateShape<SpecialType>]
    partial class Witness;
    #endregion
#endif

    class ChangeExistingState
    {
        MessagePackSerializer ModifySerializer(MessagePackSerializer serializer)
        {
            #region ModifyStateOnSerializer
            SerializationContext context = serializer.StartingContext;
            context["ValueMultiplier"] = 5;
            serializer = serializer with
            {
                StartingContext = context,
            };
            #endregion
            return serializer;
        }
    }
}
