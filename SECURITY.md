# Security Policy

## Supported versions

Laranet is in initial development and no stable release has been published yet. Once
a release is available, security fixes are applied to the latest published version.

| Version | Supported |
| --- | --- |
| Latest `0.x` release | Yes |
| Older versions | No |

## Reporting a vulnerability

Do not open a public issue for a security vulnerability.

Use GitHub's private vulnerability reporting:

1. Open <https://github.com/TheJavaZ/Laranet/security/advisories/new>.
2. Describe the issue, the affected package and version, and the impact.
3. Include a minimal reproduction if possible.

If private reporting is unavailable, contact the maintainer through the repository
profile before disclosing the issue publicly.

## What to include

- Affected package and version.
- .NET target framework and SDK version.
- Integration versions, such as EF Core or ASP.NET Core.
- Steps to reproduce.
- Expected and actual behavior.
- Any relevant logs, without secrets or personal data.

## Scope

In scope:

- Vulnerabilities in Laranet packages.
- Dependency vulnerabilities that affect Laranet usage.
- Insecure generated code or project templates.
- Command injection or path traversal in the CLI.
- Exposure of secrets or sensitive data.

Out of scope:

- Vulnerabilities in .NET, EF Core, ASP.NET Core, or database providers that do not
  involve Laranet code.
- Issues that require a compromised development machine.
- Findings that only affect unsupported versions.

## Disclosure

Please allow time to investigate and prepare a fix before public disclosure. The
maintainer will coordinate the disclosure and credit reporters when requested.

## Related

- [Support policy](docs/compatibility/support-policy.md)
- [Contributing](CONTRIBUTING.md)
