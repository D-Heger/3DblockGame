using BenchmarkDotNet.Attributes;
using OpenTK.Mathematics;
using VoxelGame.World.Data;

namespace Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class TextureDataBenchmarks
{
    [Benchmark]
    public static void GetUVsSpan()
    {
        for (int bt = 0; bt < 7; bt++)
        {
            for (int f = 0; f < 6; f++)
            {
                ReadOnlySpan<Vector2> uvs = TextureData.GetUVsSpan((BlockType)bt, (Faces)f);
                if (uvs.Length != 4)
                {
                    throw new Exception("Invalid UV count");
                }
            }
        }
    }
}
