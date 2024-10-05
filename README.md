# Pretty much a brute force implementation of a minecraft like voxel generator in godot c# with a chunk server written in rust

Not really sure where I am going with this. Just wanted to see what it took to do this.

# Things that mostly work

- Terrain generation in its own thread + height map has been moved to rust server
- Mesh generation in its own thread
- Generate chunks around player as they move
- Remove chunks outside a certain radius of the player
- Caves generate but are pretty blobby
- Heightmap is generated in the gpu using rust and vulcan
- Water is added but has visual bugs at the edge, probably just needs to be separated to its own mesh

# TODO - no idea if I will actually get to them

- Add lighting, maybe multi colored if I get ambitious
- Add more robust noise library along with better caves and maybe biomes. Update: in progress as I move generation to a rust server and am using libnoise https://libnoise.sourceforge.net/index.html rewritten in rust at https://github.com/Razaekel/noise-rs
- - Update on this, moving noise generation to gpu since it is much much faster, server in rust is working and heightmaps in the gpu are generated with a basic perlin2d function, caves still in progress.
- Add some tree generation
- save/load generated chunks
- Player can add or remove blocks
- Some sort of physics so the player can walk and jump instead of fly.
- Migrate the mesh generation to rust via GDExtension and while doing so make it greedy via bit flips
- Add to this list

# To Run

You will need to go into the poot_server directory and run `make run` to start the chunk server, then run the game from the Chunks scene. This is here because I will need it when I eventually come back to this in several years and have no idea how to make it work any more because I couldn't be bothered to get the game to start the chunk server itself.
