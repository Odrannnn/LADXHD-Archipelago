using System.IO;
using VCDiff.Encoders;
using VCDiff.Decoders;
using VCDiff.Includes;

namespace LADXHD_Launcher
{
    internal class VCDiff
    {
        public enum Operation { Create, Apply }

        private static void Create(string oldFile, string newFile, string patchFile)
        {
            using var source = File.OpenRead(oldFile);
            using var target = File.OpenRead(newFile);
            using var patch  = File.Create(patchFile);

            var encoder = new VcEncoder(source, target, patch);
            VCDiffResult result = encoder.Encode(interleaved: true);

            if (result != VCDiffResult.SUCCESS)
                throw new System.Exception($"VCDiff encode failed: {result}");
        }

        private static void Apply(string oldFile, string patchFile, string newFile)
        {
            using var source = File.OpenRead(oldFile);
            using var patch  = File.OpenRead(patchFile);
            using var output = File.Create(newFile);

            var decoder = new VcDecoder(source, patch, output);
            VCDiffResult result = decoder.Decode(out long _);

            if (result != VCDiffResult.SUCCESS)
                throw new System.Exception($"VCDiff decode failed: {result}");
        }

        public static void Execute(Operation action, string input, string diff, string output, string target = "")
        {
            if (action == Operation.Apply)
                Apply(input, diff, output);
            else if (action == Operation.Create)
                Create(input, diff, output);

            if (!string.IsNullOrEmpty(target))
                output.MovePath(target, true);
        }
    }
}