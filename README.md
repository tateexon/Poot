# Pretty much a brute force implementation of a minecraft like voxel generator in godot

Not really sure where I am going with this. Just wanted to see what it took to do this.

# Things that mostly work

- Terrain generation in its own thread + height map
- Mesh generation in its own thread
- Generate chunks around player as they move - TODO change to a radius instead of a hard square
- Remove chunks outside a certain radius of the player
- Caves generate but are pretty blobby, need to move to probably a more robust noise libray like maybe libnoise

# TODO - no idea if I will actually get to them

- Add lighting, maybe multi colored if I get ambitious
- Add more robust noise library along with better caves and maybe biomes
- Add some tree generation
- Player can add or remove blocks
- Some sort of physics so the player can walk and jump instead of fly.
- Add to this list
