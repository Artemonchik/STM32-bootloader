#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using BootloaderFileFormat;
using CommandLine;

namespace BootloaderModifier
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                Parser.Default
                    .ParseArguments<ModifierOptions>(args)
                    .WithParsed(options => OptionsWithParsedAction(options));
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("No options were provided");
            }
        }

        private static void OptionsWithParsedAction(ModifierOptions opts)
        {
            if (opts.GenKey)
            {
                const string genKey = "key.bin";
                var key = new byte[32];
                RandomNumberGenerator.Create().GetBytes(key);
                using var writer = new BinaryWriter(new FileStream(genKey, FileMode.OpenOrCreate, FileAccess.Write));
                writer.Write(key);
                
                //remove all .elf files also 
                //just for fun
                /*var name = Directory.GetParent(Directory.GetCurrentDirectory())?.Name;
                if (name != null)
                {
                    var files = GetAllFiles(name);
                    foreach(var t in files)
                    {
                        if (!t.TrimEnd().EndsWith(".elf")) continue;
                        try
                        {
                            File.Delete(t);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                }*/
            }

            if (opts.DownloadRepo)
            {
                var tempdir = DownloadRepo(opts.GitExec ?? "git");

                DestroyDotGitDir(tempdir);

                var srcDir = tempdir + "/code-transmition/Core/Src";
                var incDir = tempdir + "/code-transmition/Core/Inc";

                if (Directory.Exists("Core/Src")) Directory.Delete("Core/Src", true);
                if (Directory.Exists("Core/Inc")) Directory.Delete("Core/Inc", true);
                Directory.Move(srcDir, "Core/Src");
                Directory.Move(incDir, "Core/Inc");
                Directory.Delete(tempdir, true);
                
                FilterCoreDir();
            }

            if (opts.LinkerScript != null)
            {
                var ldPath = opts.LinkerScript ?? "";
                if (!ldPath.EndsWith(".ld"))
                {
                    throw new FormatException("Linker script name should end with \"ld\".");
                }
                var list = new List<string>();
                using (var reader = new StreamReader(ldPath))
                {
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        list.Add(line);
                    }
                }

                const string text1 = "\nTARGET(binary)\n" +
                                     "INPUT(\"../key.bin\") /*Added by BootloaderModifier*/\n" +
                                     "OUTPUT_FORMAT(default)\n";
                const string text2 =
                    "\n  .bin_data :\n" +
                    "  {\n" +
                    "    symbol_1 = .;\n" +
                    "    . = ALIGN(4);\n" +
                    "    KEEP(*(.bin_data)) /* Bin data*/\n" +
                    "    . = ALIGN(4);\n" +
                    "    \"../key.bin\"\n" +
                    "  } >FLASH\n";
                
                for (var i = 0; i < list.Count; i++)
                {
                    if (list[i].TrimStart().StartsWith("SECTIONS"))
                    {
                        list.Insert(i, text1);
                        i++;
                    }    
                }
                
                for (var i = 0; i < list.Count; i++)
                {
                    if (list[i].TrimStart().StartsWith(".text"))
                    {
                        list.Insert(i, text2);
                        i++;
                    }    
                }
                File.Delete(ldPath);
                using var writer = new StreamWriter(ldPath);
                foreach (var str in list)
                {
                    writer.WriteLine(str);
                }
            }

            if (opts.DecryptData != null && opts.DecryptData.Any())
            {
                if (opts.DecryptData.Count() != 4)
                {
                    throw new ArgumentException("Argument count given to -a/--decrypt-data is not equal to 4.");
                }

                var paths = opts.DecryptData.ToArray();
                using var infile =
                    new BinaryReader(new FileStream(paths[0], FileMode.Open, FileAccess.Read));
                using var keyfile =
                    new BinaryReader(new FileStream(paths[1], FileMode.Open, FileAccess.Read));
                using var ivfile =
                    new BinaryReader(new FileStream(paths[2], FileMode.Open, FileAccess.Read));
                using var outfile =
                    new BinaryWriter(new FileStream(paths[3], FileMode.OpenOrCreate, FileAccess.Write));
                var inbytelist = new List<byte>();
                while (true)
                {
                    try
                    {
                        inbytelist.Add(infile.ReadByte());
                    } catch(EndOfStreamException)
                    {
                        break;
                    }
                }

                
                var keybytelist = new List<byte>();
                while (true)
                {
                    try
                    {
                        keybytelist.Add(keyfile.ReadByte());
                    } catch(EndOfStreamException)
                    {
                        break;
                    }
                }

                var ivbytelist = new List<byte>();
                while (true)
                {
                    try
                    {
                        ivbytelist.Add(ivfile.ReadByte());
                    } catch(EndOfStreamException)
                    {
                        break;
                    }
                }

                var inbytes = inbytelist.ToArray();
                var ivbytes = ivbytelist.ToArray();
                var keybytes = keybytelist.ToArray();
                
                var outbytes = Utilities.Decrypt(inbytes, keybytes, ivbytes);
                outfile.Write(outbytes);
            }
        }

        /// <summary>
        ///     Clones the repo to a temporary directory.
        /// </summary>
        /// <param name="gitExec">Path to the git executable</param>
        /// <returns>The name of the created temporary directory</returns>
        private static string DownloadRepo(string gitExec)
        {
            const string repoUrl = "https://github.com/Artemonchik/STM32-bootloader";
            var process = new Process();
            var tempdir = "temp" + new Random().Next();
            var startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden, FileName = gitExec,
                Arguments = $" clone {repoUrl} {tempdir} --depth 1 --branch master --single-branch" 
            };
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
            return tempdir;
        }

        /// <summary>
        ///     Deletes .git directory in tempdir.
        /// </summary>
        /// <param name="tempdir">Temporary directory created while cloning the repo</param>
        private static void DestroyDotGitDir(string? tempdir)
        {
            // following code is done only because in Windows a .git dir is hidden
            // and 2 files in it are readonly
            // this is why we cant have nice things
            DirectoryInfo dotGitDir = new(tempdir + "/.git");
            dotGitDir.Attributes &= ~FileAttributes.Hidden;
            File.SetAttributes(dotGitDir.FullName, dotGitDir.Attributes);
            var readonlyDir = new DirectoryInfo(dotGitDir.FullName + "/objects/pack");
            var readonlyFiles = readonlyDir.GetFiles();
            foreach (var i in readonlyFiles)
            {
                i.Attributes &= ~FileAttributes.ReadOnly;
                i.IsReadOnly = false;
                File.SetAttributes(i.FullName, i.Attributes);
                i.Delete();
            }

            dotGitDir.Delete(true);
        }

        private static readonly string[] SrcExclude =
        {
            "stm32f3xx_hal_msp.c",
            "stm32f3xx_it.c",
            "syscalls.c",
            "sysmem.c",
            "system_stm32f3xx.c"
        };
        private static readonly string[] IncExclude =
        {
            "stm32f3xx_hal_conf.h",
            "main.h",
            "stm32f3xx_it.h"
        };
        
        /// <summary>
        ///     Removes unnecessary files from Core directory.
        /// </summary>
        private static void FilterCoreDir()
        {
            var coreDir = "Core/";
            var srcFiles = Directory.GetFiles(Path.Combine(coreDir, "Src/"));
            var incFiles = Directory.GetFiles(Path.Combine(coreDir, "Inc/"));

            foreach (var i in srcFiles)
            foreach (var j in SrcExclude)
                if (i.EndsWith(j))
                    File.Delete(i);

            foreach (var i in incFiles)
            foreach (var j in IncExclude)
                if (i.EndsWith(j))
                    File.Delete(i);
        }
        private static IEnumerable<string> GetAllFiles(string path) {
            Queue<string> queue = new();
            queue.Enqueue(path);
            while (queue.Count > 0) {
                path = queue.Dequeue();
                try {
                    foreach (var subDir in Directory.GetDirectories(path)) {
                        queue.Enqueue(subDir);
                    }
                }
                catch(Exception ex) {
                    Console.Error.WriteLine(ex);
                }
                var files = Array.Empty<string>();
                try {
                    files = Directory.GetFiles(path);
                }
                catch (Exception ex) {
                    Console.Error.WriteLine(ex);
                }
                foreach (var t in files)
                {
                    yield return t;
                }
                
            }
        }
    }

    internal class ModifierOptions
    {
        [Option('k', "generate-key", Required = false, HelpText = "Generate a key.")]
        public bool GenKey { set; get; }

        [Option('d', "download-src",
            Required = false,
            HelpText = "Download sources for the bootloader. " +
                       "If you don't have git in $PATH/%PATH%, use --git-executable-path.")]
        public bool DownloadRepo { set; get; }

        [Option("git-executable-path", Required = false,
            HelpText = "The path to the git executable (used only with -d).")]
        public string? GitExec { set; get; }

        [Option('l', "edit-linker-script", Required = false,
            HelpText = "Edits a linker script of the bootloader to include an encryption key. " +
                       "This option requires a linker script path as a parameter.")]
        public string? LinkerScript { set; get; }
        
        [Option('a', "decrypt-data", Required = false,
            HelpText = "Decrypts a binary file using a given key and an IV"
            )]
        public IEnumerable<string>? DecryptData { set; get; }
    }
}