using Godot;
using Godot.Collections;

public partial class Chunk : MeshInstance3D
{
	[Export] public bool IsGenerated = false;
	[Export] public long Seed = 12345L;

	[Export] public Vector3I ChunkLocation = Vector3I.Zero;
	public ChunkStruct data;

	private const int ChunkSize = 16;
	private const int BlockCountMax = ChunkSize * ChunkSize * ChunkSize;
	private const int TotalVertices = BlockCountMax * 6 * 4;  // 6 faces, 4 vertices per face
	private const int TotalIndices = BlockCountMax * 6 * 6;   // 6 faces, 6 indices per face
	private const int TotalUvs = TotalVertices;

	private const bool AddCollision = false;

	public static Texture2D AtlasTexture;
	public static Dictionary<BlockType, Vector2[]> uvMappings;

	public BlockType[,,] blocks = new BlockType[ChunkSize, ChunkSize, ChunkSize];
	private int[,] heightMap = new int[ChunkSize, ChunkSize];

	public static Dictionary<BlockType, Texture2D> blockTextures = new Dictionary<BlockType, Texture2D>
	{
		{ BlockType.Grass, (Texture2D)GD.Load("res://textures/grass.png") },
		{ BlockType.Dirt, (Texture2D)GD.Load("res://textures/dirt.png") },
		{ BlockType.Stone, (Texture2D)GD.Load("res://textures/stone.png") },
	};

	public override void _Ready()
	{
		NewEmptyChunk();
		//GenerateChunk();
		//GenerateChunkMesh();
	}

	public void Load()
	{
		if (IsGenerated)
		{
			return;
		}
		IsGenerated = true;
		//heightMap = 
		//GenerateChunk();
		GenerateChunkMesh();
	}

	public void NewEmptyChunk()
	{
		for (int x = 0; x < ChunkSize; x++)
		{
			for (int y = 0; y < ChunkSize; y++)
			{
				for (int z = 0; z < ChunkSize; z++)
				{
					blocks[x, y, z] = BlockType.Air;
				}
			}
		}
	}

	private void GenerateHeightMap()
	{
		//OpenSimplexNoise terrainNoise = new OpenSimplexNoise(Seed);
		FastNoiseLite terrainNoiseF = new FastNoiseLite();
		terrainNoiseF.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
		terrainNoiseF.FractalOctaves = 4;
		terrainNoiseF.Frequency = 0.01f;

		for (int x = 0; x < ChunkSize; x++)
		{
			for (int z = 0; z < ChunkSize; z++)
			{
				int cX = x + (ChunkSize * ChunkLocation.X);
				int cZ = z + (ChunkSize * ChunkLocation.Z);
				// Generate height map
				//float heightValue = (float)terrainNoise.Evaluate(cX, cZ);
				float heightValue = terrainNoiseF.GetNoise2D(cX, cZ);
				//float heightValue = OpenSimplexNoise2.Noise2(seed, cX, cZ);
				int height = Mathf.RoundToInt(Mathf.Lerp(4, ChunkSize - 4, (heightValue + 1) / 2.0f));
				heightMap[x, z] = height;
			}
		}
	}

	private void GenerateChunk()
	{
		//OpenSimplexNoise caveNoise = new OpenSimplexNoise(Seed + 1);
		FastNoiseLite caveNoiseF = new FastNoiseLite();
		caveNoiseF.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
		caveNoiseF.FractalOctaves = 3;
		caveNoiseF.Frequency = 0.01f;
		//caveNoise.Octaves = 3;
		//caveNoise.Period = 25;
		//caveNoise.Persistence = 0.7f;

		for (int x = 0; x < ChunkSize; x++)
		{
			for (int z = 0; z < ChunkSize; z++)
			{
				for (int y = 0; y < ChunkSize; y++)
				{
					int cY = y + (ChunkSize * ChunkLocation.Y);
					int cX = x + (ChunkSize * ChunkLocation.X);
					int cZ = z + (ChunkSize * ChunkLocation.Z);
					int height = heightMap[x, z];

					if (cY > height)
					{
						blocks[x, y, z] = BlockType.Air;
					}
					else if (cY == height)
					{
						blocks[x, y, z] = BlockType.Grass;
					}
					else if (cY >= height - 3)
					{
						blocks[x, y, z] = BlockType.Dirt;
					}
					else
					{
						blocks[x, y, z] = BlockType.Stone;
					}

					// Generate caves
					//float caveValue = (float)caveNoise.Evaluate(cX, cY, cZ);
					float caveValue = caveNoiseF.GetNoise3D(cX, cY, cZ);
					//float caveValue = OpenSimplexNoise2.Noise3_Fallback(seed, cX, cY, cZ);
					if (caveValue > 0.5f)
					{
						blocks[x, y, z] = BlockType.Air;
					}
				}
			}
		}
	}

