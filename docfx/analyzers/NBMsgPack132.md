# NBMsgPack132: Union discriminator value type is not supported

The constant is a string, boolean, integer primitive, or enum.
Null is not a discriminator value.

## Example violation

```cs
[UnionDiscriminator(1.5)]
public double Kind => 1.5;
```

## Resolution

Use a supported constant.
