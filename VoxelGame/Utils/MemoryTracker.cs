using System.Diagnostics;

namespace VoxelGame.Utils;

public class MemoryTracker
{
    private static readonly Queue<long> _memoryHistory = new(60); // Store last 60 samples
    private static DateTime _lastUpdate = DateTime.Now;
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    public static void Update()
    {
        if (DateTime.Now - _lastUpdate < UpdateInterval)
        {
            return;
        }

        _lastUpdate = DateTime.Now;

        Process currentProcess = Process.GetCurrentProcess();
        long memoryUsageMB = currentProcess.WorkingSet64 / (1024 * 1024); // Convert to MB

        _memoryHistory.Enqueue(memoryUsageMB);
        if (_memoryHistory.Count > 60)
        {
            _memoryHistory.Dequeue();
        }
    }

    public static (long Current, long Average, long Peak) GetMemoryStats()
    {
        if (_memoryHistory.Count == 0)
        {
            return (0, 0, 0);
        }

        long current = _memoryHistory.Last();
        long average = (long)_memoryHistory.Average();
        long peak = _memoryHistory.Max();

        return (current, average, peak);
    }
}
