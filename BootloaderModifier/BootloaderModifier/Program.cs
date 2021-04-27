using System.IO;
using System.Security.Cryptography;
using CommandLine;

namespace BootloaderModifier
{
    static class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<ModifierOptions>(args)
                .WithParsed<ModifierOptions>(opts =>
                    {
                        var genKey = opts.GenKey;
                        var writer = new BinaryWriter(new FileStream(genKey, FileMode.OpenOrCreate, FileAccess.Write));
                        var key = new byte[32];
                        RandomNumberGenerator.Create().GetBytes(key);
                        writer.Write(key);
                        writer.Close();
                    }
                );
        }
    }

    internal class ModifierOptions
    {
        [Option('k', "generate_key", Required = false, HelpText = "Generate a key.", Default = "key.bin")]
        public string GenKey { set; get; }
    }
}