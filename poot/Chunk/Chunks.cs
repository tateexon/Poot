using Godot;
using System;
using System.Collections.Generic;

public partial class Chunks : Node3D
{
	public static ThreadSafeDictionary<Vector3I, Chunk> ChunksMesh = new ThreadSafeDictionary<Vector3I, Chunk>();
	public static ThreadSafeDictionary<Vector3I, ChunkData> ChunksData = new ThreadSafeDictionary<Vector3I, ChunkData>();
	public static ThreadSafeDictionary<Vector3I, int[,]> heightMaps = new ThreadSafeDictionary<Vector3I, int[,]>();
	public static ThreadSafeList<Vector3I> ChunksReadyForMesh = new ThreadSafeList<Vector3I>();
	public static ThreadSafeList<Vector3I> ChunksReadyToShow = new ThreadSafeList<Vector3I>();

	private int _maxChunksRenderedPerFrame = 1000;
	private int _maxChunksRemovedPerFrame = 1000;

	private const int _TERRAIN_WORKER_THREADS = 1;
	private const int _MESH_WORKER_THREADS = 5;
	private const float _CUBE_DIAGONAL = 1.732051f; //Mathf.Sqrt(3);

	[Export] public PackedScene chunkScene;

	public static Vector3I Dimensions = new Vector3I(25, 12, 25);
	//public static Vector3I Dimensions = new Vector3I(18, 9, 18);
	//public static Vector3I Dimensions = new Vector3I(9, 6, 9);
	//public static Vector3I Dimensions = new Vector3I(3, 3, 3);


	private float _maxGenerateDistance;
	private float _maxRenderDistance;
	private float _maxRemoveDistance;

	[Export] public Camera3D Camera;

	public static TerrainWorker _terrainWorkerQueue;
	private MeshWorker _meshWorkerQueue;

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

		_maxGenerateDistance = CalculateMaxDistance(Dimensions);
		_maxRenderDistance = CalculateMaxDistance(Dimensions);
		_maxRemoveDistance = CalculateMaxDistance(Dimensions);
		_maxRemoveDistance = Mathf.Ceil(_maxRemoveDistance / 2f) + _CUBE_DIAGONAL;

		GD.Print($"render {_maxRenderDistance}");
		GD.Print($"generate {_maxGenerateDistance}");
		GD.Print($"remove {_maxRemoveDistance}");
		GD.Print($"{Int32.MaxValue}");
		GD.Print($"{32*32*32}");


		// start worker threads
		_terrainWorkerQueue = new TerrainWorker(_TERRAIN_WORKER_THREADS);
		_terrainWorkerQueue.StartWorkerThreads();

		_meshWorkerQueue = new MeshWorker(_MESH_WORKER_THREADS);
		_meshWorkerQueue.ChunkScene = chunkScene;
		_meshWorkerQueue.StartWorkerThreads();

