# Provenance

Created by Ben Schneider — https://github.com/Ben-Scr

The graph implementation is new code using .NET collections. Breadth-first search, depth-first
search, Kahn topological sorting and Kosaraju's two-pass component algorithm are implemented here;
no third-party algorithm source or private application source is included. Test references use
Floyd–Warshall transitive closure and distances, a small recursive DFS, and independent edge-order
checks. Runtime dependencies are limited to the .NET 10 platform.

Build configuration and package-verification tools adapt BenScr.Caching at commit
`6abc8b9873032f4675af41b78a541dcbf9cb6234` under the same author license. Build/test packages retain
their own licenses and are not propagated as runtime dependencies.

The original custom `LICENSE.txt` is copied byte-for-byte from that repository, including CRLF bytes.
Its SHA-256 is `f6600e285e17c48188a65745d7595807e1755c72201e8eef201a9174fca18573`.
This license is not represented as the MIT license.

The common icon is copied unchanged to `branding/BenScr_Icon_Master.png` and embedded as `icon.png`.
It is a transparent 1254 × 1254 PNG of 795819 bytes. Its SHA-256 is
`a5dcabceb766e13c1840a95130ee98a458e12e3713fd9b07f3ceae75a1f5b993`.
No variant, crop, recoloring or resized export is used.

The GitHub repository and NuGet ID were checked for collisions before creation on 2026-11-02.
Every release manifest, package repository field and portable PDB identifies the actual source commit.
