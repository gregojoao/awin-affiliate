# Publishing

Release checklist for `Awin.Affiliate`.

## 1. Update version metadata

In `src/Awin.Affiliate/Awin.Affiliate.csproj`, update:

- `<Version>` — semantic version (e.g. `1.1.0`).
- `<PackageReleaseNotes>` — short summary of what changed.

## 2. Run the test suite

```bash
dotnet restore
dotnet test --configuration Release
```

All tests must pass on both `net8.0` and `net10.0`.

## 3. Pack

```bash
dotnet pack --configuration Release
```

This produces:

- `src/Awin.Affiliate/bin/Release/Awin.Affiliate.<version>.nupkg`
- `src/Awin.Affiliate/bin/Release/Awin.Affiliate.<version>.snupkg`

## 4. Smoke-test the package locally (recommended)

Install the local `.nupkg` into a throwaway console project and call
`GenerateAffiliateLinkAsync` once. This catches packaging mistakes (missing
README, missing dependencies, wrong target frameworks) before NuGet does.

## 5. Push to NuGet

```bash
dotnet nuget push src/Awin.Affiliate/bin/Release/Awin.Affiliate.*.nupkg \
  --api-key "$NUGET_API_KEY" \
  --source https://api.nuget.org/v3/index.json
```

Alternatively, run the **Publish NuGet** GitHub Actions workflow from the
Actions tab — it expects the `NUGET_API_KEY` secret to be set on the
repository.

## 6. Tag the release

```bash
git tag v<version>
git push origin v<version>
```

## 7. Verify

After NuGet finishes indexing (usually a few minutes):

```bash
dotnet add package Awin.Affiliate --version <version>
```

Confirm the README renders on the package page and that SourceLink resolves
GitHub commits from the symbols.
