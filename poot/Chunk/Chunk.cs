using Godot;
using Godot.Collections;

public partial class Chunk : MeshInstance3D
{
	[Export] public bool IsGenerated = false;

	[Export] public Vector3I ChunkLocation = Vector3I.Zero;

	private const int TotalVertices = ChunkData.BlockCount * 6 * 4;  // 6 faces, 4 vertices per face
	private const int TotalIndices = ChunkData.BlockCount * 6 * 6;   // 6 faces, 6 indices per face
	private const int TotalUvs = TotalVertices;

	public bool GenerateChunkMesh(ChunkData data, ThreadSafeDictionary<Vector3I, ChunkData> worldChunks)
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
					var b = data.Blocks[x, y, z];
					if (!IsTransparent(b))
					{
						// Check neighboring blocks and determine if all required chunks are present
						bool[] checks;
						if (!TryCheckNeighboringBlocks(x, y, z, data, worldChunks, out checks))
						{
							// If a required chunk is missing, return false to indicate failure
							return false;
						}
						AddBlockMesh(new Vector3(x, y, z), data.Blocks[x, y, z], vertices, indices, uvs, ref vertexIndex, ref indexIndex,
							checks);
					}
					else if (b == BlockType.Water)
					{
						// check if block above is air to see if we should add the top face.
						bool check;
						if (!TryCheckBlockAboveIsAir(x, y, z, data, worldChunks, out check))
						{
							return false;
						}
						AddBlockMesh(new Vector3(x, y, z), data.Blocks[x, y, z], vertices, indices, uvs, ref vertexIndex, ref indexIndex, new bool[6] { false, false, false, false, check, false });
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
		material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
		material.DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Always;

		Mesh = mesh;
		Mesh.SurfaceSetMaterial(0, material);
		data.IsDirty = false;

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

		return true;
	}

	private bool IsTransparent(BlockType blockType)
	{
		return blockType == BlockType.Air || blockType == BlockType.Water;
	}

	private Vector3I[] _directions = new Vector3I[]
	{
		new Vector3I(0, 0, -1), // Front
		new Vector3I(0, 0, 1),  // Back
		new Vector3I(-1, 0, 0), // Left
		new Vector3I(1, 0, 0),  // Right
		new Vector3I(0, 1, 0),  // Top
		new Vector3I(0, -1, 0)  // Bottom
	};

	// Check neighboring blocks and verify that all required chunks are available
	private bool TryCheckNeighboringBlocks(int x, int y, int z, ChunkData data, ThreadSafeDictionary<Vector3I, ChunkData> worldChunks, out bool[] drawFaces)
	{
		drawFaces = new bool[6];

		for (int i = 0; i < _directions.Length; i++)
		{
			Vector3I neighborPosition = new Vector3I(x, y, z) + _directions[i];

			if (IsInsideChunk(neighborPosition))
			{
				drawFaces[i] = IsTransparent(data.Blocks[neighborPosition.X, neighborPosition.Y, neighborPosition.Z]);
			}
			else
			{
				Vector3I neighborChunkPosition = data.Location + GetChunkOffset(neighborPosition);
				if (worldChunks.TryGetValue(neighborChunkPosition, out ChunkData neighborChunk))
				{
					Vector3I localNeighborPosition = GetLocalPosition(neighborPosition);
					drawFaces[i] = IsTransparent(neighborChunk.Blocks[localNeighborPosition.X, localNeighborPosition.Y, localNeighborPosition.Z]);
				}
				else
				{
					// Required chunk is missing, return false to indicate failure
					//GD.Print($"Missing terrain chunk {neighborChunkPosition}");
					return false;
				}
			}
		}

		return true;
	}

	private bool TryCheckBlockAboveIsAir(int x, int y, int z, ChunkData data, ThreadSafeDictionary<Vector3I, ChunkData> worldChunks, out bool drawTop)
	{
		drawTop = false;
		Vector3I neighborPosition = new Vector3I(x, y, z) + _directions[(int)Face.Top];
		if (IsInsideChunk(neighborPosition))
		{
			drawTop = data.Blocks[neighborPosition.X, neighborPosition.Y, neighborPosition.Z] == BlockType.Air;
		}
		else
		{
			Vector3I neighborChunkPosition = data.Location + GetChunkOffset(neighborPosition);
			if (worldChunks.TryGetValue(neighborChunkPosition, out ChunkData neighborChunk))
			{
				Vector3I localNeighborPosition = GetLocalPosition(neighborPosition);
				drawTop = neighborChunk.Blocks[localNeighborPosition.X, localNeighborPosition.Y, localNeighborPosition.Z] == BlockType.Air;
			}
			else
			{
				// Required chunk is missing, return false to indicate failure
				//GD.Print($"Missing terrain chunk {neighborChunkPosition}");
				return false;
			}
		}
		return true;
	}

	private bool IsInsideChunk(Vector3I position)
	{
		return position.X >= 0 && position.X < ChunkData.Size &&
			   position.Y >= 0 && position.Y < ChunkData.Size &&
			   position.Z >= 0 && position.Z < ChunkData.Size;
	}

	private Vector3I GetChunkOffset(Vector3I position)
	{
		return new Vector3I(
			position.X < 0 ? -1 : (position.X >= ChunkData.Size ? 1 : 0),
			position.Y < 0 ? -1 : (position.Y >= ChunkData.Size ? 1 : 0),
			position.Z < 0 ? -1 : (position.Z >= ChunkData.Size ? 1 : 0)
		);
	}

	private Vector3I GetLocalPosition(Vector3I position)
	{
		return new Vector3I(
			Mod(position.X, ChunkData.Size),
			Mod(position.Y, ChunkData.Size),
			Mod(position.Z, ChunkData.Size)
		);
	}

	private int Mod(int value, int modulus)
	{
		int result = value % modulus;
		return result < 0 ? result + modulus : result;
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
