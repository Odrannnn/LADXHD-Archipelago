namespace ProjectZ.Editor
{
    public static class EditorBootstrap
    {
        public static EditorManager Create(Game1 game) => new EditorManager(game);
    }
}
