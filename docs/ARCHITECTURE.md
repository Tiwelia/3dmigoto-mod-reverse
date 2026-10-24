# Architecture

`ModReverse.Core` is independent of the WPF and CLI front ends.

1. `Ini` parses 3Dmigoto INI files while preserving line order, duplicate keys,
   conditions, comments and referenced command lists.
2. `Analysis` resolves model resources and expands DrawIndexed calls. WWMI has a
   dedicated analyzer for shared buffers, components and ShapeKey resources.
3. `Export` validates buffer counts, interleaves vertex streams, slices index
   ranges, rebases indices and writes compatible IB/VB/FMT triples.
4. `Profiles` contains game-specific vertex layouts. ZZZ and WWMI are the first
   validated profiles.
5. `Textures` recursively preserves DDS files and invokes DirectXTex for PNG and
   optional JPG output while preserving relative paths.

The test suite contains byte-for-byte regression tests against the Trigger-Mecha
ZZZ fixture and the Shorekeeper WWMI fixture. These cover:

- 10 whole-model ZZZ layout candidates;
- 62 DrawIndexed ZZZ slices;
- 32 WWMI component slices;
- WWMI ShapeKey semantics and dynamic strides;
- DDS, PNG and JPG output.
