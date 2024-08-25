using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

public class TerrainWorker
{
	public static List<Vector3I> queue = new List<Vector3I>();
	private static object lockObject = new object();
	private static AutoResetEvent itemAddedEvent = new AutoResetEvent(false);
	private static bool isRunning = true;

	// example start
	//Thread workerThread = new Thread(Worker);
	//workerThread.Start();

	// example stop
	// StopWorker();
	// workerThread.Join();

	public static void Worker()
	{
		while (isRunning)
		{
			// Wait for an item to be added or for a reorder to complete
			itemAddedEvent.WaitOne();

			while (true)
			{
				Vector3I? item = null;

				//Stopwatch sw = new Stopwatch();
				//sw.Start();
				// Get the next item to process
				lock (lockObject)
				{
					if (queue.Count > 0)
					{
						item = queue[0];
						queue.RemoveAt(0);
						EventManager.TerrainWorker_CountEvent?.Invoke(queue.Count);
					}
				}

				if (item == null)
				{
					break; // Queue was empty, go back to waiting for new items
				}
				ProcessItem(item.Value);
			//	sw.Stop();
			//	TimeSpan ts = sw.Elapsed;
			//	string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
			//ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
			//	GD.Print("RunTime " + elapsedTime);
			}
		}
	}

	public static void EnqueueItem(Vector3I item)
	{
		lock (lockObject)
		{
			queue.Add(item);
		}
		itemAddedEvent.Set(); // Signal the worker thread that an item is added
	}

	public static void AddList(List<Vector3I> list)
	{
		foreach (Vector3I item in list)
		{
			EnqueueItem(item);
		}
	}

	public static void UpdateList(List<Vector3I> newQueue)
	{
		lock (lockObject)
		{
			queue = newQueue;
		}

		itemAddedEvent.Set(); // Signal the worker thread to start processing the updated list
	}

	public static void StopWorker()
	{
		isRunning = false;
		itemAddedEvent.Set(); // Ensure the worker thread exits the wait state
	}

	static void ProcessItem(Vector3I item)
	{
		if (Chunks.ChunksData.ContainsKey(item)) { return; }
		ChunkData data = new ChunkData(item);
		if (!Chunks.heightMaps.ContainsKey(item))
		{
			Chunks.heightMaps.Add(item, data.GenerateHeightMap());
		}
		int[,] heightMap = Chunks.heightMaps[item];
		data.GenerateTerrain(ref heightMap);
		if (Chunks.ChunksData.ContainsKey(item)) { return; }
		Chunks.ChunksData.Add(item, data);
		MeshWorker.EnqueueItem(Chunks.ChunksData[item]);
	}
}
