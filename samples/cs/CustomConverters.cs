// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1202 // 'public' members should come before 'internal' members

using System.Reflection;

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
                        property1 = context.GetConverter<SomeOtherType>().Read(ref reader, context);
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

        #region DelegateSubValues
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
    }
}

namespace SubValuesWithWitness
{
    using Nerdbank.MessagePack;

    public record Foo(SomeOtherType? MyProperty1, string? MyProperty2);

    #region WitnessOnFormatter
    // SomeOtherType is outside your assembly and not attributed.
    public partial record SomeOtherType;

    [GenerateShapeFor<SomeOtherType>] // allow FooConverter to provide the shape for SomeOtherType
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
}

namespace WitnessForArray
{
    using Nerdbank.MessagePack;

    public record Foo(SomeOtherType? MyProperty1, string? MyProperty2);

    #region ArrayWitnessOnFormatter
    // SomeOtherType is outside your assembly and not attributed.
    public partial record SomeOtherType;

    [GenerateShapeFor<SomeOtherType[]>]
    partial class FooConverter : MessagePackConverter<Foo?>
    {
        public override Foo? Read(ref MessagePackReader reader, SerializationContext context)
        {
            // ...
            context.GetConverter<SomeOtherType[], FooConverter>().Read(ref reader, context);
            // ...
            #endregion
            throw new NotImplementedException();
        }

        public override void Write(ref MessagePackWriter writer, in Foo? value, SerializationContext context)
        {
            throw new NotImplementedException();
        }
    }
}

namespace CustomConverterTypeRegistration
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

    class CustomConverterRegisteredAtRuntime
    {
        void Main()
        {
            #region CustomConverterRegisteredAtRuntime
            MessagePackSerializer serializer = new();
            serializer = serializer with
            {
                Converters = [
                    .. serializer.Converters,    // preserve existing converters...
                    new MyCustomTypeConverter(), // ... while adding our own.
                ],
            };
            #endregion
        }
    }
}

namespace CustomConverterMemberRegistration
{
    #region CustomConverterByAttributeOnMember
    public class MyCustomType
    {
        [MessagePackConverter(typeof(EncryptingIntegerConverter))]
        public int Value { get; set; }
    }
    #endregion

    /// <summary>
    /// Simple XOR encryption for demonstration.
    /// </summary>
    public class EncryptingIntegerConverter : MessagePackConverter<int>
    {
        public override int Read(ref MessagePackReader reader, SerializationContext context) => reader.ReadInt32() ^ 0x12345678;

        public override void Write(ref MessagePackWriter writer, in int value, SerializationContext context) => writer.Write(value ^ 0x12345678); // Simple XOR encryption for demonstration
    }
}

namespace AsyncConverters
{
    [MessagePackConverter(typeof(MyCustomTypeConverter))]
    public class MyCustomType { }

    public class MyCustomTypeConverter : MessagePackConverter<MyCustomType>
    {
        public override bool PreferAsyncSerialization => true;

        public override MyCustomType? Read(ref MessagePackReader reader, SerializationContext context)
        {
            throw new NotImplementedException();
        }

        public override void Write(ref MessagePackWriter writer, in MyCustomType? value, SerializationContext context)
        {
            throw new NotImplementedException();
        }

        public override async ValueTask<MyCustomType?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
        {
            MessagePackStreamingReader streamingReader = reader.CreateStreamingReader();

            #region GetMoreBytesPattern
            int count;
            while (streamingReader.TryReadArrayHeader(out count).NeedsMoreBytes())
            {
                streamingReader = new(await streamingReader.FetchMoreBytesAsync());
            }
            #endregion

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
    #region MessagePackStringUser
    [MessagePackConverter(typeof(MyCustomTypeConverter))]
    public class MyCustomType
    {
        public string? Message1 { get; set; }

        public string? Message2 { get; set; }
    }

    public class MyCustomTypeConverter : MessagePackConverter<MyCustomType>
    {
        private static readonly MessagePackString Message1 = new(nameof(MyCustomType.Message1));
        private static readonly MessagePackString Message2 = new(nameof(MyCustomType.Message2));

        public override MyCustomType? Read(ref MessagePackReader reader, SerializationContext context)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            string? message1 = null;
            string? message2 = null;

            int count = reader.ReadMapHeader();

            // It is critical that we read or skip every element of the map, even if we don't recognize the key.
            for (int i = 0; i < count; i++)
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

        public override void Write(ref MessagePackWriter writer, in MyCustomType? value, SerializationContext context)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteMapHeader(2);

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
    #region Stateful
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
            Console.WriteLine(serializer.ConvertToJson(msgpack));
            SpecialType deserialized = serializer.Deserialize<SpecialType>(msgpack);
            Console.WriteLine($"Deserialized value: {deserialized}");
        }
    }

    public class StatefulConverter : MessagePackConverter<SpecialType>
    {
        public override SpecialType Read(ref MessagePackReader reader, SerializationContext context)
        {
            int multiplier = (int)context["ValueMultiplier"]!;
            int serializedValue = reader.ReadInt32();
            return new SpecialType(serializedValue / multiplier);
        }

        public override void Write(ref MessagePackWriter writer, in SpecialType value, SerializationContext context)
        {
            int multiplier = (int)context["ValueMultiplier"]!;
            writer.Write(value.Value * multiplier);
        }
    }

    [GenerateShape]
    [MessagePackConverter(typeof(StatefulConverter))]
    public partial record struct SpecialType(int Value);
    #endregion

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

namespace CustomConverterFactory
{
    #region CustomConverterFactory
    [AttributeUsage(AttributeTargets.Interface)]
    class MarshalByRefAttribute : Attribute;

    class MarshalingConverterFactory(object trackerKey) : IMessagePackConverterFactory, ITypeShapeFunc
    {
        public MessagePackConverter? CreateConverter(ITypeShape shape)
        {
            return shape.Type.GetCustomAttribute<MarshalByRefAttribute>() is not null ? this.Invoke(shape) : null;
        }

        object? ITypeShapeFunc.Invoke<T>(ITypeShape<T> typeShape, object? state) => new MarshalingConverter<T>(trackerKey);
    }

    class MarshalingConverter<T>(object trackerKey) : MessagePackConverter<T>
    {
        public override T? Read(ref MessagePackReader reader, SerializationContext context)
        {
            if (reader.TryReadNil())
            {
                return default;
            }

            // This is for demonstration purposes of converter factories only. Do not use.
            Dictionary<int, object?> state = (Dictionary<int, object?>?)context[trackerKey] ?? throw new InvalidOperationException();
            int handle = reader.ReadInt32();
            state.TryGetValue(handle, out var value);
            return (T?)value;
        }

        public override void Write(ref MessagePackWriter writer, in T? value, SerializationContext context)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            // This is for demonstration purposes of converter factories only. Do not use.
            Dictionary<int, object?> state = (Dictionary<int, object?>?)context[trackerKey] ?? throw new InvalidOperationException();
            int handle = state.Count;
            state.Add(handle, value);
            writer.Write(handle);
        }
    }

    class Program
    {
        static void Main()
        {
            MessagePackSerializer serializer = new()
            {
                StartingContext = new SerializationContext
                {
                    ["MarshalingState"] = new Dictionary<int, object?>(),
                },
                ConverterFactories = [new MarshalingConverterFactory("MarshalingState")],
            };

            // Use the serializer to pass object graphs that may include objects that must retain reference identity
            // between parties.
        }
    }
    #endregion
}
