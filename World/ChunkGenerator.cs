// ChunkGenerator.cs
using OpenTK.Mathematics;

public class ChunkMeshData
{
    public List<Vector3> Vertices;
    public List<Vector2> UVs;
    public List<uint> Indices;

    public ChunkMeshData()
    {
        Vertices = new List<Vector3>();
        UVs = new List<Vector2>();
        Indices = new List<uint>();
    }
}

public static class ChunkGenerator
{
    private const int SIZE = 16;
    private const int HEIGHT = 384;

    public static ChunkMeshData GenerateChunkMesh(Vector3 chunkPosition)
    {
        ChunkMeshData chunkMeshData = new ChunkMeshData();

        // Generate height map
        float[,] heightMap = GenerateHeightMap();

        // Generate blocks
        Block[,,] blocks = GenerateBlocks(heightMap);

        // Generate faces and build mesh data
        GenerateFaces(blocks, chunkMeshData);

        return chunkMeshData;
    }

    private static float[,] GenerateHeightMap()
    {
        float[,] heightMap = new float[SIZE, SIZE];

        SimplexNoise.Noise.Seed = 123456;
        for (int x = 0; x < SIZE; x++)
        {
            for (int z = 0; z < SIZE; z++)
            {
                heightMap[x, z] = SimplexNoise.Noise.CalcPixel2D(x, z, 0.01f);
            }
        }

        return heightMap;
    }

    private static Block[,,] GenerateBlocks(float[,] heightMap)
    {
        Block[,,] blocks = new Block[SIZE, HEIGHT, SIZE];

        for (int x = 0; x < SIZE; x++)
        {
            for (int z = 0; z < SIZE; z++)
            {
                int columnHeight = (int)(heightMap[x, z] / 10);
                for (int y = 0; y < HEIGHT; y++)
                {
                    BlockType type = BlockType.AIR;
                    if (y < columnHeight - 1)
                    {
                        type = BlockType.DIRT;
                    }
                    if (y == columnHeight - 1)
                    {
                        type = BlockType.GRASS;
                    }

                    blocks[x, y, z] = new Block(new Vector3(x, y, z), type);
                }
            }
        }

        return blocks;
    }

    private static void GenerateFaces(Block[,,] blocks, ChunkMeshData chunkMeshData)
    {
        uint totalIndexCount = 0;

        for (int x = 0; x < SIZE; x++)
        {
            for (int z = 0; z < SIZE; z++)
            {
                for (int y = 0; y < HEIGHT; y++)
                {
                    Block block = blocks[x, y, z];
                    if (block.Type != BlockType.AIR)
                    {
                        // Determine which faces are visible
                        // For each face, if the adjacent block is air or out of bounds, add the face

                        // Left face
                        if (x == 0 || blocks[x - 1, y, z].Type == BlockType.AIR)
                        {
                            AddFace(block, Faces.LEFT, chunkMeshData, ref totalIndexCount);
                        }

                        // Right face
                        if (x == SIZE - 1 || blocks[x + 1, y, z].Type == BlockType.AIR)
                        {
                            AddFace(block, Faces.RIGHT, chunkMeshData, ref totalIndexCount);
                        }

                        // Bottom face
                        if (y == 0 || blocks[x, y - 1, z].Type == BlockType.AIR)
                        {
                            AddFace(block, Faces.BOTTOM, chunkMeshData, ref totalIndexCount);
                        }

                        // Top face
                        if (y == HEIGHT - 1 || blocks[x, y + 1, z].Type == BlockType.AIR)
                        {
                            AddFace(block, Faces.TOP, chunkMeshData, ref totalIndexCount);
                        }

                        // Front face
                        if (z == SIZE - 1 || blocks[x, y, z + 1].Type == BlockType.AIR)
                        {
                            AddFace(block, Faces.FRONT, chunkMeshData, ref totalIndexCount);
                        }

                        // Back face
                        if (z == 0 || blocks[x, y, z - 1].Type == BlockType.AIR)
                        {
                            AddFace(block, Faces.BACK, chunkMeshData, ref totalIndexCount);
                        }
                    }
                }
            }
        }
    }

    private static void AddFace(Block block, Faces face, ChunkMeshData chunkMeshData, ref uint totalIndexCount)
    {
        var faceData = block.GetFace(face);

        chunkMeshData.Vertices.AddRange(faceData.Vertices);
        chunkMeshData.UVs.AddRange(faceData.TextureCoordinates);

        // Add indices
        chunkMeshData.Indices.Add(0 + totalIndexCount);
        chunkMeshData.Indices.Add(1 + totalIndexCount);
        chunkMeshData.Indices.Add(2 + totalIndexCount);
        chunkMeshData.Indices.Add(2 + totalIndexCount);
        chunkMeshData.Indices.Add(3 + totalIndexCount);
        chunkMeshData.Indices.Add(0 + totalIndexCount);

        totalIndexCount += 4;
    }
}
