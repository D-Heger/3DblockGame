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
            BlockType[,,] blocks = GenerateBlocks(heightMap);
            GenerateFaces(blocks, chunkMeshData);

            return chunkMeshData;
        }

        private static float[,] GenerateHeightMap()
        {
            float[,] heightMap = new float[Chunk.SIZE, Chunk.SIZE];
            SimplexNoise.Noise.Seed = 123456;

            Parallel.For(
                0,
                Chunk.SIZE,
                x =>
                {
                    for (int z = 0; z < Chunk.SIZE; z++)
                    {
                        heightMap[x, z] = SimplexNoise.Noise.CalcPixel2D(x, z, 0.01f);
                    }
                }
            );

            return heightMap;
        }

        private static BlockType[,,] GenerateBlocks(float[,] heightMap)
        {
            BlockType[,,] blocks = new BlockType[Chunk.SIZE, Chunk.HEIGHT, Chunk.SIZE];

            Parallel.For(
                0,
                Chunk.SIZE,
                x =>
                {
                    for (int z = 0; z < Chunk.SIZE; z++)
                    {
                        int columnHeight = (int)(heightMap[x, z] / 10);
                        for (int y = 0; y < Chunk.HEIGHT; y++)
                        {
                            if (y < columnHeight - 1)
                            {
                                blocks[x, y, z] = BlockType.DIRT;
                            }
                            else if (y == columnHeight - 1)
                            {
                                blocks[x, y, z] = BlockType.GRASS;
                            }
                            else
                            {
                                blocks[x, y, z] = BlockType.AIR;
                            }
                        }
                    }
                }
            );

            return blocks;
        }

        private static void GenerateFaces(BlockType[,,] blocks, ChunkMeshData chunkMeshData)
        {
            uint totalIndexCount = 0;

            Parallel.For(
                0,
                Chunk.SIZE,
                x =>
                {
                    for (int z = 0; z < Chunk.SIZE; z++)
                    {
                        for (int y = 0; y < Chunk.HEIGHT; y++)
                        {
                            if (blocks[x, y, z] != BlockType.AIR)
                            {
                                AddVisibleFaces(
                                    x,
                                    y,
                                    z,
                                    blocks,
                                    chunkMeshData,
                                    ref totalIndexCount
                                );
                            }
                        }
                    }
                }
            );
        }

        private static void AddVisibleFaces(
            int x,
            int y,
            int z,
            BlockType[,,] blocks,
            ChunkMeshData chunkMeshData,
            ref uint totalIndexCount
        )
        {
            Vector3 blockPosition = new Vector3(x, y, z);

            // Check each face to see if it should be added
            if (x == 0 || blocks[x - 1, y, z] == BlockType.AIR)
                AddFace(
                    x,
                    y,
                    z,
                    Faces.LEFT,
                    new Block(blockPosition, blocks[x, y, z]),
                    chunkMeshData,
                    ref totalIndexCount
                );

            if (x == Chunk.SIZE - 1 || blocks[x + 1, y, z] == BlockType.AIR)
                AddFace(
                    x,
                    y,
                    z,
                    Faces.RIGHT,
                    new Block(blockPosition, blocks[x, y, z]),
                    chunkMeshData,
                    ref totalIndexCount
                );

            if (y == 0 || blocks[x, y - 1, z] == BlockType.AIR)
                AddFace(
                    x,
                    y,
                    z,
                    Faces.BOTTOM,
                    new Block(blockPosition, blocks[x, y, z]),
                    chunkMeshData,
                    ref totalIndexCount
                );

            if (y == Chunk.HEIGHT - 1 || blocks[x, y + 1, z] == BlockType.AIR)
                AddFace(
                    x,
                    y,
                    z,
                    Faces.TOP,
                    new Block(blockPosition, blocks[x, y, z]),
                    chunkMeshData,
                    ref totalIndexCount
                );

            if (z == 0 || blocks[x, y, z - 1] == BlockType.AIR)
                AddFace(
                    x,
                    y,
                    z,
                    Faces.BACK,
                    new Block(blockPosition, blocks[x, y, z]),
                    chunkMeshData,
                    ref totalIndexCount
                );

            if (z == Chunk.SIZE - 1 || blocks[x, y, z + 1] == BlockType.AIR)
                AddFace(
                    x,
                    y,
                    z,
                    Faces.FRONT,
                    new Block(blockPosition, blocks[x, y, z]),
                    chunkMeshData,
                    ref totalIndexCount
                );
        }

        private static void AddFace(
            int x,
            int y,
            int z,
            Faces face,
            Block block,
            ChunkMeshData chunkMeshData,
            ref uint totalIndexCount
        )
        {
            var faceData = block.GetFace(face);

            lock (chunkMeshData)
            {
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
}
