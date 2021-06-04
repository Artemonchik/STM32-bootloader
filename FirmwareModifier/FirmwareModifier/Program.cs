#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CommandLine;

namespace FirmwareModifier
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(WithParsedAction);
        }

        private static void WithParsedAction(Options opts)
        {
            if (opts.ModifyLinker != String.Empty)
            {
                var bootloaderSize =
                    Convert.ToUInt32(opts.BootloaderSize);

                var linkerPath = opts.ModifyLinker;

                using var reader = new StreamReader(linkerPath);
                char[] txt = Array.Empty<char>();
                while (!reader.EndOfStream)
                {
                    txt = txt.Append((char)reader.Read()).ToArray();
                }
                reader.Close();
                string str = new(txt);

                var pattern =
                    @"(MEMORY[\s\n]*{[^}]*FLASH\s*\(.*\)\s*:\s*ORIGIN\s*=\s*)(0x[0-9a-fA-F]+)(\s*,\s*LENGTH\s=\s)(\d+)(K[^{]*})";
                var match = Regex.Match(str, pattern, RegexOptions.Singleline);
                
                var origin = 
                    Convert.ToUInt32(
                    match
                        .Groups[2]
                        .ToString(), 
                    16
                    ) + bootloaderSize;
                
                var length = 
                    Convert.ToUInt32(
                        match
                            .Groups[4]
                            .ToString(), 
                        10
                        ) - (bootloaderSize / 1024 + (bootloaderSize % 1024 > 0 ? 1 : 0));
                var newStr =
                    Regex.Replace(
                        str,
                        pattern,
                        match.Groups[1].ToString() +
                            "0x" + Convert.ToString(origin, 16) +
                            match.Groups[3] +
                            length +
                            match.Groups[5]
                        );
                File.Delete(opts.ModifyLinker);
                using var writer = new StreamWriter(opts.ModifyLinker);
                writer.Write(newStr);
                writer.Close();
            }
        }
    }

    internal class Options
    {
        [Option('l', 
            "modify-linker-script", 
            Required = false, 
            HelpText = "Modifies FLASH variable for bootloader compatibility in a linker script. " +
                       "You have to provide a filename (as a parameter to this option) and bootloader size (-s) for this.")]
        public string ModifyLinker { set; get; }
        [Option('s',
            "bootloader-size",
            Required = false,
            HelpText = "The size of the compiled bootloader in bytes.",
            Default = "16512")]
        public string? BootloaderSize { set; get; } 
    }
}