	private void GenerateChunkMesh()
	{
		Vector3[] vertices = new Vector3[TotalVertices];
		int[] indices = new int[TotalIndices];
		Vector2[] uvs = new Vector2[TotalUvs];
		int vertexIndex = 0;
		int indexIndex = 0;

		for (int x = 0; x < ChunkSize; x++)
		{
			for (int y = 0; y < ChunkSize; y++)
			{
				for (int z = 0; z < ChunkSize; z++)
				{
					if (blocks[x, y, z] != BlockType.Air)
					{
						bool[] checks = CheckNeighboringBlocks(x, y, z);
						AddBlockMesh(new Vector3(x, y, z), blocks[x, y, z], vertices, indices, uvs, ref vertexIndex, ref indexIndex,
							checks);
					}
				}
			}
		}

		ArrayMesh mesh = new ArrayMesh();
		Array arrays = new Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices;
		arrays[(int)Mesh.ArrayType.Index] = indices;
		arrays[(int)Mesh.ArrayType.TexUV] = uvs;
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

		StandardMaterial3D material = new StandardMaterial3D();
		material.TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;
		material.AnisotropyEnabled = false;
		material.AlbedoTexture = AtlasTexture;

		Mesh = mesh;
		Mesh.SurfaceSetMaterial(0, material);

		if (AddCollision)
		{
			// Add collision
			ConcavePolygonShape3D collisionShape = new ConcavePolygonShape3D();
			//collisionShape.SetFaces(mesh.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Index]);
			collisionShape.SetFaces(vertices);

			// Create a StaticBody3D and attach the collision shape
			StaticBody3D staticBody = new StaticBody3D();
			CollisionShape3D collisionShapeNode = new CollisionShape3D();
			collisionShapeNode.Shape = collisionShape;

			// Add the StaticBody3D and CollisionShape3D to the scene
			AddChild(staticBody);
			staticBody.AddChild(collisionShapeNode);
		}

		// add occluder
		//OccluderInstance3D occluderInstance = new OccluderInstance3D();
		//ArrayOccluder3D occluder = new ArrayOccluder3D();
		//occluder.SetArrays(vertices, indices);
		//occluderInstance.SetOccluder(occluder);
		//AddChild(occluderInstance);
	}

	private bool[] CheckNeighboringBlocks(int x, int y, int z)
	{
		// Check neighboring blocks to determine which faces to draw
		bool drawFront = (z == 0 || blocks[x, y, z - 1] == BlockType.Air);
		bool drawBack = (z == ChunkSize - 1 || blocks[x, y, z + 1] == BlockType.Air);
		bool drawLeft = (x == 0 || blocks[x - 1, y, z] == BlockType.Air);
		bool drawRight = (x == ChunkSize - 1 || blocks[x + 1, y, z] == BlockType.Air);
		bool drawTop = (y == ChunkSize - 1 || blocks[x, y + 1, z] == BlockType.Air);
		bool drawBottom = (y == 0 || blocks[x, y - 1, z] == BlockType.Air);

		return new bool[6] { drawFront, drawBack, drawLeft, drawRight, drawTop, drawBottom };
	}

	private void AddBlockMesh(Vector3 position, BlockType blockType, Vector3[] vertices, int[] indices, Vector2[] uvs, ref int vertexIndex, ref int indexIndex, bool[] drawChecks)
	{
		Vector2[] faceUvs = uvMappings[blockType];
		for (int i = 0; i < drawChecks.Length; i++) 
		{
			if (!drawChecks[i]) { continue; }
			AddFace(vertices, indices, uvs, position, vertexIndex, indexIndex, (Face)i, faceUvs);
			vertexIndex += 4;
			indexIndex += 6;
		}
	}

	private int[] faceIndices = new int[] { 0, 1, 2, 0, 2, 3 };
	private void AddFace(Vector3[] vertices, int[] indices, Vector2[] uvs, Vector3 position, int vertexIndex, int indexIndex, Face face, Vector2[] faceUvs)
	{
		Vector3[] faceVertices = GetFaceVertices(position, face);

		for (int i = 0; i < 4; i++)
		{
			vertices[vertexIndex + i] = faceVertices[i];
			uvs[vertexIndex + i] = faceUvs[i];
			//GD.Print($"Assigned UVs: {uvs[vertexIndex + i]} to vertex {i} of face {face}");
		}

		for (int i = 0; i < 6; i++)
		{
			indices[indexIndex + i] = faceIndices[i] + vertexIndex;
		}
	}

