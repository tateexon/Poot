using Godot;
using Godot.Collections;

public partial class Chunk : MeshInstance3D
{
	[Export] public bool IsGenerated = false;

	[Export] public Vector3I ChunkLocation = Vector3I.Zero;

	private const int TotalVertices = ChunkData.BlockCount * 6 * 4;  // 6 faces, 4 vertices per face
	private const int TotalIndices = ChunkData.BlockCount * 6 * 6;   // 6 faces, 6 indices per face
	private const int TotalUvs = TotalVertices;

	public void GenerateChunkMesh(ChunkData data)
	{
		Vector3[] vertices = new Vector3[TotalVertices];
		int[] indices = new int[TotalIndices];
		Vector2[] uvs = new Vector2[TotalUvs];
		int vertexIndex = 0;
		int indexIndex = 0;

		for (int x = 0; x < ChunkData.Size; x++)
		{
			for (int y = 0; y < ChunkData.Size; y++)
			{
				for (int z = 0; z < ChunkData.Size; z++)
				{
					if (data.Blocks[x, y, z] != BlockType.Air)
					{
						bool[] checks = CheckNeighboringBlocks(x, y, z, data);
						AddBlockMesh(new Vector3(x, y, z), data.Blocks[x, y, z], vertices, indices, uvs, ref vertexIndex, ref indexIndex,
							checks);
					}
				}
			}
		}

		//GD.Print($"Vertice count {vertices.Length}");

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
		material.AlbedoTexture = BlockAtlasTexture.AtlasTexture;

		Mesh = mesh;
		Mesh.SurfaceSetMaterial(0, material);

		//// Add collision
		//ConcavePolygonShape3D collisionShape = new ConcavePolygonShape3D();
		////collisionShape.SetFaces(mesh.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Index]);
		//collisionShape.SetFaces(vertices);

		//// Create a StaticBody3D and attach the collision shape
		//StaticBody3D staticBody = new StaticBody3D();
		//CollisionShape3D collisionShapeNode = new CollisionShape3D();
		//collisionShapeNode.Shape = collisionShape;

		//// Add the StaticBody3D and CollisionShape3D to the scene
		//AddChild(staticBody);
		//staticBody.AddChild(collisionShapeNode);

		//// Add occlusion culling
		//OccluderInstance3D occluderInstance = new OccluderInstance3D();
		//ArrayOccluder3D occluder = new ArrayOccluder3D();
		//occluder.SetArrays(vertices, indices);
		//occluderInstance.SetOccluder(occluder);
		//AddChild(occluderInstance);
	}

	// Check neighboring blocks to determine which faces to draw
	private bool[] CheckNeighboringBlocks(int x, int y, int z, ChunkData data)
	{
		bool drawFront = (z == 0 || data.Blocks[x, y, z - 1] == BlockType.Air);
		bool drawBack = (z == ChunkData.Size - 1 || data.Blocks[x, y, z + 1] == BlockType.Air);
		bool drawLeft = (x == 0 || data.Blocks[x - 1, y, z] == BlockType.Air);
		bool drawRight = (x == ChunkData.Size - 1 || data.Blocks[x + 1, y, z] == BlockType.Air);
		bool drawTop = (y == ChunkData.Size - 1 || data.Blocks[x, y + 1, z] == BlockType.Air);
		bool drawBottom = (y == 0 || data.Blocks[x, y - 1, z] == BlockType.Air);

		return new bool[6] { drawFront, drawBack, drawLeft, drawRight, drawTop, drawBottom };
	}

	private void AddBlockMesh(Vector3 position, BlockType blockType, Vector3[] vertices, int[] indices, Vector2[] uvs, ref int vertexIndex, ref int indexIndex, bool[] drawChecks)
	{
		Vector2[] faceUvs = BlockAtlasTexture.UvMappings[blockType];
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
				faceVertices = new Vector3[4] { position + face1, position + face0, position + face2, position + face3 };
				break;
			case Face.Right: // Right
				faceVertices = new Vector3[4] { position + face4, position + face5, position + face7, position + face6 };
				break;
			case Face.Top: // Top
				faceVertices = new Vector3[4] { position + face2, position + face6, position + face7, position + face3 };
				break;
			case Face.Bottom: // Bottom
				faceVertices = new Vector3[4] { position + face1, position + face5, position + face4, position + face0 };
				break;
			default:
				faceVertices = new Vector3[4];
				break;
		}
		return faceVertices;
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
