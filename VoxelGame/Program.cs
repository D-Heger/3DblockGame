namespace VoxelGame;

/// <summary>
/// The main entry point class for the Voxel Game application.
/// This class initializes and launches the game window.
/// </summary>
public class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// Creates a new game instance with a 1280x720 window size and starts the game loop.
    /// </summary>
    /// <param name="args">Command line arguments passed to the application (not used)</param>
    /// <remarks>
    /// The game instance is created within a using statement to ensure proper disposal
    /// of resources when the game exits. The window size is set to 1280x720 pixels.
    /// </remarks>
    static void Main(string[] args)
    {
        // Creates game object and disposes of it after leaving the scope
        using Game game = new(1280, 720);
        // running the game
        game.Run();
    }
}
