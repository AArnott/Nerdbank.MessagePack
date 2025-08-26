In the perf comparisons below, the following legend applies

Library alias | Full name
--- | ---
NB.MessagePack | Nerdbank.MessagePack (this library)
MsgPack-CS | MessagePack-CSharp
Newtonsoft | Newtonsoft.Json
STJ | System.Text.Json

### Object serialization comparisons

Each stacked bar shows the time taken to serialize and deserialize an object.
The two times added together represent round-trip time.

In messagepack, an object may be serialized as a map of property names and values, or as an array of just values.

Some libraries are absent from some comparisons because they don't support a particular format.

```mermaid
xychart-beta
     x-axis "Libraries" ["NB.MessagePack", "MsgPack-CS", "STJ", "Newtonsoft"]
     y-axis "Time (ns)" 0 --> 1100
     title "object as map"
     bar "Serialize+Deserialize" [288.68,235.88,564.09,1072.37]
     bar "Serialize" [91.84,86.31,107.03,405.12]
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
     bar "Serialize+Deserialize" [222.75,200.18]
     bar "Serialize" [103.15,79.75]
 ```
 ```mermaid
xychart-beta
     x-axis "Libraries" ["NB.MessagePack", "MsgPack-CS"]
     y-axis "Allocated (bytes)" 0 --> 100
     title "object as array"
     bar "Serialize+Deserialize" [80,80]
     bar "Serialize" [0,0]
 ```

