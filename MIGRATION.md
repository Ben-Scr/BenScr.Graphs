# Compatibility

`BenScr.Graphs` is a new package and API. It does not replace an existing published package or
claim source/binary compatibility with another graph library. No application or private-repository
source was migrated.

All public APIs are inventoried in `docs/PublicApi.txt` and checked during verification. The package,
assembly and root namespace use `BenScr.Graphs`. Version 1.0.0 targets .NET 10.

To adapt an adjacency-list model, pass every vertex (including isolated ones) and an edge for each
adjacency entry. Duplicate edges are collapsed. Supply the same equality comparer used by the source
model; vertices duplicated under that comparer must be resolved before constructing the graph.
Retain stable vertex equality/hash codes and construct a new graph when its structure changes.

Algorithm result order is part of the documented contract. SCC presentation order is based on input
vertices and must not be used as a topological order of the component graph.
