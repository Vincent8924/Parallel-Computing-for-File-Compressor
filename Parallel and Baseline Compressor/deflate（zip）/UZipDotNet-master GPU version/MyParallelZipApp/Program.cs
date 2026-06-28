using System;

using System.IO;

using System.Linq;

using System.Threading.Tasks;

using UZipDotNet;

using ILGPU;

using ILGPU.Runtime;

using System.Diagnostics;


//gpu parallel
namespace MyParallelZipApp

{

    class Program

    {

        static void Main(string[] args)

        {

            



            //string targetToCompress = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\fate";
            //string filepath = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\Gpu fate.zip";
            //string tempunzip = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\gpu fate";


            //string targetToCompress = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\Drone Videos";
            //string filepath = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\deflate\Drone Videos.zip";
            //string tempunzip = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\deflate\Drone Videos";


            //string targetToCompress = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\Fruits and Vegetables Image Recognition Dataset";
            //string filepath = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\Fruits and Vegetables Image Recognition Dataset.zip";
            //string tempunzip = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\Fruits and Vegetables Image Recognition Dataset";


            string targetToCompress = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\image\Image_1.jpg";
            string filepath = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\deflate\image.zip";
            string tempunzip = @"D:\Study File\degree\Sem5\TPC6323\project\dataset\test\deflate";





            using var context = ILGPU.Context.CreateDefault();

            using var accelerator = context.Devices[2].CreateAccelerator(context);

            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

            Stopwatch totalStopwatch = new Stopwatch();

            totalStopwatch.Start();





            compressWithGPU(filepath, targetToCompress, accelerator);




            decompressWithGPU(filepath, tempunzip, accelerator);



            totalStopwatch.Stop();



            Console.WriteLine("\n========================================");

            Console.WriteLine(" GPU PERFORMANCE REPORT ");

            Console.WriteLine("========================================");

            Console.WriteLine($"Using GPU Accelerator: {accelerator.Name}");

            Console.WriteLine($"Total Execution Time: {totalStopwatch.Elapsed.TotalSeconds:F4} seconds");

            Console.WriteLine($"Total Milliseconds: {totalStopwatch.ElapsedMilliseconds} ms");

            Console.WriteLine("========================================");



            Console.WriteLine("\nAll GPU Tasks completed successfully! Press any key to exit.");

            Console.ReadKey();

        }





        static void DrawProgressBar(string taskName, int progress, int total, int width = 30)

        {



            float percentage = (total == 0) ? 100 : ((float)progress / total);

            int progressBlocks = (int)(percentage * width);



         

            string progressBar = new string('█', progressBlocks) + new string('-', width - progressBlocks);





            Console.Write($"\r{taskName,-20} [{progressBar}] {percentage * 100:0.0}% ({progress}/{total})");

        }





        static void MyGpuProcessingKernel(

        Index1D index,

        ArrayView<byte> rawData,

        ArrayView<byte> processedData)

        {

            byte currentByte = rawData[index];

            processedData[index] = (byte)(currentByte ^ 0x5A);

        }



        static void compressWithGPU(string filepath, string targetToCompress, Accelerator accelerator)