	private Vector3 face0 = new Vector3(0, 0, 0);
	private Vector3 face1 = new Vector3(0, 0, 1);
	private Vector3 face2 = new Vector3(0, 1, 0);
	private Vector3 face3 = new Vector3(0, 1, 1);
	private Vector3 face4 = new Vector3(1, 0, 0);
	private Vector3 face5 = new Vector3(1, 0, 1);
	private Vector3 face6 = new Vector3(1, 1, 0);
	private Vector3 face7 = new Vector3(1, 1, 1);

	// Get the face vertices with the calculated position taken into account
	private Vector3[] GetFaceVertices(Vector3 position, Face face)
	{
		Vector3[] faceVertices;
		switch (face)
		{
			case Face.Front: // Front
				faceVertices = new Vector3[4] { position + face0, position + face4, position + face6, position + face2 };
				break;
			case Face.Back: // Back
				faceVertices = new Vector3[4] { position + face5, position + face1, position + face3, position + face7 };
				break;
			case Face.Left: // Left
				faceVertices = new Vector3[4] {position + face1, position + face0, position + face2, position + face3 };
				break;
			case Face.Right: // Right
				faceVertices = new Vector3[4] {position + face4, position + face5, position + face7, position + face6 };
				break;
			case Face.Top: // Top
				faceVertices = new Vector3[4] {position + face2, position + face6, position + face7, position + face3 };
				break;
			case Face.Bottom: // Bottom
				faceVertices = new Vector3[4] {position + face1, position + face5, position + face4, position + face0 };
				break;
			default:
				faceVertices = new Vector3[4];
				break;
		}
		return faceVertices;
	}

	public static Texture2D CreateTextureAtlas(Dictionary<BlockType, Texture2D> blockTextures, out Dictionary<BlockType, Vector2[]> uvMappings)
	{
		int textureSize = 16; // Assuming each block texture is 16x16
		int atlasWidth = textureSize * blockTextures.Count;
		int atlasHeight = textureSize; // Assuming all textures fit in one row
		Image atlasImage = Image.CreateEmpty(atlasWidth, atlasHeight, false, Image.Format.Rgba8);
		atlasImage.Fill(new Color(1, 1, 1, 0)); // Fill with transparent color initially

		uvMappings = new Dictionary<BlockType, Vector2[]>();
		int xOffset = 0;

		foreach (var block in blockTextures)
		{
			Texture2D blockTexture = block.Value;
			Image blockImage = blockTexture.GetImage();
			if (blockImage == null)
			{
				GD.PrintErr($"Failed to get image from texture for block type: {block.Key}");
				continue;
			}
			//GD.Print($"Image format: {blockImage.GetFormat()}");
			//GD.Print($"Block {block.Key} - Image Size: {blockImage.GetWidth()}x{blockImage.GetHeight()}");

			blockImage = (Image)blockImage.Duplicate();  // Duplicate to avoid modifying the original
			blockImage.Convert(Image.Format.Rgba8);  // Ensure format is correct

			if (blockImage.GetWidth() != textureSize || blockImage.GetHeight() != textureSize)
			{
				GD.PrintErr($"Unexpected texture size for block type: {block.Key}. Expected {textureSize}x{textureSize}, got {blockImage.GetWidth()}x{blockImage.GetHeight()}");
				continue;
			}

			Rect2I sourceRect = new Rect2I(0, 0, textureSize, textureSize);
			Vector2I destPosition = new Vector2I(xOffset, 0);
			atlasImage.BlitRect(blockImage, sourceRect, destPosition);

			// Calculate UV mapping for this block
			float uMin = ((float)xOffset) / atlasWidth;
			float uMax = (float)(xOffset + textureSize) / atlasWidth;
			Vector2[] uvs = new Vector2[]
			{
				new Vector2(uMin, 0),  // Top-left
				new Vector2(uMax, 0),  // Top-right
				new Vector2(uMax, 1),  // Bottom-right
				new Vector2(uMin, 1)   // Bottom-left
			};
			uvMappings[block.Key] = uvs;
			//GD.Print($"Block Type: {block.Key}, UVs: {uvs[0]}, {uvs[1]}, {uvs[2]}, {uvs[3]}");
			xOffset += textureSize;
		}

		// Create texture from the atlas image
		ImageTexture atlasTexture = ImageTexture.CreateFromImage(atlasImage);
		//atlasImage.SavePng("res://atlas_debug.png");
		//GD.Print("Texture atlas saved as atlas_debug.png");
		return atlasTexture;
	}

	public int NextPowerOfTwo(int number)
	{
		if (number < 1)
		{
			GD.PrintErr($"Number must be greater than 0: {number}");
		}

		// Check if the number is already a power of 2
		if ((number & (number - 1)) == 0)
		{
			return number;
		}

		// If not, find the next power of 2
		int result = 1;
		while (result < number)
		{
			result <<= 1;
		}

		return result;
	}
}

public enum Face
{
	Front = 0,
	Back,
	Left,
	Right,
	Top,
	Bottom,
}
