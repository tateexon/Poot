using Godot;
using System;

public struct ChunkData
{
	public const int Size = 16;
	public const int BlockCount = Size * Size * Size;
	public static long Seed = 12345L;

	public BlockType[,,] Blocks;
	public Vector3I Location;
	public bool IsGenerated = false;

	public ChunkData(Vector3I location)
	{
		Blocks = new BlockType[Size, Size, Size];
		Location = location;
	}

	public int[,] GenerateHeightMap()
	{
		//OpenSimplexNoise terrainNoise = new OpenSimplexNoise(Seed);
		FastNoiseLite terrainNoiseF = new FastNoiseLite();
		terrainNoiseF.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
		terrainNoiseF.FractalOctaves = 4;
		terrainNoiseF.Frequency = 0.01f;
		
		int[,] heightMap = new int[Size, Size];

		for (int x = 0; x < Size; x++)
		{
			for (int z = 0; z < Size; z++)
			{
				int cX = x + (Size * Location.X);
				int cZ = z + (Size * Location.Z);
				// Generate height map
				//float heightValue = (float)terrainNoise.Evaluate(cX, cZ);
				float heightValue = terrainNoiseF.GetNoise2D(cX, cZ);
				//float heightValue = OpenSimplexNoise2.Noise2(seed, cX, cZ);
				int height = Mathf.RoundToInt(Mathf.Lerp(4, Size - 4, (heightValue + 1) / 2.0f));
				heightMap[x, z] = height;
			}
		}
		return heightMap;
	}

	public void GenerateTerrain(ref int[,] heightMap)
	{
		//OpenSimplexNoise caveNoise = new OpenSimplexNoise(Seed + 1);
		FastNoiseLite caveNoiseF = new FastNoiseLite();
		caveNoiseF.Seed = (int)Seed;
		caveNoiseF.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
		caveNoiseF.FractalOctaves = 3;
		caveNoiseF.Frequency = 0.01f;
		//caveNoise.Octaves = 3;
		//caveNoise.Period = 25;
		//caveNoise.Persistence = 0.7f;

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
						Blocks[x, y, z] = BlockType.Air;
					}
					else if (cY == height)
					{
						Blocks[x, y, z] = BlockType.Grass;
					}
					else if (cY >= height - 3)
					{
						Blocks[x, y, z] = BlockType.Dirt;
					}
					else
					{
						Blocks[x, y, z] = BlockType.Stone;
					}

					// Generate caves
					//float caveValue = (float)caveNoise.Evaluate(cX, cY, cZ);
					float caveValue = caveNoiseF.GetNoise3D(cX, cY, cZ);
					//float caveValue = OpenSimplexNoise2.Noise3_Fallback(seed, cX, cY, cZ);
					if (caveValue > 0.5f)
					{
						Blocks[x, y, z] = BlockType.Air;
					}
				}
			}
		}
	}

}
