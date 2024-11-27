using System.Collections.Generic;
using System.Threading.Tasks;
using OpenTK.Mathematics;
using VoxelGame.EntityComponentSystem.Systems;
using VoxelGame.World.Data;

namespace VoxelGame.World
{
    public static class ChunkGenerator
    {
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
            SimplexNoise.Noise.Seed = new Random().Next();

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
                            0.01f
                        );
                        heightMap[x, z] = noiseValue;
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
                        int columnHeight = (int)(heightMap[x, z] / 16);
                        for (int y = 0; y < heigh; y++)
                        {
                            if (y <= columnHeight)
                            {
                                blocks[x, y, z] = BlockType.STONE;
                            }
                            else if (y == columnHeight + 1)
                            {
                                blocks[x, y, z] = BlockType.SAND;
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
                ChunkPosition neighbourChunkPosition = chunkPosition;

                int neighbourX = x;
                int neighbourY = y;
                int neighbourZ = z;

                // Adjust the neighboring chunk position and local coordinates
                if (x < 0)
                {
                    neighbourChunkPosition = new ChunkPosition(
                        neighbourChunkPosition.X - size,
                        neighbourChunkPosition.Y,
                        neighbourChunkPosition.Z
                    );
                    neighbourX = x + size; // Wrap to the other side of the neighboring chunk
                }
                else if (x >= size)
                {
                    neighbourChunkPosition = new ChunkPosition(
                        neighbourChunkPosition.X + size,
                        neighbourChunkPosition.Y,
                        neighbourChunkPosition.Z
                    );
                    neighbourX = x - size; // Wrap to the other side of the neighboring chunk
                }

                if (y < 0 || y >= height)
                {
                    // If y is out of bounds, there is no neighboring chunk vertically.
                    // Assume the face is visible, as there is no block above or below the chunk.
                    return true;
                }

                if (z < 0)
                {
                    neighbourChunkPosition = new ChunkPosition(
                        neighbourChunkPosition.X,
                        neighbourChunkPosition.Y,
                        neighbourChunkPosition.Z - size
                    );
                    neighbourZ = z + size; // Wrap to the other side of the neighboring chunk
                }
                else if (z >= size)
                {
                    neighbourChunkPosition = new ChunkPosition(
                        neighbourChunkPosition.X,
                        neighbourChunkPosition.Y,
                        neighbourChunkPosition.Z + size
                    );
                    neighbourZ = z - size; // Wrap to the other side of the neighboring chunk
                }

                // Check if the neighboring chunk exists in the world
                if (worldSystem.ChunkExists(neighbourChunkPosition))
                {
                    // Get the neighboring chunk's data
                    ChunkData neighbourChunk = worldSystem.GetChunk(neighbourChunkPosition);

                    // Check if the corresponding block in the neighboring chunk is air
                    return neighbourChunk.Blocks[neighbourX, neighbourY, neighbourZ]
                        == BlockType.AIR;
                }
                else
                {
                    // If the neighboring chunk does not exist, assume the face is visible
                    return true;
                }
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
