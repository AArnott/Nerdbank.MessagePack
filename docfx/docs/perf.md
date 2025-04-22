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
     bar "Serialize+Deserialize" [323.85,219.42,479.19,1014.76]
     bar "Serialize" [94.21,87.23,99.95,370.93]
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
     y-axis "Time (ns)" 0 --> 300
     title "object as array"
     bar "Serialize+Deserialize" [246.1,182.45]
     bar "Serialize" [83.6,75.09]
 ```
 ```mermaid
xychart-beta
     x-axis "Libraries" ["NB.MessagePack", "MsgPack-CS"]
     y-axis "Allocated (bytes)" 0 --> 100
     title "object as array"
     bar "Serialize+Deserialize" [80,80]
     bar "Serialize" [0,0]
 ```

