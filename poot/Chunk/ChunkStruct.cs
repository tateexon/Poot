using Godot;
using System;

public struct ChunkStruct
{
	public const int Size = 16;
	public const int BlockCount = Size * Size * Size;

	public int[,] HeightMap;
	public BlockType[,,] Blocks;
	public Vector3I Location;
	public bool IsGenerated = false;

	public ChunkStruct(Vector3I location)
	{
		Blocks = new BlockType[Size, Size, Size];
		HeightMap = new int[Size, Size];
		Location = location;
	}

	private int[,] GenerateHeightMap()
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
}
