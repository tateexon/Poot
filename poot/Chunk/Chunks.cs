using Godot;
using System.Collections.Generic;
using System.Threading;

public partial class Chunks : Node3D
{
	public static ThreadSafeDictionary<Vector3I, Chunk> ChunksMesh = new ThreadSafeDictionary<Vector3I, Chunk>();
	public static ThreadSafeDictionary<Vector3I, ChunkData> ChunksData = new ThreadSafeDictionary<Vector3I, ChunkData>();
	public static ThreadSafeDictionary<Vector3I, int[,]> heightMaps = new ThreadSafeDictionary<Vector3I, int[,]>();
	public static Queue<Vector3I> ChunksReadyToShow = new Queue<Vector3I>();

	[Export] public PackedScene chunkScene;

	public static Vector3I Dimensions = new Vector3I(12, 9, 12);

	[Export] public Camera3D Camera;

	private Thread terrainWorkerThread;
	private Thread meshWorkerThread;

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
		// make sure the texture atlas is created before starting worker threads
		if (BlockAtlasTexture.AtlasTexture == null)
		{
			BlockAtlasTexture.AtlasTexture = BlockAtlasTexture.CreateTextureAtlas(BlockAtlasTexture.BlockTextures, out BlockAtlasTexture.UvMappings);
		}

		// start worker threads
		terrainWorkerThread = new Thread(TerrainWorker.Worker);
		terrainWorkerThread.Start();

		MeshWorker.ChunkScene = chunkScene;
		meshWorkerThread = new Thread(MeshWorker.Worker);
		meshWorkerThread.Start();
	}

	public override void _ExitTree()
	{
		// Stop worker threads
		TerrainWorker.StopWorker();
		terrainWorkerThread.Join();

		MeshWorker.StopWorker();
		meshWorkerThread.Join();
	}

	private int tickCounter = -1;
	public override void _PhysicsProcess(double delta)
	{
		if (tickCounter >= 15 || tickCounter == -1)
		{
			tickCounter = 0;
			Vector3I location = (Vector3I)(Camera.GlobalPosition / ChunkData.Size);
			var neededChunks = GetNeededChunks(location, Dimensions, ChunksData);
			if (neededChunks.Count > 0)
			{
				//GD.Print("Add Chunks");
				var sortedChunks = SortChunks(neededChunks);
				TerrainWorker.UpdateList(sortedChunks);
			}
			RemoveChunksOutOfRange();
		}
		tickCounter++;
	}

	public override void _Process(double delta)
	{
		AddChunksFromQueue();
	}

	public void AddChunksFromQueue()
	{
		if (ChunksReadyToShow.Count == 0) { return; }
		int maxInOneFrame = 200;
		int currentCount = ChunksReadyToShow.Count;
		for (int i = 0; i < currentCount; i++)
		{
			Vector3I index = ChunksReadyToShow.Dequeue();
			//GD.Print($"Adding child: {c}");
			Chunk c = ChunksMesh.SafeGet(index);
			if (c != null)
			{
				AddChild(c);
			}
			if (i >= maxInOneFrame)
			{
				break;
			}
		}
	}

	// The center chunk is where the character is at so we need to get all the chunks around it
	public List<ChunkGenInfoHelper> GetNeededChunks(Vector3I centerChunk, Vector3I dimensions, ThreadSafeDictionary<Vector3I, ChunkData> currentChunks)
	{
		List<ChunkGenInfoHelper> neededChunks = new();
		int xx = centerChunk.X - (dimensions.X / 2); // 1-(3/2) = 0
		int yy = centerChunk.Y - (dimensions.Y / 2);
		int zz = centerChunk.Z - (dimensions.Z / 2);
		for (int x = 0; x < dimensions.X; x++)
		{
			for (int y = 0; y < dimensions.Y; y++)
			{
				for (int z = 0; z < dimensions.Z; z++)
				{
					Vector3I correctedPosition = new Vector3I(x + xx, y + yy, z + zz);
					if (!currentChunks.SafeContainsKey(correctedPosition))
					{
						neededChunks.Add(new ChunkGenInfoHelper(correctedPosition, centerChunk.DistanceTo(correctedPosition)));
					}
				}
			}
		}
		return neededChunks;
	}

	public List<Vector3I> SortChunks(List<ChunkGenInfoHelper> chunks)
	{
		// use a map and a list of keys to store lists of chunks to generate
		Dictionary<float, List<Vector3I>> map = new Dictionary<float, List<Vector3I>>();
		List<float> keys = new List<float>();

		foreach (var chunk in chunks)
		{
			if (!map.ContainsKey(chunk.DistanceFromPlayer))
			{
				map.Add(chunk.DistanceFromPlayer, new List<Vector3I>());

				// only add the key once since we can have duplicates
				keys.Add(chunk.DistanceFromPlayer);
			}
			map[chunk.DistanceFromPlayer].Add(chunk.Location);
		}

		// sort the keys so we can access them from shortest to longest
		keys.Sort();

		// loop through keys shortest to longest and add them to the list
		// this makes the first element the highest priority
		List<Vector3I> sortedChunksByDistance = new List<Vector3I>();
		float largest = 0;
		foreach (var key in keys)
		{
			if (Mathf.Abs(key) > largest)
			{
				largest = key;
			}
			foreach (var chunk in map[key])
			{
				sortedChunksByDistance.Add(chunk);
			}
		}
		//GD.Print($"largest distance is {largest}");
		return sortedChunksByDistance;
	}

	public void RemoveChunksOutOfRange()
	{
		float cullDistance = ((Dimensions.X + 2) * Mathf.Sqrt2) / 2;
		Vector3 cameraPosition = ((Vector3I)Camera.GlobalPosition) / ChunkData.Size;
		// loop over chunks data
		var keys = ChunksMesh.SafeGetKeys();
		int maxOnOneFrame = 200;
		int count = 0;
		foreach (Vector3I key in keys)
		{
			float distanceAway = cameraPosition.DistanceTo(key);
			if (Mathf.Abs(distanceAway) >= cullDistance)
			{
				//GD.Print($"cull distance: {cullDistance} distanceAway: {distanceAway}");
				ChunksMesh.SafeGet(key)?.QueueFree();
				ChunksMesh.SafeRemove(key);
				ChunksData.SafeRemove(key);
				heightMaps.SafeRemove(key);
				//GD.Print($"Removed chunk at {kvp.Key}");
				// TODO maybe? will we ever try to remove a chunk that hasn't been drawn yet?
				//if (ChunksReadyToShow.Contains(kvp.Key))
				//{
				//	ChunksReadyToShow.
				//}
				count++;
				if (count == maxOnOneFrame)
				{
					break;
				}
			}
		}
	}
}
