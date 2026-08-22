namespace ProjectZ
{
    public interface IPlatformDisplayConfiguration
    {
        int PreferredBackBufferWidth { get; }
        int PreferredBackBufferHeight { get; }
    }

    public sealed class PlatformDisplayConfiguration : IPlatformDisplayConfiguration
    {
        public PlatformDisplayConfiguration(int preferredBackBufferWidth, int preferredBackBufferHeight)
        {
            PreferredBackBufferWidth = preferredBackBufferWidth;
            PreferredBackBufferHeight = preferredBackBufferHeight;
        }

        public int PreferredBackBufferWidth { get; }
        public int PreferredBackBufferHeight { get; }
    }
}
