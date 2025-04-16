# Performance comparisons

Perf isn't everything, but it can be important in some scenarios.
Nerdbank.MessagePack is very fast, but not quite as fast as MessagePack-CSharp v3 with source generation turned on.

Features and ease of use are also important.
Nerdbank.MessagePack is much simpler to use, and comes [loaded with features](migrating.md#feature-comparison) that MessagePack-CSharp does not have.
Nerdbank.MessagePack also reliably works in AOT environments, while MessagePack-CSharp does not.

In the perf comparisons below, the following legend applies

Library alias | Full name
--- | ---
NB.MessagePack | Nerdbank.MessagePack (this library)
MsgPack-CS | MessagePack-CSharp
Newtonsoft | Newtonsoft.Json
STJ | System.Text.Json

## Object serialization comparisons

Each stacked bar shows the time taken to serialize and deserialize an object.
The two times added together represent round-trip time.

In messagepack, an object may be serialized as a map of property names and values, or as an array of just values.

Some libraries are absent from some comparisons because they don't support a particular format.

```mermaid
xychart-beta
     x-axis "Libraries" ["NB.MessagePack", "MsgPack-CS", "STJ", "Newtonsoft"]
     y-axis "Time (ns)" 0 --> 1100
     title "object as map"
     bar "Serialize+Deserialize" [269.13,222.12,480.03,1030.56]
     bar "Serialize" [88.53,81.61,99.59,368.74]
 ```
 ```mermaid
xychart-beta
     x-axis "Libraries" ["NB.MessagePack", "MsgPack-CS", "STJ", "Newtonsoft"]
     y-axis "Allocated (bytes)" 0 --> 4200
     title "object as map"
     bar "Serialize+Deserialize" [80,80,208,4112]
     bar "Serialize" [0,0,128,1424]
 ```
 ```mermaid
xychart-beta
     x-axis "Libraries" ["NB.MessagePack", "MsgPack-CS"]
     y-axis "Time (ns)" 0 --> 200
     title "object as array"
     bar "Serialize+Deserialize" [190.18,185.39]
     bar "Serialize" [86.48,76.03]
 ```
 ```mermaid
xychart-beta
     x-axis "Libraries" ["NB.MessagePack", "MsgPack-CS"]
     y-axis "Allocated (bytes)" 0 --> 100
     title "object as array"
     bar "Serialize+Deserialize" [80,80]
     bar "Serialize" [0,0]
 ```

