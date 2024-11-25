using System.Collections.Concurrent;
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
            int chunkSize = Chunk.SIZE;
            int chunkHeight = Chunk.HEIGHT;

            // Thread-local storage for mesh data
            ConcurrentBag<FaceData> faceDataBag = [];

            Parallel.For(
                0,
                chunkSize,
                x =>
                {
                    List<FaceData> localFaceData = [];

                    for (int z = 0; z < chunkSize; z++)
                    {
                        for (int y = 0; y < chunkHeight; y++)
                        {
                            if (blocks[x, y, z] != BlockType.AIR)
                            {
                                AddVisibleFaces(x, y, z, blocks, localFaceData);
                            }
                        }
                    }

                    // Add local data to the concurrent bag
                    foreach (var faceData in localFaceData)
                    {
                        faceDataBag.Add(faceData);
                    }
                }
            );

            // After parallel loop, merge all face data
            uint totalIndexCount = 0;
            foreach (var faceData in faceDataBag)
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

        private static void AddVisibleFaces(
            int x,
            int y,
            int z,
            BlockType[,,] blocks,
            List<FaceData> localFaceData
        )
        {
            Vector3 blockPosition = new(x, y, z);
            Block block = new(blockPosition, blocks[x, y, z]);

            // Check each face to see if it should be added
            if (x == 0 || blocks[x - 1, y, z] == BlockType.AIR)
                localFaceData.Add(block.GetFace(Faces.LEFT));

            if (x == Chunk.SIZE - 1 || blocks[x + 1, y, z] == BlockType.AIR)
                localFaceData.Add(block.GetFace(Faces.RIGHT));

            if (y == 0 || blocks[x, y - 1, z] == BlockType.AIR)
                localFaceData.Add(block.GetFace(Faces.BOTTOM));

            if (y == Chunk.HEIGHT - 1 || blocks[x, y + 1, z] == BlockType.AIR)
                localFaceData.Add(block.GetFace(Faces.TOP));

            if (z == 0 || blocks[x, y, z - 1] == BlockType.AIR)
                localFaceData.Add(block.GetFace(Faces.BACK));

            if (z == Chunk.SIZE - 1 || blocks[x, y, z + 1] == BlockType.AIR)
                localFaceData.Add(block.GetFace(Faces.FRONT));
        }
    }
}
