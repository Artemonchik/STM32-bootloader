#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
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
                var genKey = "key.bin";
                var key = new byte[32];
                RandomNumberGenerator.Create().GetBytes(key);
                using var writer = new BinaryWriter(new FileStream(genKey, FileMode.OpenOrCreate, FileAccess.Write));
                writer.Write(key);
            }

            if (opts.DownloadRepo)
            {
                var tempdir = DownloadRepo(opts.GitExec ?? "git");

                DestroyDotGitDir(tempdir);

                var coreDir = tempdir + "/code-transmition/Core";

                if (Directory.Exists("Core")) Directory.Delete("Core", true);
                Directory.Move(coreDir, "Core");
                Directory.Delete(tempdir, true);


                FilterCoreDir();
            }
        }

        /// <summary>
        ///     Clones the repo to a temporary directory.
        /// </summary>
        /// <param name="gitExec">Path to the git executable</param>
        /// <returns>The name of the created temporary directory</returns>
        private static string DownloadRepo(string gitExec)
        {
            var repoURL = "https://github.com/Artemonchik/STM32-bootloader";
            var process = new Process();
            var tempdir = "temp" + new Random().Next();
            var startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden, FileName = gitExec,
                Arguments = $" clone {repoURL} {tempdir} --depth 1 --branch master --single-branch" 
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
            Directory.Delete(Path.Combine(coreDir, "Startup"), true);
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
    }
}