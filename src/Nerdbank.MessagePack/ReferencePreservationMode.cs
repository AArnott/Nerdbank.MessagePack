// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// An enumeration of the modes supported for preserving references in serialized object graphs.
/// </summary>
/// <remarks>
/// When using <see cref="RejectCycles"/> or <see cref="AllowCycles"/>, the following considerations apply:
/// <list type="bullet">
/// <item>
/// Custom converters should be implemented to delegate to sub-converters consistently in <see cref="MessagePackConverter{T}.Read"/>
/// and <see cref="MessagePackConverter{T}.Write"/>.
/// For example, using a converter in <see cref="MessagePackConverter{T}.Read"/> to serialize a particular sub-value
/// but using <see cref="MessagePackWriter"/> directly in your <see cref="MessagePackConverter{T}.Write"/> method to write that same sub-value
/// may confuse the reference tracker, leading to malfunctions that may manifest as false reports about reference cycles or assigning the
/// wrong references to your deserialized object graph.
/// </item>
/// <item>
/// Custom <see cref="MessagePackConverter{T}.Write"/> methods should avoid calling <see cref="MessagePackWriter.WriteNil"/> for a non-null reference value
/// where <see cref="MessagePackConverter{T}.Read"/> methods would deserialize that msgpack Nil token as a non-null value.
/// This is because when the reference tracker sees a MessagePack Nil token, it skips the converter and simply returns a <see langword="null"/> reference.
/// </item>
/// </list>
/// </remarks>
public enum ReferencePreservationMode
{
	/// <summary>
	/// References are not preserved.
	/// Attempts to deserialize a msgpack stream that includes references will throw <see cref="MessagePackSerializationException" />.
	/// </summary>
	/// <remarks>
	/// <para>
	/// If an object appears multiple times in a serialized object graph, it will be serialized anew at each location it is referenced.
	/// This has two outcomes: redundant data leading to larger serialized payloads and the loss of reference equality when deserialized.
	/// </para>
	/// <para>
	/// Serialization will throw <see cref="StackOverflowException" /> (typically crashing the process) if the object graph contains cycles.
	/// </para>
	/// </remarks>
	Off,

	/// <summary>
	/// References are preserved. Cycles are rejected during serialization and deserialization.
	/// </summary>
	/// <remarks>
	/// <para>
	/// When an object appears multiple times in an object graph, it will be serialized as a reference to the first occurrence.
	/// Deserialization will create a new object graph wherein objects are shared across the graph as they were in the original object graph.
	/// Of course there will not be reference equality between the original and deserialized objects, but the deserialized objects will have reference equality with each other.
	/// </para>
	/// <para>
	/// This option utilizes a proprietary msgpack extension and can only be deserialized by libraries that understand this extension.
	/// A reference requires between 3-6 bytes in the msgpack stream instead of whatever the object's by-value representation would have required.
	/// </para>
	/// <para>
	/// There is a small perf penalty for this feature, but depending on the object graph it may turn out to improve performance due to avoiding redundant serializations.
	/// </para>
	/// </remarks>
	RejectCycles,

	/// <summary>
	/// References are preserved. Cycles are allowed.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The remarks in <see cref="RejectCycles"/> apply to this mode as well.
	/// </para>
	/// <para>
	/// This setting adds security implications.
	/// Deserializing untrusted data with this setting enabled may lead to a denial of service attack
	/// by allowing the data source to introduce unexpected reference cycles, leading to an object graph that causes the program to crash
	/// with a <see cref="StackOverflowException"/> later while operating on that object graph.
	/// </para>
	/// <para>
	/// Not all cycles can be deserialized.
	/// Consider the cycle of A -> B -> A.
	/// If A is first to be deserialized, then this cycle can be deserialized if and only if A has a default constructor
	/// and <em>no</em> <see langword="required" /> or <see langword="init" /> properties.
	/// In brief, only the last serialized object in a cycle is allowed to have a non-default constructor or <see langword="required"/> or <see langword="init" />properties
	/// due to limitations in the C# language and the way PolyType works.
	/// Since the order of serialized objects in a cycle is difficult to control in practice,
	/// any types that may be involved in cycles should avoid non-default constructors and <see langword="required"/> or <see langword="init" />properties.
	/// </para>
	/// </remarks>
	AllowCycles,
}
