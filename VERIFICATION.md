# Verification

## Functional evidence

The initial local .NET 10 Release run passed 34 tests, with no failed or skipped tests and no build
warnings. The executable example also passed. The tests cover empty/isolated graphs, self-loops,
duplicate edges, comparers and collisions, invalid/null inputs, input ownership, read-only results,
FIFO topological ordering, recursive-equivalent DFS preorder, concurrency and size validation.

Reference coverage includes 360 fixed-seed random graphs with all-start reachability and BFS-distance
checks against Floyd–Warshall, SCC equivalence from mutual reachability, recursive DFS preorder,
cycle detection, and all 1,024 forward-edge subsets on five vertices. A 50,000-vertex chain and cycle
exercise iterative algorithms beyond ordinary recursion depth. Tests use no timing thresholds.

## Artifact evidence

`tools/Verify.ps1` emits a unique ignored artifact directory and `release-manifest.json` for each
executed run. It records actual test counts, commit and package hashes. The archive audit verifies
the exact inventory, nuspec metadata, absence of runtime dependencies, original license/icon bytes,
XML documentation, matching portable PDB identities and source checksums. The package consumer uses
only PackageReference, an explicit feed, a fresh temporary directory and a separate empty cache.

The API inventory is reviewed in `docs/PublicApi.txt`. CI is configured to execute the same script
on Windows and Ubuntu; configuration alone is not evidence that a particular CI run passed.

The common unchanged icon's shared visual inspection covers 32/128-pixel display on light and dark
backgrounds. Archive checks establish byte identity. The actual NuGet gallery rendering still needs
to be inspected after indexing for each release.

Publication, public-feed consumption and public symbol retrieval are separate release checks.
A local successful verification does not claim publication. Their final status belongs in the exact
release manifest and linked CI/release evidence.
