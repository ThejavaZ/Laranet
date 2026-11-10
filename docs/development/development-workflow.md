# Development Workflow

This page covers the local commands used to build, test, pack, and try Laranet. For
the contribution process, see [`CONTRIBUTING.md`](../../CONTRIBUTING.md) and the
[contribution guide](contribution-guide.md).

## Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0). The repository
  pins the SDK through [`global.json`](../../global.json).
- Git.
- An IDE or editor with C# support.

Verify the SDK:

```bash
dotnet --version
```

## Restore and build

```bash
dotnet restore Laranet.slnx
dotnet build Laranet.slnx --configuration Release
```

The build treats warnings as errors and generates XML documentation. A missing XML
comment on a public member fails the build.

## Run tests

```bash
dotnet test Laranet.slnx --configuration Release --no-build
```

Run a focused subset:

```bash
dotnet test Laranet.slnx --configuration Release --no-build \
  --filter "FullyQualifiedName~PaginationIntegrationTests"
```

See [Testing guidelines](testing-guidelines.md) for conventions.

## Run the samples

```bash
dotnet run --project samples/Laranet.Samples.Console
dotnet run --project samples/Laranet.Samples.WebApi
```

The samples reference the projects in `src/`, so they exercise the current source
without packing.

## Try the CLI from source

Run the tool project directly:

```bash
dotnet run --project src/Laranet.Cli -- --help
dotnet run --project src/Laranet.Cli -- new demo-api --with-data
```

Install it as a local tool to test the packaged experience:

```bash
dotnet new tool-manifest
dotnet tool install --local Laranet.Cli
dotnet tool run laranet --version
```

## Pack and validate packages

```bash
mkdir -p artifacts/packages
dotnet pack src/Laranet/Laranet.csproj --configuration Release --no-build --output artifacts/packages
dotnet pack src/Laranet.Data/Laranet.Data.csproj --configuration Release --no-build --output artifacts/packages
dotnet pack src/Laranet.EntityFrameworkCore/Laranet.EntityFrameworkCore.csproj --configuration Release --no-build --output artifacts/packages
dotnet pack src/Laranet.AspNetCore/Laranet.AspNetCore.csproj --configuration Release --no-build --output artifacts/packages
dotnet pack src/Laranet.Cli/Laranet.Cli.csproj --configuration Release --no-build --output artifacts/packages

bash .github/scripts/verify-packages.sh artifacts/packages
```

The validation script checks the package count, versions, dependencies, metadata,
tool configuration, and the shared package icon. It is the same script CI uses.

## Inspect a package

```bash
unzip -l artifacts/packages/Laranet.Data.0.1.0.nupkg
unzip -p artifacts/packages/Laranet.Data.0.1.0.nupkg '*.nuspec'
```

Confirm that the icon, README, and expected dependencies are present.

## Documentation checks

- Every relative link under `docs/` must resolve.
- Markdown tables and code fences must be balanced.
- Examples should be valid C# or shell commands.

A link check can be run with any Markdown link checker, or with a short script:

```bash
python3 - <<'PY'
import os, re

root = os.getcwd()
pattern = re.compile(r'\[[^\]]*\]\(([^)]+)\)')
problems = []

for dirpath, _, filenames in os.walk(root):
    if any(part in dirpath for part in ('/.git', '/bin', '/obj', '/artifacts')):
        continue
    for name in filenames:
        if not name.endswith('.md'):
            continue
        path = os.path.join(dirpath, name)
        with open(path, encoding='utf-8') as handle:
            text = handle.read()
        for match in pattern.finditer(text):
            target = match.group(1).strip()
            if target.startswith(('http://', 'https://', 'mailto:', '#')):
                continue
            target = target.split('#', 1)[0]
            if target and not os.path.exists(os.path.normpath(os.path.join(dirpath, target))):
                problems.append((os.path.relpath(path, root), target))

print(f'BROKEN_LINKS={len(problems)}')
for source, target in problems:
    print(f'{source}: {target}')
PY
```

## Common commands

| Task | Command |
| --- | --- |
| Restore | `dotnet restore Laranet.slnx` |
| Build | `dotnet build Laranet.slnx --configuration Release` |
| Test | `dotnet test Laranet.slnx --configuration Release --no-build` |
| Pack | `dotnet pack <project> --configuration Release --output artifacts/packages` |
| Validate packages | `bash .github/scripts/verify-packages.sh artifacts/packages` |
| Run console sample | `dotnet run --project samples/Laranet.Samples.Console` |
| Run Web API sample | `dotnet run --project samples/Laranet.Samples.WebApi` |
| Run CLI from source | `dotnet run --project src/Laranet.Cli -- --help` |

## Troubleshooting

### The build fails on XML documentation

Every public member needs an XML comment. Add the missing `<summary>`, `<param>`,
`<returns>`, or `<exception>` element.

### The build fails on warnings

Warnings are errors. Fix the warning; do not suppress it without understanding why it
occurs and documenting the reason.

### Tests fail against SQLite

Delete the temporary database files under the test output directory and rerun. The
integration tests create isolated databases per test class.

### Package validation fails

Read the error from `verify-packages.sh`. The most common causes are a version mismatch
between projects, a dependency that violates the module boundaries, or a missing
README or icon.

## Related

- [Contribution guide](contribution-guide.md)
- [Testing guidelines](testing-guidelines.md)
- [Release process](release.md)
- [`CONTRIBUTING.md`](../../CONTRIBUTING.md)
