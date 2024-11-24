using OpenTK.Mathematics;
using VoxelGame.World.Data;

namespace VoxelGame.World
{
    public static class ChunkGenerator
    {
        public static ChunkMeshData GenerateChunkMesh(Vector3 chunkPosition)
        {
            ChunkMeshData chunkMeshData = new();

            float[,] heightMap = GenerateHeightMap();
            Block[,,] blocks = GenerateBlocks(heightMap);
            GenerateFaces(blocks, chunkMeshData);

            return chunkMeshData;
        }

        private static float[,] GenerateHeightMap()
        {
            float[,] heightMap = new float[Chunk.SIZE, Chunk.SIZE];
            SimplexNoise.Noise.Seed = 123456;
            for (int x = 0; x < Chunk.SIZE; x++)
            {
                for (int z = 0; z < Chunk.SIZE; z++)
                {
                    heightMap[x, z] = SimplexNoise.Noise.CalcPixel2D(x, z, 0.01f);
                }
            }
            return heightMap;
        }

        private static Block[,,] GenerateBlocks(float[,] heightMap)
        {
            Block[,,] blocks = new Block[Chunk.SIZE, Chunk.HEIGHT, Chunk.SIZE];
            for (int x = 0; x < Chunk.SIZE; x++)
            {
                for (int z = 0; z < Chunk.SIZE; z++)
                {
                    int columnHeight = (int)(heightMap[x, z] / 10);
                    for (int y = 0; y < Chunk.HEIGHT; y++)
                    {
                        BlockType type =
                            y < columnHeight - 1 ? BlockType.DIRT
                            : y == columnHeight - 1 ? BlockType.GRASS
                            : BlockType.AIR;
                        blocks[x, y, z] = new Block(new Vector3(x, y, z), type);
                    }
                }
            }
            return blocks;
        }

        private static void GenerateFaces(Block[,,] blocks, ChunkMeshData chunkMeshData)
        {
            uint totalIndexCount = 0;
            for (int x = 0; x < Chunk.SIZE; x++)
            {
                for (int z = 0; z < Chunk.SIZE; z++)
                {
                    for (int y = 0; y < Chunk.HEIGHT; y++)
                    {
                        Block block = blocks[x, y, z];
                        if (block.Type != BlockType.AIR)
                        {
                            AddVisibleFaces(
                                block,
                                blocks,
                                chunkMeshData,
                                ref totalIndexCount,
                                x,
                                y,
                                z
                            );
                        }
                    }
                }
            }
        }

        private static void AddVisibleFaces(
            Block block,
            Block[,,] blocks,
            ChunkMeshData chunkMeshData,
            ref uint totalIndexCount,
            int x,
            int y,
            int z
        )
        {
            if (x == 0 || blocks[x - 1, y, z].Type == BlockType.AIR)
                AddFace(block, Faces.LEFT, chunkMeshData, ref totalIndexCount);

            // Right Face
            if (x == Chunk.SIZE - 1 || blocks[x + 1, y, z].Type == BlockType.AIR)
                AddFace(block, Faces.RIGHT, chunkMeshData, ref totalIndexCount);

            // Bottom Face
            if (y == 0 || blocks[x, y - 1, z].Type == BlockType.AIR)
                AddFace(block, Faces.BOTTOM, chunkMeshData, ref totalIndexCount);

            // Top Face
            if (y == Chunk.HEIGHT - 1 || blocks[x, y + 1, z].Type == BlockType.AIR)
                AddFace(block, Faces.TOP, chunkMeshData, ref totalIndexCount);

            // Back Face
            if (z == 0 || blocks[x, y, z - 1].Type == BlockType.AIR)
                AddFace(block, Faces.BACK, chunkMeshData, ref totalIndexCount);

            // Front Face
            if (z == Chunk.SIZE - 1 || blocks[x, y, z + 1].Type == BlockType.AIR)
                AddFace(block, Faces.FRONT, chunkMeshData, ref totalIndexCount);
        }

        private static void AddFace(
            Block block,
            Faces face,
            ChunkMeshData chunkMeshData,
            ref uint totalIndexCount
        )
        {
            var faceData = block.GetFace(face);
            chunkMeshData.Vertices.AddRange(faceData.Vertices);
            chunkMeshData.UVs.AddRange(faceData.TextureCoordinates);
            chunkMeshData.Indices.Add(0 + totalIndexCount);
            chunkMeshData.Indices.Add(1 + totalIndexCount);
            chunkMeshData.Indices.Add(2 + totalIndexCount);
            chunkMeshData.Indices.Add(2 + totalIndexCount);
            chunkMeshData.Indices.Add(3 + totalIndexCount);
            chunkMeshData.Indices.Add(0 + totalIndexCount);
            totalIndexCount += 4;
        }
    }
}