		LoadNewChunksToGenerate(Camera.GlobalPosition, Dimensions);
		_firstGetNeededChunks = false;
	}

	public override void _ExitTree()
	{
		// Stop worker threads
		_terrainWorkerQueue.Stop();
		_meshWorkerQueue.Stop();
	}

	private int tickCounter = 0;
	private int tickEnd = 15;
	public override void _PhysicsProcess(double delta)
	{

		if (tickCounter >= tickEnd || tickCounter == -1)
		{
			tickCounter = 0;
			LoadNewChunksToGenerate(Camera.GlobalPosition, Dimensions);
			LoadChunksToRender(Camera.GlobalPosition, Dimensions);
			AddChunksFromQueue();
			RemoveChunksOutOfRange(Camera.GlobalPosition, Dimensions);
		}
		tickCounter++;
	}

	private void LoadNewChunksToGenerate(Vector3 centerChunk, Vector3I dimensions)
	{
		var neededChunks = GetNeededChunks(centerChunk, dimensions, ChunksData);
		if (neededChunks.SafeCount() > 0)
		{
			var sortedChunks = SortChunks(neededChunks, centerChunk);
			_terrainWorkerQueue.UpdateList(sortedChunks);
		}
	}

	private void LoadChunksToRender(Vector3 centerChunk, Vector3I dimensions)
	{
		Vector3I correctedCenterChunk = ((Vector3I)centerChunk) / ChunkData.Size;
		var neededChunks = new ThreadSafeList<Vector3I>();
		var sortedData = SortChunks(ChunksReadyForMesh, centerChunk);
		foreach (var chunk in sortedData)
		{
			if (correctedCenterChunk.DistanceTo(chunk) >= _maxRenderDistance) { continue; }
			ChunkData d = ChunksData.SafeGet(chunk);
			if (d.Blocks == null) { continue; }
			if (d.IsGenerated && !d.IsDirty)
			{
				ChunksReadyForMesh.SafeRemove(chunk);
			}
			else if (d.IsGenerated && d.IsDirty && !neededChunks.SafeContains(chunk))
			{
				neededChunks.SafeAdd(chunk);
			}
		}
		if (neededChunks.SafeCount() > 0)
		{
			var sortedChunks = SortChunks(neededChunks, Camera.GlobalPosition);
			_meshWorkerQueue.UpdateList(sortedChunks);
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
			if (i >= _maxChunksRenderedPerFrame)
			{
				break;
			}
		}
	}

	private int _plusChunks = 2;

	private void RemoveChunksOutOfRange(Vector3 centerChunk, Vector3I dimensions)
	{
		Vector3 cameraPosition = ((Vector3I)centerChunk) / ChunkData.Size;

		// loop over chunks data
		var keys = ChunksData.SafeGetKeys();
		int count = 0;
		foreach (Vector3I key in keys)
		{
			float distanceAway = cameraPosition.DistanceTo(key);
			if (Mathf.Abs(distanceAway) > _maxRemoveDistance)
			{
				//GD.Print($"Removing chunk {key}");
				ChunksMesh.SafeGet(key)?.QueueFree();
				ChunksMesh.SafeRemove(key);
				ChunksData.SafeRemove(key);
				heightMaps.SafeRemove(key);
				if (ChunksReadyToShow.SafeContains(key))
				{
					ChunksReadyToShow.SafeRemove(key);
				}
				if (ChunksReadyForMesh.SafeContains(key))
				{
					ChunksReadyForMesh.SafeRemove(key);
				}
				count++;
				if (count == _maxChunksRemovedPerFrame)
				{
					break;
				}
			}
		}
	}

	private Vector3I _lastPosition = Vector3I.Zero;
	private bool _firstGetNeededChunks = true;
	// The center chunk is where the character is at so we need to get all the chunks around it
	private ThreadSafeList<Vector3I> GetNeededChunks(Vector3 centerChunk, Vector3I dimensions, ThreadSafeDictionary<Vector3I, ChunkData> currentChunks)
	{
		// short circuit if this isn't the first iterations and we haven't moved
		Vector3I c = GetCorrectedVector3IFromWorldSpace(centerChunk);
		if (!_firstGetNeededChunks && _lastPosition == c)
		{
			return new ThreadSafeList<Vector3I>();
		}
		_lastPosition = c;


		// find the needed chunks within the dimensions provided to loop over
		ThreadSafeList<Vector3I> neededChunks = new();
		int halfDimX = Mathf.FloorToInt(dimensions.X / 2f);
		int halfDimY = Mathf.FloorToInt(dimensions.Y / 2f);
		int halfDimZ = Mathf.FloorToInt(dimensions.Z / 2f);

		//GD.Print($"gen {maxDistance}");

		for (int x = -halfDimX; x <= halfDimX; x++)
		{
			for (int y = -halfDimY; y <= halfDimY; y++)
			{
				for (int z = -halfDimZ; z <= halfDimZ; z++)
				{
					Vector3I correctedPosition = new Vector3I(c.X + x, c.Y + y, c.Z + z);
					float distance = c.DistanceTo(correctedPosition);

					if (distance >= _maxGenerateDistance)
					{
						continue;
					}
					if (!currentChunks.SafeContainsKey(correctedPosition))
					{

						neededChunks.SafeAdd(correctedPosition);
					}
				}
			}
		}
		return neededChunks;
	}

	// Get the chunk space from the world space and account for negative numbers
	private Vector3I GetCorrectedVector3IFromWorldSpace(Vector3 position)
	{
		int chunkX = Mathf.FloorToInt(position.X / ChunkData.Size);
		int chunkY = Mathf.FloorToInt(position.Y / ChunkData.Size);
		int chunkZ = Mathf.FloorToInt(position.Z / ChunkData.Size);
		return new Vector3I(chunkX, chunkY, chunkZ);
	}

	// Helper function to calculate max distance for a given dimension
	// get max distance for a circular/elliptical area in a 3D space sprt(x^2 + y^2 + z^2)
	private float CalculateMaxDistance(Vector3 dimensions)
	{
		float halfX = dimensions.X / 2f;
		float halfY = dimensions.Y / 2f;
		float halfZ = dimensions.Z / 2f;

		return Mathf.Sqrt(halfX * halfX + halfY * halfY + halfZ * halfZ);
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
