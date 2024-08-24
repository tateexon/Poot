using Godot;
using System;
using System.Collections.Generic;
using System.Threading;

public partial class TerrainWorker
{
	public static List<Vector3I> queue = new List<Vector3I>();
	private static object lockObject = new object();
	private static AutoResetEvent itemAddedEvent = new AutoResetEvent(false);
	private static AutoResetEvent waitForReorderEvent = new AutoResetEvent(true);
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

				// If reorder is requested, wait for it to complete
				waitForReorderEvent.WaitOne();

				// Get the next item to process
				lock (lockObject)
				{
					if (queue.Count > 0)
					{
						item = queue[0];
						queue.RemoveAt(0);
					}
				}

				if (item == null)
				{
					break; // Queue was empty, go back to waiting for new items
				}

				ProcessItem(item.Value);
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
		waitForReorderEvent.Reset(); // Block the worker thread from processing items

		lock (lockObject)
		{
			queue = newQueue;
		}

		waitForReorderEvent.Set(); // Allow the worker thread to continue processing
		itemAddedEvent.Set(); // Signal the worker thread to start processing the updated list
	}

	static void StopWorker()
	{
		isRunning = false;
		itemAddedEvent.Set(); // Ensure the worker thread exits the wait state
	}

	static void ProcessItem(Vector3I item)
	{
		Console.WriteLine($"Processing {item}");
	}
}
