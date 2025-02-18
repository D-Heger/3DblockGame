using System.Collections.Generic;
using System.Threading.Tasks;
using OpenTK.Mathematics;
using VoxelGame.EntityComponentSystem.Systems;
using VoxelGame.World.Data;

namespace VoxelGame.World
{
    public static class ChunkGenerator
    {
        private static readonly object _noiseSeedLock = new object();
        private static int? _noiseSeed = null;
        private static readonly object _neighborChunkLock = new object();

        private static void EnsureNoiseSeedInitialized()
        {
            if (_noiseSeed == null)
            {
                lock (_noiseSeedLock)
                {
                    if (_noiseSeed == null)
                    {
                        _noiseSeed = new Random().Next();
                        SimplexNoise.Noise.Seed = _noiseSeed.Value;
                    }
                }
            }
        }

        public static ChunkMeshData GenerateChunkMesh(
            ChunkPosition chunkPosition,
            WorldSystem worldSystem,
            out ChunkData chunkData
        )
        {
            ChunkMeshData chunkMeshData = new();

            float[,] heightMap = GenerateHeightMap((int)chunkPosition.X, (int)chunkPosition.Z);
            BlockType[,,] blocks = GenerateBlocks(heightMap);
            GenerateFaces(blocks, chunkPosition, chunkMeshData, worldSystem);

            chunkData = new ChunkData(blocks);
            return chunkMeshData;
        }

        public static ChunkData GenerateChunkData(ChunkPosition chunkPosition)
        {
            float[,] heightMap = GenerateHeightMap(chunkPosition.X, chunkPosition.Z);
            BlockType[,,] blocks = GenerateBlocks(heightMap);
            return new ChunkData(blocks);
        }

        private static float[,] GenerateHeightMap(int offsetX, int offsetZ)
        {
            int size = Chunk.SIZE;
            float[,] heightMap = new float[size, size];

            // Ensure noise seed is initialized
            EnsureNoiseSeedInitialized();

            Parallel.For(
                0,
                size,
                x =>
                {
                    for (int z = 0; z < size; z++)
                    {
                        float noiseValue = SimplexNoise.Noise.CalcPixel2D(
                            x + offsetX,
                            z + offsetZ,
                            0.1f
                        );
                        heightMap[x, z] = noiseValue;
                    }
                }
            );

            return heightMap;
        }

        private static float[,] SmoothHeightMap(float[,] heightMap)
        {
            int size = heightMap.GetLength(0);
            float[,] tempMap = new float[size, size];
            float[,] smoothedHeightMap = new float[size, size];

            // Horizontal blur pass
            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    float totalHeight = 0f;
                    int count = 0;

                    for (int dz = -1; dz <= 1; dz++)
                    {
                        int nz = z + dz;
                        if (nz >= 0 && nz < size)
                        {
                            totalHeight += heightMap[x, nz];
                            count++;
                        }
                    }

                    tempMap[x, z] = totalHeight / count;
                }
            }

