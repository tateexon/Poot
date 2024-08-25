using Godot;
using Godot.Collections;
using System;

public class BlockAtlasTexture
{
	public static Texture2D AtlasTexture;
	public static Dictionary<BlockType, Vector2[]> UvMappings;

	public static Dictionary<BlockType, Texture2D> BlockTextures = new Dictionary<BlockType, Texture2D>
	{
		{ BlockType.Grass, (Texture2D)GD.Load("res://textures/grass.png") },
		{ BlockType.Dirt, (Texture2D)GD.Load("res://textures/dirt.png") },
		{ BlockType.Stone, (Texture2D)GD.Load("res://textures/stone.png") },
	};
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
}
