using OpenTK.Mathematics;
using VoxelGame.EntityComponentSystem.Systems;
using VoxelGame.World.Data;

namespace VoxelGame.World;

public static class ChunkGenerator
{
    private static readonly Lock _noiseSeedLock = new();
    private static int? _noiseSeed;

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
        BlockType[] blocks = GenerateBlocks(heightMap);
        int size = Chunk.SIZE;
        int height = Chunk.HEIGHT;
        chunkData = new ChunkData(blocks, size, height, size);
        GenerateFaces(chunkData, chunkPosition, chunkMeshData, worldSystem);

        chunkMeshData.Vertices.TrimExcess();
        chunkMeshData.UVs.TrimExcess();
        chunkMeshData.Normals.TrimExcess();
        chunkMeshData.Indices.TrimExcess();

        return chunkMeshData;
    }

    public static ChunkData GenerateChunkData(ChunkPosition chunkPosition)
    {
        float[,] heightMap = GenerateHeightMap(chunkPosition.X, chunkPosition.Z);
        BlockType[] blocks = GenerateBlocks(heightMap);
        int size = Chunk.SIZE;
        int height = Chunk.HEIGHT;
        return new ChunkData(blocks, size, height, size);
    }

    private static float[,] GenerateHeightMap(int offsetX, int offsetZ)
    {
        int size = Chunk.SIZE;
        float[,] heightMap = new float[size, size];

        // Ensure noise seed is initialized
        EnsureNoiseSeedInitialized();

        for (int x = 0; x < size; x++)
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

        return heightMap;
    }

    private static float[,] SmoothHeightMap(float[,] heightMap)
    {
        int size = heightMap.GetLength(0);
        float[,] smoothedHeightMap = new float[size, size];

        // Single-pass blur: combine horizontal and vertical neighbors
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                float totalHeight = 0f;
                int count = 0;

                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx;
                    if (nx < 0 || nx >= size)
                    {
                        continue;
                    }

                    for (int dz = -1; dz <= 1; dz++)
                    {
                        int nz = z + dz;
                        if (nz < 0 || nz >= size)
                        {
                            continue;
                        }

                        totalHeight += heightMap[nx, nz];
                        count++;
                    }
                }

                smoothedHeightMap[x, z] = totalHeight / count;
            }
        }

        return smoothedHeightMap;
    }

    private static BlockType[] GenerateBlocks(float[,] heightMap)
    {
        heightMap = SmoothHeightMap(heightMap);

        int size = Chunk.SIZE;
        int height = Chunk.HEIGHT;
        int minHeight = 5;
        BlockType[] blocks = new BlockType[size * height * size];

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
                        int index = x * height * size + y * size + z;
                        if (y <= 0)
                        {
                            blocks[index] = BlockType.BEDROCK;
                        }
                        else if (y <= columnHeight)
                        {
                            blocks[index] = BlockType.STONE;
                        }
                        else if (y == columnHeight + 1)
                        {
                            blocks[index] = BlockType.SAND;
                        }
                        else if (y == columnHeight + 2)
                        {
                            blocks[index] = BlockType.DIRT;
                        }
                        else if (y == columnHeight + 3)
                        {
                            blocks[index] = BlockType.GRASS;
                        }
                        else
                        {
                            blocks[index] = BlockType.AIR;
                        }
                    }
                }
            }
        );

        return blocks;
    }

    private static void GenerateFaces(
        ChunkData chunkData,
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
                        if (chunkData.GetBlock(x, y, z) != BlockType.AIR)
                        {
                            AddVisibleFaces(
                                x,
                                y,
                                z,
                                chunkData,
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
                    foreach (uint index in localMeshData.Indices)
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
        ChunkData chunkData,
        ChunkPosition chunkPosition,
        PerThreadMeshData localMeshData,
        WorldSystem worldSystem
    )
    {
        BlockType blockType = chunkData.GetBlock(x, y, z);

        if (IsFaceVisible(x, y, z + 1, chunkData, chunkPosition, worldSystem))
        {
            AddFace(x, y, z, Faces.FRONT, blockType, localMeshData);
        }

        if (IsFaceVisible(x, y, z - 1, chunkData, chunkPosition, worldSystem))
        {
            AddFace(x, y, z, Faces.BACK, blockType, localMeshData);
        }

        if (IsFaceVisible(x + 1, y, z, chunkData, chunkPosition, worldSystem))
        {
            AddFace(x, y, z, Faces.RIGHT, blockType, localMeshData);
        }

        if (IsFaceVisible(x - 1, y, z, chunkData, chunkPosition, worldSystem))
        {
            AddFace(x, y, z, Faces.LEFT, blockType, localMeshData);
        }

        if (IsFaceVisible(x, y + 1, z, chunkData, chunkPosition, worldSystem))
        {
            AddFace(x, y, z, Faces.TOP, blockType, localMeshData);
        }

        if (IsFaceVisible(x, y - 1, z, chunkData, chunkPosition, worldSystem))
        {
            AddFace(x, y, z, Faces.BOTTOM, blockType, localMeshData);
        }
    }

    private static bool IsFaceVisible(
        int x,
        int y,
        int z,
        ChunkData chunkData,
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
            return chunkData.GetBlock(x, y, z) == BlockType.AIR;
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

            // Check if the neighboring chunk exists in the world
            if (worldSystem.ChunkExists(neighborChunkPosition))
            {
                // Get the neighboring chunk's data
                ChunkData? neighborChunk = worldSystem.GetChunk(neighborChunkPosition);

                // If the chunk is null or the block is air, the face is visible
                if (neighborChunk == null)
                {
                    return true;
                }

                // Check if the corresponding block in the neighboring chunk is air
                return neighborChunk.GetBlock(neighborX, neighborY, neighborZ)
                    == BlockType.AIR;
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
        ReadOnlySpan<Vector3> faceVertices = FaceDataRaw.rawVertexData[(int)face];

        ref readonly Vector3 normal = ref FaceDataRaw.faceNormals[(int)face];

        Vector3 offset = new(x, y, z);
        foreach (ref readonly Vector3 vert in faceVertices)
        {
            localMeshData.Vertices.Add(vert + offset);
            localMeshData.Normals.Add(normal);
        }

        ReadOnlySpan<Vector2> uvCoords = TextureData.GetUVsSpan(blockType, face);
        for (int i = 0; i < uvCoords.Length; i++)
        {
            localMeshData.UVs.Add(uvCoords[i]);
        }

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
internal class PerThreadMeshData
{
    public List<Vector3> Vertices = new(1024);
    public List<Vector2> UVs = new(1024);
    public List<Vector3> Normals = new(1024);
    public List<uint> Indices = new(3072);
    public uint TotalIndexCount;
}