        {
            Console.WriteLine("\n--- [GPU Parallel] Compression Started ---");
            try
            {
                if (File.Exists(filepath)) File.Delete(filepath);
            }
            catch (Exception ex) { Console.WriteLine("Ignore : " + ex); }

            bool isDirectory = Directory.Exists(targetToCompress);
            bool isFile = File.Exists(targetToCompress);

            if (!isDirectory && !isFile)
            {
                Console.WriteLine($"Error: Target not found: {targetToCompress}");
                return;
            }

            if(isFile)
            {
                var loadedKernel = accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<byte>, ArrayView<byte>>(MyGpuProcessingKernel);
                string[] allFiles = new string[] { targetToCompress };
                string? projectDir = Path.GetDirectoryName(filepath);
                string gpuTempFolder = Path.Combine(projectDir!, "GpuTempCompress");

                if (Directory.Exists(gpuTempFolder)) Directory.Delete(gpuTempFolder, true);
                Directory.CreateDirectory(gpuTempFolder);

                int totalFiles = allFiles.Length;

                Console.WriteLine("Step 1/2: GPU Hardware Encryption");

                for (int i = 0; i < totalFiles; i++)

                {
                    string file = allFiles[i];
                    string fileName = Path.GetFileName(file);
                    string folderName = Path.GetFileNameWithoutExtension(file);
                    string relativePath = folderName + "/" + fileName;
                    byte[] rawBytes = File.ReadAllBytes(file);

                    if (rawBytes.Length > 0)
                    {
                        using var gpuInputBuffer = accelerator.Allocate1D<byte>(rawBytes);
                        using var gpuOutputBuffer = accelerator.Allocate1D<byte>(rawBytes.Length);
                        loadedKernel(rawBytes.Length, gpuInputBuffer.View, gpuOutputBuffer.View);
                        accelerator.Synchronize();
                        byte[] gpuResultBytes = gpuOutputBuffer.GetAsArray1D();
                        string tempOutPath = Path.Combine(gpuTempFolder, relativePath);
                        string? tempOutDir = Path.GetDirectoryName(tempOutPath);

                        if (!Directory.Exists(tempOutDir!)) Directory.CreateDirectory(tempOutDir!);

                        File.WriteAllBytes(tempOutPath, gpuResultBytes);
                    }
                    DrawProgressBar("Encrypting Files", i + 1, totalFiles);
                }
                Console.WriteLine();
                Console.WriteLine("Step 2/2: Archiving to Zip");

                using (var def = new DeflateZipFile(filepath))
                {
                    for (int i = 0; i < totalFiles; i++)
                    {
                        string file = allFiles[i];
                        string fileName = Path.GetFileName(file);
                        string folderName = Path.GetFileNameWithoutExtension(file);
                        string relativePath = folderName + "/" + fileName;
                        string gpuProcessedFilePath = Path.Combine(gpuTempFolder, relativePath);

                        if (File.Exists(gpuProcessedFilePath)) def.Compress(gpuProcessedFilePath, relativePath);

                        DrawProgressBar("Archiving Files", i + 1, totalFiles);
                    }
                    def.Save();
                }
                Console.WriteLine(); 

                try { Directory.Delete(gpuTempFolder, true); } catch { }

                Console.WriteLine("-> GPU compression completed successfully!");
            }
            else if (isDirectory)
            {
                var loadedKernel = accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<byte>, ArrayView<byte>>(MyGpuProcessingKernel);
                string[] allFiles = Directory.GetFiles(targetToCompress, "*.*", SearchOption.AllDirectories);
                string safeFolderPath = targetToCompress.EndsWith("\\") ? targetToCompress : targetToCompress + "\\";
                Uri folderUri = new Uri(safeFolderPath);
                string? projectDir = Path.GetDirectoryName(filepath);
                string gpuTempFolder = Path.Combine(projectDir!, "GpuTempCompress");
                if (Directory.Exists(gpuTempFolder)) Directory.Delete(gpuTempFolder, true);

                Directory.CreateDirectory(gpuTempFolder);
                int totalFiles = allFiles.Length;

                Console.WriteLine("Step 1/2: GPU Hardware Encryption");

                for (int i = 0; i < totalFiles; i++)
                {
                    string file = allFiles[i];
                    Uri fileUri = new Uri(file);
                    string relativePath = Uri.UnescapeDataString(folderUri.MakeRelativeUri(fileUri).ToString());
                    relativePath = relativePath.Replace("\\", "/");
                    byte[] rawBytes = File.ReadAllBytes(file);

                    if (rawBytes.Length > 0)
                    {
                        using var gpuInputBuffer = accelerator.Allocate1D<byte>(rawBytes);
                        using var gpuOutputBuffer = accelerator.Allocate1D<byte>(rawBytes.Length);
                        loadedKernel(rawBytes.Length, gpuInputBuffer.View, gpuOutputBuffer.View);
                        accelerator.Synchronize();
                        byte[] gpuResultBytes = gpuOutputBuffer.GetAsArray1D();
                        string tempOutPath = Path.Combine(gpuTempFolder, relativePath);
                        string? tempOutDir = Path.GetDirectoryName(tempOutPath);

                        if (!Directory.Exists(tempOutDir!)) Directory.CreateDirectory(tempOutDir!);

                        File.WriteAllBytes(tempOutPath, gpuResultBytes);
                    }
                    DrawProgressBar("Encrypting Files", i + 1, totalFiles);
                }
                Console.WriteLine(); 
                Console.WriteLine("Step 2/2: Archiving to Zip");

                using (var def = new DeflateZipFile(filepath))
                {
                    for (int i = 0; i < totalFiles; i++)
                    {
                        string file = allFiles[i];
                        Uri fileUri = new Uri(file);
                        string relativePath = Uri.UnescapeDataString(folderUri.MakeRelativeUri(fileUri).ToString());
                        relativePath = relativePath.Replace("\\", "/");
                        string gpuProcessedFilePath = Path.Combine(gpuTempFolder, relativePath);

                        if (File.Exists(gpuProcessedFilePath)) def.Compress(gpuProcessedFilePath, relativePath);

                        DrawProgressBar("Archiving Files", i + 1, totalFiles);
                    }
                    def.Save();
                }
                Console.WriteLine(); 

                try { Directory.Delete(gpuTempFolder, true); } catch { }

                Console.WriteLine("-> GPU compression completed successfully!");
            }
        }