            // Vertical blur pass
            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    float totalHeight = 0f;
                    int count = 0;

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx;
                        if (nx >= 0 && nx < size)
                        {
                            totalHeight += tempMap[nx, z];
                            count++;
                        }
                    }

                    smoothedHeightMap[x, z] = totalHeight / count;
                }
            }

            return smoothedHeightMap;
        }

        private static BlockType[,,] GenerateBlocks(float[,] heightMap)
        {
            heightMap = SmoothHeightMap(heightMap);

            int size = Chunk.SIZE;
            int height = Chunk.HEIGHT;
            int minHeight = 5;
            BlockType[,,] blocks = new BlockType[size, height, size];

            Parallel.For(
                0,
                size,
                x =>
                {
                    for (int z = 0; z < size; z++)
                    {
                        int columnHeight = minHeight + (int)(heightMap[x, z] / 32);
                        for (int y = 0; y < height; y++)
                        {
                            if (y <= 0)
                            {
                                blocks[x, y, z] = BlockType.BEDROCK;
                            }
                            else if (y <= columnHeight)
                            {
                                blocks[x, y, z] = BlockType.STONE;
                            }
                            else if (y == columnHeight + 1)
                            {
                                blocks[x, y, z] = BlockType.SAND;
                            }
                            else if (y == columnHeight + 2)
                            {
                                blocks[x, y, z] = BlockType.DIRT;
                            }
                            else if (y == columnHeight + 3)
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

        private static void GenerateFaces(
            BlockType[,,] blocks,
            ChunkPosition chunkPosition,
            ChunkMeshData chunkMeshData,
            WorldSystem worldSystem
        )
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
                                AddVisibleFaces(
                                    x,
                                    y,
                                    z,
                                    blocks,
                                    chunkPosition,
                                    localMeshData,
                                    worldSystem
                                );
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
                        chunkMeshData.Normals.AddRange(localMeshData.Normals);

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
            ChunkPosition chunkPosition,
            PerThreadMeshData localMeshData,
            WorldSystem worldSystem
        )
        {
            BlockType blockType = blocks[x, y, z];

            if (IsFaceVisible(x, y, z + 1, blocks, chunkPosition, worldSystem))
            {
                AddFace(x, y, z, Faces.FRONT, blockType, localMeshData);
            }

            if (IsFaceVisible(x, y, z - 1, blocks, chunkPosition, worldSystem))
            {
                AddFace(x, y, z, Faces.BACK, blockType, localMeshData);
            }

            if (IsFaceVisible(x + 1, y, z, blocks, chunkPosition, worldSystem))
            {
                AddFace(x, y, z, Faces.RIGHT, blockType, localMeshData);
            }

            if (IsFaceVisible(x - 1, y, z, blocks, chunkPosition, worldSystem))
            {
                AddFace(x, y, z, Faces.LEFT, blockType, localMeshData);
            }

            if (IsFaceVisible(x, y + 1, z, blocks, chunkPosition, worldSystem))
            {
                AddFace(x, y, z, Faces.TOP, blockType, localMeshData);
            }

            if (IsFaceVisible(x, y - 1, z, blocks, chunkPosition, worldSystem))
            {
                AddFace(x, y, z, Faces.BOTTOM, blockType, localMeshData);
            }
        }

        private static bool IsFaceVisible(
            int x,
            int y,
            int z,
            BlockType[,,] blocks,
            ChunkPosition chunkPosition,
            WorldSystem worldSystem
        )
        {
            int size = Chunk.SIZE;
            int height = Chunk.HEIGHT;

            // Check if the coordinates are within the current chunk bounds
            if (x >= 0 && x < size && y >= 0 && y < height && z >= 0 && z < size)
            {
                // If the block is air, the face is visible
                return blocks[x, y, z] == BlockType.AIR;
            }
            else
            {
                // Determine the neighboring chunk's position
                ChunkPosition neighborChunkPosition = chunkPosition;

                int neighborX = x;
                int neighborY = y;
                int neighborZ = z;

                // Adjust the neighboring chunk position and local coordinates
                if (x < 0)
                {
                    neighborChunkPosition = new ChunkPosition(
                        neighborChunkPosition.X - size,
                        neighborChunkPosition.Y,
                        neighborChunkPosition.Z
                    );
                    neighborX = x + size; // Wrap to the other side of the neighboring chunk
                }
                else if (x >= size)
                {
                    neighborChunkPosition = new ChunkPosition(
                        neighborChunkPosition.X + size,
                        neighborChunkPosition.Y,
                        neighborChunkPosition.Z
                    );
                    neighborX = x - size; // Wrap to the other side of the neighboring chunk
                }

                if (y < 0 || y >= height)
                {
                    // If y is out of bounds, there is no neighboring chunk vertically.
                    // Assume the face is visible, as there is no block above or below the chunk.
                    return true;
                }

                if (z < 0)
                {
                    neighborChunkPosition = new ChunkPosition(
                        neighborChunkPosition.X,
                        neighborChunkPosition.Y,
                        neighborChunkPosition.Z - size
                    );
                    neighborZ = z + size; // Wrap to the other side of the neighboring chunk
                }
                else if (z >= size)
                {
                    neighborChunkPosition = new ChunkPosition(
                        neighborChunkPosition.X,
                        neighborChunkPosition.Y,
                        neighborChunkPosition.Z + size
                    );
                    neighborZ = z - size; // Wrap to the other side of the neighboring chunk
                }

                // Use a lock when checking and accessing neighboring chunks
                lock (_neighborChunkLock)
                {
                    // Check if the neighboring chunk exists in the world
                    if (worldSystem.ChunkExists(neighborChunkPosition))
                    {
                        // Get the neighboring chunk's data
                        ChunkData? neighborChunk = worldSystem.GetChunk(neighborChunkPosition);

                        // If the chunk is null or the block is air, the face is visible
                        if (neighborChunk == null)
                            return true;

                        // Check if the corresponding block in the neighboring chunk is air
                        return neighborChunk.Blocks[neighborX, neighborY, neighborZ]
                            == BlockType.AIR;
                    }
                }

                // If the neighboring chunk does not exist, assume the face is visible
                return true;
            }
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

            // Get the normal for this face
            Vector3 normal = FaceDataRaw.faceNormals[face];

            // Transform the vertices by adding the block position
            foreach (var vert in faceVertices)
            {
                localMeshData.Vertices.Add(vert + new Vector3(x, y, z));
                localMeshData.Normals.Add(normal); // Add the same normal for each vertex
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
        public List<Vector3> Normals = []; // Add normals list
        public List<uint> Indices = [];
        public uint TotalIndexCount = 0;
    }
}
