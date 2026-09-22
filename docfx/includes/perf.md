In the perf comparisons below, the following legend applies

Library alias | Full name
--- | ---
NB.MessagePack | Nerdbank.MessagePack (this library)
MsgPack-CS | MessagePack-CSharp
Newtonsoft | Newtonsoft.Json
STJ | System.Text.Json

These measurements were produced by the checked-in `SimplePoco` BenchmarkDotNet suite on .NET 10.

### Object serialization comparisons

Each stacked bar shows the time taken to serialize and deserialize an object.
The two times added together represent round-trip time.

In messagepack, an object may be serialized as a map of property names and values, or as an array of just values.

Some libraries are absent from some comparisons because they don't support a particular format.

```mermaid
xychart-beta
     x-axis "Libraries" ["NB.MessagePack", "MsgPack-CS", "STJ", "Newtonsoft"]
     y-axis "Time (ns)" 0 --> 600
     title "object as map"
     bar "Serialize+Deserialize" [156.08,148.36,281.5,542.6]
     bar "Serialize" [57.76,51.74,58.27,207.64]
 ```
 ```mermaid
xychart-beta
     x-axis "Libraries" ["NB.MessagePack", "MsgPack-CS", "STJ", "Newtonsoft"]
     y-axis "Allocated (bytes)" 0 --> 4200
     title "object as map"
     bar "Serialize+Deserialize" [80,80,216,4120]
     bar "Serialize" [0,0,136,1432]
 ```
 ```mermaid
xychart-beta
     x-axis "Libraries" ["NB.MessagePack", "MsgPack-CS"]
     y-axis "Time (ns)" 0 --> 200
     title "object as array"
     bar "Serialize+Deserialize" [101.37,120.51]
     bar "Serialize" [38.75,47.01]
 ```
 ```mermaid
xychart-beta
     x-axis "Libraries" ["NB.MessagePack", "MsgPack-CS"]
     y-axis "Allocated (bytes)" 0 --> 100
     title "object as array"
     bar "Serialize+Deserialize" [80,80]
     bar "Serialize" [0,0]
 ```

