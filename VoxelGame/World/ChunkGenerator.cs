using System.Runtime.InteropServices;
using OpenTK.Mathematics;
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
        IChunkSource chunkSource,
        out ChunkData chunkData
    )
    {
        chunkData = GenerateChunkData(chunkPosition);
        return GenerateChunkMesh(chunkPosition, chunkSource, chunkData);
    }

    /// <summary>
    /// Meshes already-generated block data. Used for neighbor re-meshing, where
    /// the registered ChunkData is authoritative and regenerating it would
    /// duplicate (then discard) the expensive block-generation pass.
    /// </summary>
    public static ChunkMeshData GenerateChunkMesh(
        ChunkPosition chunkPosition,
        IChunkSource chunkSource,
        ChunkData chunkData
    )
    {
        List<ChunkVertex> vertices = [];
        List<uint> indices = [];
        GenerateFaces(chunkData, chunkPosition, vertices, indices, chunkSource);
        return BuildMeshData(vertices, indices);
    }

    private static ChunkMeshData BuildMeshData(List<ChunkVertex> vertices, List<uint> indices)
    {
        ChunkMeshData chunkMeshData = new()
        {
            Vertices = CollectionsMarshal.AsSpan(vertices).ToArray(),
        };

        if (vertices.Count <= ChunkMeshData.Max16BitVertices)
        {
            ushort[] indices16 = new ushort[indices.Count];
            for (int i = 0; i < indices.Count; i++)
            {
                indices16[i] = (ushort)indices[i];
            }
            chunkMeshData.Indices16 = indices16;
        }
        else
        {
            chunkMeshData.Indices32 = CollectionsMarshal.AsSpan(indices).ToArray();
        }

        return chunkMeshData;
    }

    public static ChunkData GenerateChunkData(ChunkPosition chunkPosition)
    {
        float[,] heightMap = GenerateHeightMap(chunkPosition.WorldX, chunkPosition.WorldZ);
        ChunkData chunkData = new();
        GenerateBlocks(heightMap, chunkData);
        return chunkData;
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

    private static void GenerateBlocks(float[,] heightMap, ChunkData chunkData)
    {
        heightMap = SmoothHeightMap(heightMap);

        int size = Chunk.SIZE;
        int minHeight = 5;

        Parallel.For(
            0,
            size,
            x =>
            {
                for (int z = 0; z < size; z++)
                {
                    int columnHeight = minHeight + (int)(heightMap[x, z] / 32);

                    // Air above columnHeight + 3 stays implicit (null sections).
                    chunkData.SetBlock(x, 0, z, BlockType.BEDROCK);
                    for (int y = 1; y <= columnHeight; y++)
                    {
                        chunkData.SetBlock(x, y, z, BlockType.STONE);
                    }
                    chunkData.SetBlock(x, columnHeight + 1, z, BlockType.SAND);
                    chunkData.SetBlock(x, columnHeight + 2, z, BlockType.DIRT);
                    chunkData.SetBlock(x, columnHeight + 3, z, BlockType.GRASS);
                }
            }
        );
    }

    private static void GenerateFaces(
        ChunkData chunkData,
        ChunkPosition chunkPosition,
        List<ChunkVertex> vertices,
        List<uint> indices,
        IChunkSource chunkSource
    )
    {
        object lockObj = new();

        Parallel.For(
            0,
            ChunkData.SectionCount,
            () => new PerThreadMeshData(),
            (sectionY, state, localMeshData) =>
            {
                // Null sections are all air and contribute no faces.
                ChunkSection? section = chunkData.Sections[sectionY];
                if (section == null)
                {
                    return localMeshData;
                }

                int yBase = sectionY << 4;
                BlockType[] blocks = section.Blocks;

                for (int x = 0; x < ChunkSection.Size; x++)
                {
                    for (int z = 0; z < ChunkSection.Size; z++)
                    {
                        for (int localY = 0; localY < ChunkSection.Size; localY++)
                        {
                            BlockType blockType = blocks[(x << 8) | (localY << 4) | z];
                            if (blockType == BlockType.AIR)
                            {
                                continue;
                            }

                            AddVisibleFaces(
                                x,
                                yBase + localY,
                                z,
                                blockType,
                                chunkData,
                                chunkPosition,
                                localMeshData,
                                chunkSource
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
                    uint indexOffset = (uint)vertices.Count;
                    vertices.AddRange(CollectionsMarshal.AsSpan(localMeshData.Vertices));

                    // Adjust indices
                    foreach (uint index in localMeshData.Indices)
                    {
                        indices.Add(index + indexOffset);
                    }
                }
            }
        );
    }

    private static void AddVisibleFaces(
        int x,
        int y,
        int z,
        BlockType blockType,
        ChunkData chunkData,
        ChunkPosition chunkPosition,
        PerThreadMeshData localMeshData,
        IChunkSource chunkSource
    )
    {
        if (IsFaceVisible(x, y, z + 1, chunkData, chunkPosition, chunkSource))
        {
            AddFace(x, y, z, Faces.FRONT, blockType, localMeshData);
        }

        if (IsFaceVisible(x, y, z - 1, chunkData, chunkPosition, chunkSource))
        {
            AddFace(x, y, z, Faces.BACK, blockType, localMeshData);
        }

        if (IsFaceVisible(x + 1, y, z, chunkData, chunkPosition, chunkSource))
        {
            AddFace(x, y, z, Faces.RIGHT, blockType, localMeshData);
        }

        if (IsFaceVisible(x - 1, y, z, chunkData, chunkPosition, chunkSource))
        {
            AddFace(x, y, z, Faces.LEFT, blockType, localMeshData);
        }

        if (IsFaceVisible(x, y + 1, z, chunkData, chunkPosition, chunkSource))
        {
            AddFace(x, y, z, Faces.TOP, blockType, localMeshData);
        }

        if (IsFaceVisible(x, y - 1, z, chunkData, chunkPosition, chunkSource))
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
        IChunkSource chunkSource
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
                    neighborChunkPosition.X - 1,
                    neighborChunkPosition.Z
                );
                neighborX = x + size; // Wrap to the other side of the neighboring chunk
            }
            else if (x >= size)
            {
                neighborChunkPosition = new ChunkPosition(
                    neighborChunkPosition.X + 1,
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
                    neighborChunkPosition.Z - 1
                );
                neighborZ = z + size; // Wrap to the other side of the neighboring chunk
            }
            else if (z >= size)
            {
                neighborChunkPosition = new ChunkPosition(
                    neighborChunkPosition.X,
                    neighborChunkPosition.Z + 1
                );
                neighborZ = z - size; // Wrap to the other side of the neighboring chunk
            }

            // Look up the neighboring chunk. A missing neighbor keeps its
            // border faces exposed until the neighbor arrives (the streaming
            // system corrects them via arrival re-meshing).
            ChunkData? neighborChunk = chunkSource.GetChunk(neighborChunkPosition);
            if (neighborChunk == null)
            {
                return true;
            }

            // The face is visible iff the adjacent block in the neighbor is air
            return neighborChunk.GetBlock(neighborX, neighborY, neighborZ)
                == BlockType.AIR;
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
        uint packedNormal = ChunkVertex.PackNormal(in normal);

        Vector3 offset = new(x, y, z);
        ReadOnlySpan<Vector2> uvCoords = TextureData.GetUVsSpan(blockType, face);

        for (int i = 0; i < faceVertices.Length; i++)
        {
            localMeshData.Vertices.Add(new ChunkVertex(faceVertices[i] + offset, uvCoords[i], packedNormal));
        }

        uint baseIndex = (uint)localMeshData.Vertices.Count - 4;

        localMeshData.Indices.Add(0 + baseIndex);
        localMeshData.Indices.Add(1 + baseIndex);
        localMeshData.Indices.Add(2 + baseIndex);
        localMeshData.Indices.Add(2 + baseIndex);
        localMeshData.Indices.Add(3 + baseIndex);
        localMeshData.Indices.Add(0 + baseIndex);
    }
}

// Class to hold per-thread mesh data
internal class PerThreadMeshData
{
    public List<ChunkVertex> Vertices = new(1024);
    public List<uint> Indices = new(3072);
}
