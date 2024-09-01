# Pretty much a brute force implementation of a minecraft like voxel generator in godot c# with a chunk server written in rust

Not really sure where I am going with this. Just wanted to see what it took to do this.

# Things that mostly work

- Terrain generation in its own thread + height map has been moved to rust server
- Mesh generation in its own thread
- Generate chunks around player as they move
- Remove chunks outside a certain radius of the player
- Caves generate but are pretty blobby
- Heightmap is generated in the gpu using rust and vulcan but has bugs
- Water is added but has visual bugs at the edge, probably just needs to be separated to its own mesh

# TODO - no idea if I will actually get to them

- Add lighting, maybe multi colored if I get ambitious
- Add more robust noise library along with better caves and maybe biomes. Update: in progress as I move generation to a rust server and am using libnoise https://libnoise.sourceforge.net/index.html rewritten in rust at https://github.com/Razaekel/noise-rs
- - Update on this, moving noise generation to gpu since it is much much faster, server in rust is working and heightmaps in the gpu are generated with a basic perlin2d function, caves still in progress.
- Add some tree generation
- Player can add or remove blocks
- Some sort of physics so the player can walk and jump instead of fly.
- Add to this list