        static void decompressWithGPU(string filepath, string tempunzip, Accelerator accelerator)
        {
            Console.WriteLine("\n--- [GPU Parallel] Decompression Started ---");

            if (!File.Exists(filepath)) return;

            string? projectDir = Path.GetDirectoryName(filepath);
            string gpuTempFolder = Path.Combine(projectDir!, "GpuUnzip");

            if (Directory.Exists(gpuTempFolder)) Directory.Delete(gpuTempFolder, true);

            Directory.CreateDirectory(gpuTempFolder);

            Console.WriteLine("Step 1/2: Extracting Files to Workspace");

            using (var inf = new InflateZipFile(filepath))
            {
                var entries = inf.ZipDir.Cast<object>().ToArray();
                int totalEntries = entries.Length;

                for (int i = 0; i < totalEntries; i++)
                {
                    dynamic entry = entries[i];

                    try { inf.Decompress(entry, gpuTempFolder, null, true, true); } catch { }

                    DrawProgressBar("Extracting Zip", i + 1, totalEntries);
                }
            }
            Console.WriteLine();
            Console.WriteLine("Step 2/2: GPU Hardware Decryption");

            var loadedKernel = accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<byte>, ArrayView<byte>>(MyGpuProcessingKernel);
            string[] extractedFiles = Directory.GetFiles(gpuTempFolder, "*.*", SearchOption.AllDirectories);
            string safeTempPath = gpuTempFolder.EndsWith("\\") ? gpuTempFolder : gpuTempFolder + "\\";
            Uri tempFolderUri = new Uri(safeTempPath);
            int totalExtracted = extractedFiles.Length;

            for (int i = 0; i < totalExtracted; i++)
            {
                string file = extractedFiles[i];
                Uri fileUri = new Uri(file);
                string relativePath = Uri.UnescapeDataString(tempFolderUri.MakeRelativeUri(fileUri).ToString());
                byte[] encryptedBytes = File.ReadAllBytes(file);

                if (encryptedBytes.Length > 0)
                {
                    using var gpuInputBuffer = accelerator.Allocate1D<byte>(encryptedBytes);
                    using var gpuOutputBuffer = accelerator.Allocate1D<byte>(encryptedBytes.Length);
                    loadedKernel(encryptedBytes.Length, gpuInputBuffer.View, gpuOutputBuffer.View);
                    accelerator.Synchronize();
                    byte[] restoredBytes = gpuOutputBuffer.GetAsArray1D();
                    string finalOutPath = Path.Combine(tempunzip, relativePath);
                    string? finalOutDir = Path.GetDirectoryName(finalOutPath);

                    if (!Directory.Exists(finalOutDir!)) Directory.CreateDirectory(finalOutDir!);

                    File.WriteAllBytes(finalOutPath, restoredBytes);
                }
                DrawProgressBar("Decrypting Files", i + 1, totalExtracted);
            }
            Console.WriteLine();

            try { Directory.Delete(gpuTempFolder, true); } catch { }

            Console.WriteLine($"-> GPU decompression completed! Restored files are in: {tempunzip}");
        }

    }

}