# Security Policy

## Supported Versions

Servicing will typically be applied to the latest stable x.y version as well as the latest prerelease version (where applicable).

For example if v1.3 is the latest stable version, it will receive security fixes and v1.2 will not.
A v1.4-beta or similar prerelease would also receive the security fixes.
To illustrate:

| Version               | Supported          |
| --------------------- | ------------------ |
| Older stable releases | :x:                |
| Latest stable release | :white_check_mark: |
| Latest prerelease     | :white_check_mark: |

Servicing of older stable versions is available by request with a support agreement or one-time payment.

If and when major breaking changes are made, leading to a major version increment, this policy *may* be updated to indicate which prior major versions are still supported with security fixes.
Until such updates, the plan is to support -1 major release version. e.g. when v3 is released, we'll support v2 but drop support for v1.

## Reporting a Vulnerability

Please [the security tab on this repo](https://github.com/AArnott/Nerdbank.MessagePack/security) to privately and securely report vulnerabilities.
This gives us a chance to review the report, author a fix and publish a patched version before the vulnerability is made public.
