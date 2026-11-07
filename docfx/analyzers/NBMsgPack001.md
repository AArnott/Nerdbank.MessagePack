# NBMsgPack001: Apply `[Key]` consistently across members

<xref:Nerdbank.MessagePack.KeyAttribute> should either not be applied to any members or should be applied to all serialized members.

## Data-contract attributes

The analyzer follows PolyType's member-selection rules when attributes from `System.Runtime.Serialization` are applied:

* On a type annotated with `[DataContract]`, only fields and properties annotated with `[DataMember]` are included by default, including non-public members. Unannotated members do not require `[Key]`.
* Without `[DataContract]` on the declaring type, `[IgnoreDataMember]` excludes a member. `[DataMember]` alone does not opt a non-public member into serialization.
* These rules apply separately to each member's declaring type, including base types. Applying `[DataContract]` to a derived type does not change which members of its base type are included. Key usage must still be consistent across all serialized members in the hierarchy.
* `[PropertyShape]` takes precedence over the data-contract attributes: it explicitly includes a member unless `Ignore = true`, which excludes it. Within a `[DataContract]` type, `[DataMember]` includes a member even if it also has `[IgnoreDataMember]`.

Apply `[Key]` consistently to the members that are actually serialized, not to excluded members. `[DataMember(Order = ...)]` does not substitute for `[Key]`.

## Resolution

There are two fixes for this condition:

* Remove the attribute from all members. This will cause the type to serialize as a map of property name=value pairs.
* Consistently apply the attribute to all serialized members. This will cause the type to serialize as an array of values.
