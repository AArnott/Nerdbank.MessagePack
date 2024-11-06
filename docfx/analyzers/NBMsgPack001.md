# NBMsgPack001: Apply `[Key]` consistently across members

@Nerdbank.MessagePack.KeyAttribute should either not be applied to any members or should be applied to all serialized members.

## Resolution

There are two fixes for this condition:

* Remove the attribute from all members. This will cause the type to serialize as a map of property name=value pairs.
* Consistently apply the attribute to all serialized members. This will cause the type to serialize as an array of values.
