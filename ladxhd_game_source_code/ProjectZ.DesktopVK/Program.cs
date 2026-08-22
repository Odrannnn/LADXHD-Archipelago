using System;

namespace ProjectZ
{
    public static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var editorMode = false;
            var loadSave = false;
            var saveSlot = 0;

            // Try to find any arguments added.
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                // Editor Mode
                if (arg.Equals("editor", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("--editor", StringComparison.OrdinalIgnoreCase))
                {
                    editorMode = true;
                }

                // Quick Load Save File Slot
                else if (arg.Equals("loadSave", StringComparison.OrdinalIgnoreCase))
                {
                    loadSave = true;

                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int parsedSlot))
                    {
                        saveSlot = parsedSlot;
                        i++;
                    }
                }
            }

            // Try to start the game.
            try
            {
                using (var game = new Game1(editorMode, loadSave, saveSlot))
                {
                    game.Services.AddService(typeof(IEditorManager), ProjectZ.Editor.EditorBootstrap.Create(game));
                    game.Services.AddService(typeof(IPlatformFileSystem), new LocalPlatformFileSystem());
                    game.Services.AddService(typeof(IUserDataPaths), new LocalUserDataPaths());
                    game.Services.AddService(typeof(ISharedSaveService), new UnavailableSharedSaveService());
                    game.Services.AddService(typeof(IPlatformInput), new NullPlatformInput());
                    game.Services.AddService(typeof(IPlatformWindow), new DesktopVKPlatformWindow());
                    game.Services.AddService(typeof(IGraphicsCapabilities), new GraphicsCapabilities(
                        usePresentationParametersForSize: false,
                        canCreateGraphicsResourcesOnWorkerThread: false,
                        supportsBlendFunctionMax: true,
                        useAnisotropicFiltering: true));
                    game.Services.AddService(typeof(IPlatformPresentation), new PlatformPresentation(256, false, false, 0));
                    game.Services.AddService(typeof(IFileDialogService), new NativeFileDialogService());
                    game.Run();
                }
            }

            // If it fails, catch the exception and print out a crash log.
            catch (Exception exception)
            {
                // Cross-platform: write to stderr + optionally a file
                Console.Error.WriteLine(exception.ToString());
                System.IO.File.WriteAllText("crash.txt", exception.ToString());
                throw;
            }
        }
    }
}
