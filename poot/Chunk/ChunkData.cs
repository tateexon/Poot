using Godot;
using System;
using System.Net.Http;

public struct ChunkData
{
	public const int Size = 16;
	public const int BlockCount = Size * Size * Size;
	public static long Seed = 12345L;

	public BlockType[,,] Blocks;
	public Vector3I Location;
	public bool IsGenerated = false;
	public bool IsDirty = false;

	public ChunkData(Vector3I location)
	{
		Blocks = new BlockType[Size, Size, Size];
		Location = location;
	}

	//public float[,] GenerateHeightMap()
	//{
	//	FastNoiseLite terrainNoiseF = new FastNoiseLite();
	//	terrainNoiseF.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
	//	terrainNoiseF.FractalOctaves = 3;
	//	terrainNoiseF.Frequency = 0.01f;

	//	float[,] heightMap = new float[Size, Size];

	//	for (int x = 0; x < Size; x++)
	//	{
	//		for (int z = 0; z < Size; z++)
	//		{
	//			int cX = x + (Size * Location.X);
	//			int cZ = z + (Size * Location.Z);

	//			// Generate height map
	//			float heightValue = terrainNoiseF.GetNoise2D(cX, cZ);
	//			heightMap[x, z] = heightValue;
	//		}
	//	}
	//	return heightMap;
	//}

	public int[,] HeightFromFloat(float[,] heightMap)
	{
		int[,] newMap = new int[Size, Size];
		for (int x = 0; x < Size; x++)
		{
			for (int z = 0; z < Size; z++)
			{
				float heightValue = heightMap[x, z];
				int height = Mathf.RoundToInt(Mathf.Lerp(64, Size - 64, (heightValue + 1) / 2.0f));
				newMap[x, z] = height;
			}
		}
		return newMap;
	}

	public void GenerateTerrain(ref int[,] heightMap)
	{
		//FastNoiseLite caveNoiseF = new FastNoiseLite();
		//caveNoiseF.Seed = (int)Seed;
		//caveNoiseF.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
		//caveNoiseF.FractalOctaves = 3;
		//caveNoiseF.Frequency = 0.02f;

		float[,,] caveMap = FetchCaveMap();

		for (int x = 0; x < Size; x++)
		{
			for (int z = 0; z < Size; z++)
			{
				for (int y = 0; y < Size; y++)
				{
					int cY = y + (Size * Location.Y);
					int cX = x + (Size * Location.X);
					int cZ = z + (Size * Location.Z);
					int height = heightMap[x, z];

					if (cY > height)
					{
						if (cY <= 0)
						{
							Blocks[x, y, z] = BlockType.Water;
						}
						else
						{
							Blocks[x, y, z] = BlockType.Air;
						}
					}
					else if (cY == height)
					{
						if (height <= 0)
						{
							Blocks[x, y, z] = BlockType.Sand;
						}
						else
						{
							Blocks[x, y, z] = BlockType.Grass;
						}

					}
					else if (cY >= height - 3)
					{
						if (height <= 0)
						{
							Blocks[x, y, z] = BlockType.Stone;
						}
						else
						{
							Blocks[x, y, z] = BlockType.Dirt;
						}
					}
					else
					{
						Blocks[x, y, z] = BlockType.Stone;
					}


					// Generate caves
					//float caveValue = caveNoiseF.GetNoise3D(cX, cY, cZ);
					float caveValue = caveMap[x, y, z];
					if (caveValue > 0.7f)
					{
						var b = Blocks[x, y, z];
						if (b != BlockType.Air && b != BlockType.Water)
						{
							Blocks[x, y, z] = BlockType.Air;
						}

					}
				}
			}
		}
		IsGenerated = true;
		IsDirty = true;
	}

	private static readonly System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
	public float[,] FetchHeightMap()
	{
		float[,] heightMap = new float[Size, Size];
		string url = $"http://127.0.0.1:8080/height/{Seed}/{Location.X}/{Location.Z}";
		try
		{
			// Send the request and get the response synchronously
			string responseText = httpClient.GetStringAsync(url).Result;

			// Parse the response into a 2D float array
			heightMap = ParseHeightMap(responseText);
		}
		catch (HttpRequestException e)
		{
			// Handle any errors during the HTTP request
			Console.WriteLine($"Request error: {e.Message}");
			return null;
		}

		return heightMap;
	}


	private float[,] ParseHeightMap(string data)
	{
		//string[] lines = data.Split('\n');
		float[,] heightMap = new float[Size, Size];
		string[] values = data.Trim().Split(' ');
		int index = 0;
		for (int y = 0; y < Size; y++)
		{
			//string[] values = lines[y].Trim().Split(' ');
			for (int x = 0; x < Size; x++)
			{
				if (float.TryParse(values[index]/*values[x]*/, out float height))
				{
					heightMap[x, y] = height;
				}
				else
				{
					GD.PrintErr($"Failed to parse value at line {y + 1}, column {x + 1}: {values[x]}");
				}
				index++;
			}
		}

		return heightMap;
	}

	public float[,,] FetchCaveMap()
	{
		float[,,] caveMap = new float[Size, Size, Size];
		string url = $"http://127.0.0.1:8080/cave/{Seed}/{Location.X}/{Location.Y}/{Location.Z}";
		try
		{
			// Send the request and get the response synchronously
			string responseText = httpClient.GetStringAsync(url).Result;

			// Parse the response into a 2D float array
			caveMap = ParseCaveMap(responseText);
		}
		catch (HttpRequestException e)
		{
			// Handle any errors during the HTTP request
			Console.WriteLine($"Request error: {e.Message}");
			return null;
		}

		return caveMap;
	}

	private float[,,] ParseCaveMap(string data)
	{
		float[,,] caveMap = new float[Size, Size, Size];
		string[] values = data.Trim().Split(' ');
		int index = 0;
		for (int x = 0; x < Size; x++)
		{
			for (int y = 0; y < Size; y++)
			{
				for (int z = 0; z < Size; z++)
				{
					if (float.TryParse(values[index], out float height))
					{
						caveMap[x, y, z] = (height + 1) / 2;
					}
					else
					{
						GD.PrintErr($"Failed to parse value at line {y + 1}, column {x + 1}: {values[x]}");
					}
					index++;
				}
			}
		}

		return caveMap;
	}
}
