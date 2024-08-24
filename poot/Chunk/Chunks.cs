using Godot;
using Godot.Collections;

public partial class Chunks : Node3D
{

	[Export] public Dictionary<Vector3I, Chunk> chunks = new Dictionary<Vector3I, Chunk>();
	[Export] public PackedScene chunkScene;

	[Export] public Vector3I ChunksAroundMe = new Vector3I(3, 3, 3);
	[Export] public Camera3D Camera;

	public struct ChunkGenInfoHelper
	{
		public Vector3I Location;
		public float DistanceFromPlayer;

		public ChunkGenInfoHelper(Vector3I location, float distanceFromPlayer)
		{
			Location = location;
			DistanceFromPlayer = distanceFromPlayer;
		}
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (Chunk.AtlasTexture == null)
		{
			Chunk.AtlasTexture = Chunk.CreateTextureAtlas(Chunk.blockTextures, out Chunk.uvMappings);
		}
		for (int x = 0; x < ChunksAroundMe.X; x++)
		{
			for (int y = 0; y < ChunksAroundMe.Y; y++)
			{
				for (int z = 0; z < ChunksAroundMe.Z; z++)
				{
					AddChunk(new Vector3I(x - (ChunksAroundMe.X / 2), y - (ChunksAroundMe.Y / 2), z - (ChunksAroundMe.Z / 2)));
				}
			}
		}
	}

	public void AddChunk(Vector3I pos)
	{
		Chunk chunkInstance = (Chunk)chunkScene.Instantiate();
		chunkInstance.ChunkLocation = pos;
		chunkInstance.Transform = new Transform3D(Basis.Identity, new Vector3(pos.X * 16, pos.Y * 16, pos.Z * 16));
		chunkInstance.Load();
		AddChild(chunkInstance);
		chunks[pos] = chunkInstance;
	}

	public System.Collections.Generic.List<ChunkGenInfoHelper> GetNeededChunks(Vector3I characterLocation, Vector3I dimensions, Dictionary<Vector3I, Chunk> currentChunks)
	{
		System.Collections.Generic.List<ChunkGenInfoHelper> neededChunks = new();
		for(int x = 0; x < dimensions.X; x++)
		{
			for(int y = 0; y < dimensions.Y; y++)
			{
				for(int z = 0; z < dimensions.Z; z++)
				{
					Vector3I correctedPosition = new Vector3I(x + characterLocation.X, y + characterLocation.Y, z + characterLocation.Z);
					if (!currentChunks.ContainsKey(correctedPosition))
					{
						neededChunks.Add(new ChunkGenInfoHelper(correctedPosition, characterLocation.DistanceTo(characterLocation)));
					}
				}
			}
		}
		return neededChunks;
	}

	public Array<Vector3I> SortChunks(System.Collections.Generic.List<ChunkGenInfoHelper> chunks, Vector3I characterLocation)
	{
		Dictionary<float, Array<Vector3I>> map = new Dictionary<float, Array<Vector3I>>();
		Array<float> keys = new Array<float>();
		foreach(var chunk in chunks)
		{
			keys.Add(chunk.DistanceFromPlayer);
			if(!map.ContainsKey(chunk.DistanceFromPlayer))
			{
				map.Add(chunk.DistanceFromPlayer, new Array<Vector3I>());
			}
			map[chunk.DistanceFromPlayer].Add(chunk.Location);
		}
		return new Array<Vector3I>();
	}
}
