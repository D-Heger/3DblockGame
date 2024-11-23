using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

internal class Chunk
{
    public List<Vector3> ChunkVertices;
    public List<Vector2> ChunkTextureCoordinates;
    public List<uint> ChunkIndices;

    private const sbyte SIZE = 16;
    private const short HEIGHT = 384;
    public Vector3 Position;

    public uint TotalIndexCount;

    private VertexArrayObject _chunkVertexArrayObject;
    private VertexBufferObject _chunkVertexBufferObject;
    private VertexBufferObject _chunkTextureCoordinatesBuffer;
    private IndexBufferObject _chunkIndexBufferObject;

    private Texture _texture;
    private Block[,,] _chunkBlocks = new Block[SIZE, HEIGHT, SIZE];

    public Chunk(Vector3 postition)
    {
        Position = postition;

        ChunkVertices = [];
        ChunkTextureCoordinates = [];
        ChunkIndices = [];

        float[,] heightMap = GenerateChunk();
        GenBlocks(heightMap);
        GenFaces(heightMap);
        BuildChunk();
    }

    public float[,] GenerateChunk()
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

    public void GenBlocks(float[,] heightMap)
    {
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

                    _chunkBlocks[x, y, z] = new Block(new Vector3(x, y, z), type);
                }
            }
        }
    }

    public void GenFaces(float[,] heightMap)
    {
        for (int x = 0; x < SIZE; x++)
        {
            for (int z = 0; z < SIZE; z++)
            {
                for (int y = 0; y < HEIGHT; y++)
                {
                    int numFaces = 0;

                    if (_chunkBlocks[x, y, z].Type != BlockType.AIR)
                    {
                        // Left Faces
                        if (x > 0)
                        {
                            if (_chunkBlocks[x - 1, y, z].Type == BlockType.AIR)
                            {
                                IntegrateFace(_chunkBlocks[x, y, z], Faces.LEFT);
                                numFaces++;
                            }
                        }
                        else
                        {
                            IntegrateFace(_chunkBlocks[x, y, z], Faces.LEFT);
                            numFaces++;
                        }

                        // Right Faces
                        if (x < SIZE - 1)
                        {
                            if (_chunkBlocks[x + 1, y, z].Type == BlockType.AIR)
                            {
                                IntegrateFace(_chunkBlocks[x, y, z], Faces.RIGHT);
                                numFaces++;
                            }
                        }
                        else
                        {
                            IntegrateFace(_chunkBlocks[x, y, z], Faces.RIGHT);
                            numFaces++;
                        }

                        // Top Faces
                        if (y < HEIGHT - 1)
                        {
                            if (_chunkBlocks[x, y + 1, z].Type == BlockType.AIR)
                            {
                                IntegrateFace(_chunkBlocks[x, y, z], Faces.TOP);
                                numFaces++;
                            }
                        }
                        else
                        {
                            IntegrateFace(_chunkBlocks[x, y, z], Faces.TOP);
                            numFaces++;
                        }

                        // Bottom Faces
                        if (y > 0)
                        {
                            if (_chunkBlocks[x, y - 1, z].Type == BlockType.AIR)
                            {
                                IntegrateFace(_chunkBlocks[x, y, z], Faces.BOTTOM);
                                numFaces++;
                            }
                        }
                        else
                        {
                            IntegrateFace(_chunkBlocks[x, y, z], Faces.BOTTOM);
                            numFaces++;
                        }

                        // Front Faces
                        if (z < SIZE - 1)
                        {
                            if (_chunkBlocks[x, y, z + 1].Type == BlockType.AIR)
                            {
                                IntegrateFace(_chunkBlocks[x, y, z], Faces.FRONT);
                                numFaces++;
                            }
                        }
                        else
                        {
                            IntegrateFace(_chunkBlocks[x, y, z], Faces.FRONT);
                            numFaces++;
                        }

                        // Back Faces
                        if (z > 0)
                        {
                            if (_chunkBlocks[x, y, z - 1].Type == BlockType.AIR)
                            {
                                IntegrateFace(_chunkBlocks[x, y, z], Faces.BACK);
                                numFaces++;
                            }
                        }
                        else
                        {
                            IntegrateFace(_chunkBlocks[x, y, z], Faces.BACK);
                            numFaces++;
                        }

                        AddIndices(numFaces);
                    }
                }
            }
        }
    }

    public void IntegrateFace(Block block, Faces face)
    {
        var faceData = block.GetFace(face);
        ChunkVertices.AddRange(faceData.Vertices);
        ChunkTextureCoordinates.AddRange(faceData.TextureCoordinates);
    }

    public void AddIndices(int amtFaces)
    {
        for (int i = 0; i < amtFaces; i++)
        {
            ChunkIndices.Add(0 + TotalIndexCount);
            ChunkIndices.Add(1 + TotalIndexCount);
            ChunkIndices.Add(2 + TotalIndexCount);
            ChunkIndices.Add(2 + TotalIndexCount);
            ChunkIndices.Add(3 + TotalIndexCount);
            ChunkIndices.Add(0 + TotalIndexCount);

            TotalIndexCount += 4;
        }
    }

    public void BuildChunk()
    {
        _chunkVertexArrayObject = new VertexArrayObject();
        _chunkVertexArrayObject.Bind();

        _chunkVertexBufferObject = new VertexBufferObject(ChunkVertices);
        _chunkVertexBufferObject.Bind();
        _chunkVertexArrayObject.LinkToVAO(0, 3, _chunkVertexBufferObject);

        _chunkTextureCoordinatesBuffer = new VertexBufferObject(ChunkTextureCoordinates);
        _chunkTextureCoordinatesBuffer.Bind();
        _chunkVertexArrayObject.LinkToVAO(1, 2, _chunkTextureCoordinatesBuffer);

        _chunkIndexBufferObject = new IndexBufferObject(ChunkIndices);

        _texture = new Texture("atlas");
    }

    public void RenderChunk(ShaderProgram program) // drawing the chunk
    {
        program.Bind();
        _chunkVertexArrayObject.Bind();
        _chunkIndexBufferObject.Bind();
        _texture.Bind();
        GL.DrawElements(
            PrimitiveType.Triangles,
            ChunkIndices.Count,
            DrawElementsType.UnsignedInt,
            0
        );
    }

    public void Dispose()
    {
        _chunkVertexArrayObject.Dispose();
        _chunkVertexBufferObject.Dispose();
        _chunkTextureCoordinatesBuffer.Dispose();
        _chunkIndexBufferObject.Dispose();
        _texture.Dispose();
    }
}
