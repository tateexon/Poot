using Godot;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

public partial class Chunks : Node3D
{
	public static ThreadSafeDictionary<Vector3I, Chunk> ChunksMesh = new ThreadSafeDictionary<Vector3I, Chunk>();
	public static ThreadSafeDictionary<Vector3I, ChunkData> ChunksData = new ThreadSafeDictionary<Vector3I, ChunkData>();
	public static ThreadSafeDictionary<Vector3I, int[,]> heightMaps = new ThreadSafeDictionary<Vector3I, int[,]>();
	public static ThreadSafeList<Vector3I> ChunksReadyForMesh = new ThreadSafeList<Vector3I>();
	public static ThreadSafeList<Vector3I> ChunksReadyToShow = new ThreadSafeList<Vector3I>();

	private int _MaxChunksRenderedPerFrame = 1000;
	private int _MaxChunksRemovedPerFrame = 1000;

	[Export] public PackedScene chunkScene;

	public static Vector3I Dimensions = new Vector3I(15, 9, 15);

	[Export] public Camera3D Camera;

	private TerrainWorker terrainWorkerQueue;
	public static MeshWorker meshWorkerQueue;

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
		terrainWorkerQueue = new TerrainWorker(10);
		terrainWorkerQueue.StartWorkerThreads();

		meshWorkerQueue = new MeshWorker(10);
		meshWorkerQueue.ChunkScene = chunkScene;
		meshWorkerQueue.StartWorkerThreads();
	}

	public override void _ExitTree()
	{
		// Stop worker threads
		terrainWorkerQueue.Stop();
		meshWorkerQueue.Stop();
	}

	private int tickCounter = -1;
	private int tickEnd = 15;
	public override void _PhysicsProcess(double delta)
	{

		if (tickCounter >= tickEnd || tickCounter == -1)
		{
			tickCounter = 0;
			LoadNewChunksToGenerate();
			LoadChunksToRender();
			AddChunksFromQueue();
			RemoveChunksOutOfRange();
		}
		tickCounter++;
	}

	public override void _Process(double delta)
	{
		AddChunksFromQueue();
		RemoveChunksOutOfRange();
	}

	private void LoadNewChunksToGenerate()
	{
		var neededChunks = GetNeededChunks((Vector3I)Camera.GlobalPosition, Dimensions, ChunksData);
		if (neededChunks.SafeCount() > 0)
		{
			var sortedChunks = SortChunks(neededChunks, Camera.GlobalPosition);
			terrainWorkerQueue.UpdateList(sortedChunks);
		}
	}

	private void LoadChunksToRender()
	{
		var data = ChunksReadyForMesh.SafeShallowClone();
		var neededChunks = new ThreadSafeList<Vector3I>();
		foreach (var chunk in data)
		{
			ChunkData d = ChunksData.SafeGet(chunk);
			if (d.Blocks == null) { continue; }
			if (d.IsGenerated && !d.IsDirty)
			{
				ChunksReadyForMesh.SafeRemove(chunk);
			}
			else if (d.IsGenerated && d.IsDirty)
			{
				neededChunks.SafeAdd(chunk);
			}
		}
		if (neededChunks.SafeCount() > 0)
		{
			var sortedChunks = SortChunks(neededChunks, Camera.GlobalPosition);
			meshWorkerQueue.UpdateList(sortedChunks);
		}
	}

	private void AddChunksFromQueue()
	{
		if (ChunksReadyToShow.SafeCount() == 0) { return; }
		List<Vector3I> sortedChunks = SortChunks(ChunksReadyToShow, Camera.GlobalPosition);
		int currentCount = sortedChunks.Count;
		for (int i = 0; i < currentCount; i++)
		{
			Vector3I index = sortedChunks[0];
			sortedChunks.RemoveAt(0);
			ChunksReadyToShow.SafeRemove(index);

			Chunk c = ChunksMesh.SafeGet(index);
			if (c != null)
			{
				AddChild(c);
			}
			if (i >= _MaxChunksRenderedPerFrame)
			{
				break;
			}
		}
	}

	private void RemoveChunksOutOfRange()
	{
		float cullDistance = ((Dimensions.X + 2) * Mathf.Sqrt2) / 2;
		Vector3 cameraPosition = ((Vector3I)Camera.GlobalPosition) / ChunkData.Size;
		// loop over chunks data
		var keys = ChunksMesh.SafeGetKeys();
		int count = 0;
		foreach (Vector3I key in keys)
		{
			float distanceAway = cameraPosition.DistanceTo(key);
			if (Mathf.Abs(distanceAway) >= cullDistance)
			{
				ChunksMesh.SafeGet(key)?.QueueFree();
				ChunksMesh.SafeRemove(key);
				ChunksData.SafeRemove(key);
				heightMaps.SafeRemove(key);
				if (ChunksReadyToShow.SafeContains(key))
				{
					ChunksReadyToShow.SafeRemove(key);
				}
				if (!ChunksReadyForMesh.SafeContains(key))
				{
					ChunksReadyForMesh.SafeRemove(key);
				}
				count++;
				if (count == _MaxChunksRemovedPerFrame)
				{
					break;
				}
			}
		}
	}

	// The center chunk is where the character is at so we need to get all the chunks around it
	private ThreadSafeList<Vector3I> GetNeededChunks(Vector3I centerChunk, Vector3I dimensions, ThreadSafeDictionary<Vector3I, ChunkData> currentChunks)
	{
		Vector3I correctedCenterChunk = centerChunk / ChunkData.Size;
		ThreadSafeList<Vector3I> neededChunks = new();
		int xx = correctedCenterChunk.X - (dimensions.X / 2); // 1-(3/2) = 0
		int yy = correctedCenterChunk.Y - (dimensions.Y / 2);
		int zz = correctedCenterChunk.Z - (dimensions.Z / 2);
		for (int x = 0; x < dimensions.X; x++)
		{
			for (int y = 0; y < dimensions.Y; y++)
			{
				for (int z = 0; z < dimensions.Z; z++)
				{
					Vector3I correctedPosition = new Vector3I(x + xx, y + yy, z + zz);
					if (!currentChunks.SafeContainsKey(correctedPosition))
					{
						neededChunks.SafeAdd(correctedPosition);
					}
				}
			}
		}
		return neededChunks;
	}

	private List<Vector3I> SortChunks(ThreadSafeList<Vector3I> chunks, Vector3 centralPoint)
	{
		// use a map and a list of keys to store lists of chunks to generate
		List<Vector3I> chunksCopy = chunks.SafeShallowClone();
		Dictionary<float, List<Vector3I>> map = new Dictionary<float, List<Vector3I>>();
		List<float> keys = new List<float>();
		Vector3 correctedCentralPoint = centralPoint / ChunkData.Size;

		foreach (var chunk in chunksCopy)
		{
			float distance = correctedCentralPoint.DistanceTo(chunk);
			if (!map.ContainsKey(distance))
			{
				map.Add(distance, new List<Vector3I>());

				// only add the key once since we can have duplicates
				keys.Add(distance);
			}
			map[distance].Add(chunk);
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
		return sortedChunksByDistance;
	}
}
