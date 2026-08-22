/// <summary>
/// Entry point for the Content Builder project, 
/// which when executed will build content according to the "Content Collection Strategy" defined in the Builder class.
/// </summary>
/// <remarks>
/// Make sure to validate the directory paths in the "ContentBuilderParams" for your specific project.
/// For more details regarding the Content Builder, see the MonoGame documentation: <tbc.>
/// </remarks>

using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using MonoGame.Extended.Content.Pipeline.BitmapFonts;
using MonoGame.Framework.Content.Pipeline.Builder;

static string GetProjectRoot([CallerFilePath] string here = "")
{
    var dir = new DirectoryInfo(Path.GetDirectoryName(here)!);
    while (dir != null && dir.GetFiles("*.csproj").Length == 0)
        dir = dir.Parent;

    return dir?.FullName
        ?? throw new DirectoryNotFoundException("Could not locate the ProjectZ.Content project directory.");
}

var projectRoot = GetProjectRoot();
var totalFailures = 0;

//--------------------------------------------------------------------------------------------------------
// The content folders to be written. The "OutputDirectory" is in the project "bin" folder.
//--------------------------------------------------------------------------------------------------------

TargetPlatform platform = args.Length > 0
    ? Enum.Parse<TargetPlatform>(args[0], true)
    : TargetPlatform.DesktopGL;

string outputDir = platform switch
{
    TargetPlatform.Android      => Path.Combine("bin", "Android"),
    TargetPlatform.DesktopGL    => Path.Combine("bin", "DesktopGL"),
    TargetPlatform.DesktopVK    => Path.Combine("bin", "Vulkan"),
    TargetPlatform.Windows      => Path.Combine("bin", "DirectX11"),
    TargetPlatform.WindowsDX12  => Path.Combine("bin", "DirectX12"),
    _ => throw new NotSupportedException()
};

var sourceDirectory = args.Length > 1
    ? Path.GetFullPath(args[1])
    : Path.Combine(projectRoot, "Content");

//--------------------------------------------------------------------------------------------------------
// Write to output folders when testing with Visual Studio.
//--------------------------------------------------------------------------------------------------------

Console.WriteLine($"=== Building content for {platform} -> {outputDir} ===");

var contentCollectionArgs = new ContentBuilderParams()
{
    Mode = ContentBuilderMode.Builder,
    WorkingDirectory = projectRoot,
    SourceDirectory = sourceDirectory,
    OutputDirectory = outputDir,
    IntermediateDirectory = Path.Combine(projectRoot, "obj", platform.ToString()),
    Platform = platform,
    CompressContent = false,
    LogLevel = LogLevel.Info
};
var builder = new Builder();
builder.Platform = platform;
builder.Run(contentCollectionArgs);
totalFailures += (int)builder.FailedToBuild;

//--------------------------------------------------------------------------------------------------------

return totalFailures > 0 ? -1 : 0;

public class Builder : ContentBuilder
{
    public TargetPlatform Platform { get; set; }

    public override IContentCollection GetContentCollection()
    {
        // start a new collection.
        var contentCollection = new ContentCollection();

        // Include the base folder by default.
        contentCollection.Include<WildcardRule>("*");

        // Generated content and intermediates are not source assets.
        contentCollection.Exclude<WildcardRule>("bin/*");
        contentCollection.Exclude<WildcardRule>("obj/*");
        contentCollection.Exclude<WildcardRule>("*.mgcb");

        // Exclude any atlas or txt files.
        contentCollection.Exclude<WildcardRule>("*.atlas");
        contentCollection.Exclude<WildcardRule>("*.txt");

        // Because MonoGame decided not to incorporate a DX12 variable for shaders, we make our own.
        var effectProcessor = new EffectProcessor();
        if (Platform == TargetPlatform.WindowsDX12)
            effectProcessor.Defines = "DIRECTX12";
        contentCollection.Include<WildcardRule>("Shader/*.fx", new EffectImporter(), effectProcessor);

        // Create a list of SpriteFont textures.
        string[] spriteFontTextures =
        {
            "Fonts/credits font.png",
            "Fonts/credits header font.png",
            "Fonts/headerFont.png",
            "Fonts/newHeaderFont.png",
            "Fonts/smallFont.png",
            "Fonts/smallFont_redux.png",
            "Fonts/smallFont_vwf.png",
            "Fonts/smallFont_vwf_redux.png",
        };
        // Sprite fonts are assembled from a PNG file. 
        foreach (var path in spriteFontTextures)
        {
            var texImporter = new FontTextureProcessor{ FirstCharacter = ' ', PremultiplyAlpha = true, TextureFormat = TextureProcessorOutputFormat.Color };
            contentCollection.Include<WildcardRule>(path, new TextureImporter(), texImporter);
        }
        // Bitmap fonts use MonoGame extended.
        contentCollection.Include<WildcardRule>("Fonts/smallFont_chn.fnt", new BitmapFontImporter(), new BitmapFontProcessor());
        contentCollection.Include<WildcardRule>("Fonts/smallFont_chn_redux.fnt", new BitmapFontImporter(), new BitmapFontProcessor());

        // Bitmap font atlas pages stay plain Texture2D.
        contentCollection.Include<WildcardRule>("Fonts/smallFont_chn_0.png", new TextureImporter(), new TextureProcessor { ColorKeyEnabled = false, PremultiplyAlpha = true });
        contentCollection.Include<WildcardRule>("Fonts/smallFont_chn_redux_0.png", new TextureImporter(), new TextureProcessor { ColorKeyEnabled = false, PremultiplyAlpha = true });

        return contentCollection;
    }
}
