using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;

namespace pdj.tiny7z
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== 100% Pure Parallel Process-Isolation Tool ===");

            // =================================================================
            // CONFIGURATION: Set your paths here
            // =================================================================
            string sourceFolder = @"D:\Study File\degree\Sem5\TPC6323\project\can\parallel\tiny7z-master\test";
            string outputDirectory = @"D:\Study File\degree\Sem5\TPC6323\project\can\parallel\tiny7z-master\parallel_output";

            // Path to the CLI executable you compiled in step 1
            string exePath = @"D:\Study File\degree\Sem5\TPC6323\project\can\parallel\tiny7z-master\tiny7zTool\bin\Debug\t7zt.exe";
            // =================================================================

            if (!Directory.Exists(sourceFolder) || !File.Exists(exePath))
            {
                Console.WriteLine("Error: Source folder or t7zt.exe missing!");
                return;
            }

            if (Directory.Exists(outputDirectory)) Directory.Delete(outputDirectory, true);
            Directory.CreateDirectory(outputDirectory);

            string[] filesToCompress = Directory.GetFiles(sourceFolder, "*.*", SearchOption.AllDirectories);
            Console.WriteLine($"Found {filesToCompress.Length} files to process in parallel.");

            // =================================================================
            // PURE PARALLEL PROCESSING VIA PROCESS ISOLATION
            // =================================================================
            Stopwatch timer = Stopwatch.StartNew();

            // Utilizing Task Parallel Library (TPL) to spawn OS processes simultaneously
            Parallel.ForEach(filesToCompress, file =>
            {
                string filename = Path.GetFileName(file);
                string targetArchive = Path.Combine(outputDirectory, filename + ".7z");

                lock (Console.Out)
                {
                    Console.WriteLine($"[Core/Thread {Environment.CurrentManagedThreadId}] Launching independent process for: {filename}");
                }

                // Configuring OS Process Start Info for absolute memory isolation
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = $"a \"{targetArchive}\" \"{file}\"", // Command: add file to archive
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                // Launching the process on a dedicated CPU worker thread
                using (Process process = Process.Start(startInfo))
                {
                    // Wait for this isolated process to finish without blocking other parallel threads
                    process.WaitForExit();
                }
            });

            timer.Stop();
            Console.WriteLine($"\n>> Pure Parallel Execution Successful!");
            Console.WriteLine($"Total Time Taken: {timer.ElapsedMilliseconds} ms");
            Console.ReadKey();
        }
    }
}