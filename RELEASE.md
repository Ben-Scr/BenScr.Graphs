# Release procedure

1. Work from a clean commit on the existing branch with the pinned SDK and locked dependencies.
2. Run `./tools/Verify.ps1`; inspect the resulting manifest, test report, package audit and fresh
   consumer evidence. Require complete passing Windows and Ubuntu CI for the same commit.
3. Use the verified CI artifact unchanged. Check its SHA-256 and source commit before tagging
   `packages/BenScr.Graphs/1.0.0`. Verify that the remote tag resolves to that exact commit.
4. Publish the explicit `BenScr.Graphs.1.0.0.nupkg` and matching `.snupkg`, then wait for validation
   and indexing. Do not silently skip a conflicting published version or repack the same version.
5. Run the public consumer with a new temporary cache:

   ```powershell
   ./tools/TestPackage.ps1 -Source https://api.nuget.org/v3/index.json `
       -ExpectedCommit <release-commit> -EvidencePath artifacts/public-consumer.json
   ```

6. Compare installed package payloads with the verified archive, accounting for NuGet repository
   signing. Verify signatures, matching public symbol downloads, live Source Link and the gallery icon.
7. Publish the GitHub release with the exact package, symbols, manifest and audit evidence. Record
   actual NuGet/CI/release URLs and distinguish pending checks from completed publication.

No publication credentials are stored in this repository. The CI workflow verifies and uploads
artifacts; it does not imply that a NuGet publishing identity or policy has been configured.
