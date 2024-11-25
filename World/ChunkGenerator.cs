using OpenTK.Mathematics;
using VoxelGame.World.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

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
            int size = Chunk.SIZE;
            float[,] heightMap = new float[size, size];
            SimplexNoise.Noise.Seed = 123456;

            Parallel.For(
                0,
                size,
                x =>
                {
                    for (int z = 0; z < size; z++)
                    {
                        heightMap[x, z] = SimplexNoise.Noise.CalcPixel2D(x, z, 0.01f);
                    }
                }
            );

            return heightMap;
        }

        private static BlockType[,,] GenerateBlocks(float[,] heightMap)
        {
            int size = Chunk.SIZE;
            int heigh = Chunk.HEIGHT;
            BlockType[,,] blocks = new BlockType[size, heigh, size];

            Parallel.For(
                0,
                size,
                x =>
                {
                    for (int z = 0; z < size; z++)
                    {
                        int columnHeight = (int)(heightMap[x, z] / 10);
                        for (int y = 0; y < heigh; y++)
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
            int size = Chunk.SIZE;
            int height = Chunk.HEIGHT;
            object lockObj = new();

            Parallel.For(
                0,
                size,
                () => new PerThreadMeshData(),
                (x, state, localMeshData) =>
                {
                    for (int z = 0; z < size; z++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            if (blocks[x, y, z] != BlockType.AIR)
                            {
                                AddVisibleFaces(x, y, z, blocks, localMeshData);
                            }
                        }
                    }
                    return localMeshData;
                },
                localMeshData =>
                {
                    lock (lockObj)
                    {
                        uint indexOffset = (uint)chunkMeshData.Vertices.Count;
                        chunkMeshData.Vertices.AddRange(localMeshData.Vertices);
                        chunkMeshData.UVs.AddRange(localMeshData.UVs);

                        // Adjust indices
                        foreach (var index in localMeshData.Indices)
                        {
                            chunkMeshData.Indices.Add(index + indexOffset);
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
            PerThreadMeshData localMeshData
        )
        {
            BlockType blockType = blocks[x, y, z];

            // Check each face to see if it should be added
            if (x == 0 || blocks[x - 1, y, z] == BlockType.AIR)
                AddFace(x, y, z, Faces.LEFT, blockType, localMeshData);

            if (x == Chunk.SIZE - 1 || blocks[x + 1, y, z] == BlockType.AIR)
                AddFace(x, y, z, Faces.RIGHT, blockType, localMeshData);

            if (y == 0 || blocks[x, y - 1, z] == BlockType.AIR)
                AddFace(x, y, z, Faces.BOTTOM, blockType, localMeshData);

            if (y == Chunk.HEIGHT - 1 || blocks[x, y + 1, z] == BlockType.AIR)
                AddFace(x, y, z, Faces.TOP, blockType, localMeshData);

            if (z == 0 || blocks[x, y, z - 1] == BlockType.AIR)
                AddFace(x, y, z, Faces.BACK, blockType, localMeshData);

            if (z == Chunk.SIZE - 1 || blocks[x, y, z + 1] == BlockType.AIR)
                AddFace(x, y, z, Faces.FRONT, blockType, localMeshData);
        }

        private static void AddFace(
            int x,
            int y,
            int z,
            Faces face,
            BlockType blockType,
            PerThreadMeshData localMeshData
        )
        {
            // Get the raw vertex data for the face
            List<Vector3> faceVertices = FaceDataRaw.rawVertexData[face];

            // Transform the vertices by adding the block position
            foreach (var vert in faceVertices)
            {
                localMeshData.Vertices.Add(vert + new Vector3(x, y, z));
            }

            // Get the UV coordinates for the block type and face
            List<Vector2> uvCoords = TextureData.GetUVs(blockType, face);
            localMeshData.UVs.AddRange(uvCoords);

            uint baseIndex = localMeshData.TotalIndexCount;

            localMeshData.Indices.Add(0 + baseIndex);
            localMeshData.Indices.Add(1 + baseIndex);
            localMeshData.Indices.Add(2 + baseIndex);
            localMeshData.Indices.Add(2 + baseIndex);
            localMeshData.Indices.Add(3 + baseIndex);
            localMeshData.Indices.Add(0 + baseIndex);

            localMeshData.TotalIndexCount += 4;
        }
    }

    // Class to hold per-thread mesh data
    class PerThreadMeshData
    {
        public List<Vector3> Vertices = [];
        public List<Vector2> UVs = [];
        public List<uint> Indices = [];
        public uint TotalIndexCount = 0;
    }
}
