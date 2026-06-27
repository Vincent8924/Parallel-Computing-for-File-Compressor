using System;
using System.IO;

namespace UZipDotNet.ConsoleApp.net452
{
    class Program
    {




        static void Main(string[] args)
        {
            //string temp = args.Length > 0 ? args[0] : @"c:\temp";
            //string tempunzip = Path.Combine(temp, "Users\\lojinkai\\Downloads\\UZipDotNet-master\\test"); 
            //string filepath = Path.Combine(temp, "Users\\lojinkai\\Downloads\\UZipDotNet-master\\");

            string tempunzip = @"C:\Users\lojinkai\Downloads\UZipDotNet-master\test";
            string filepath = @"C:\Users\lojinkai\Downloads\UZipDotNet-master\test.zip";

            compress(filepath);
            //de(filepath, tempunzip);

        }

        //static void compress(string filepath) {
        //    Console.WriteLine("start");
        //    try
        //    {
        //        File.Delete(filepath);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Ignore : " + ex);
        //    }

        //    using (var def = new DeflateZipFile(filepath))
        //    {
        //        // compress files
        //        def.Compress("../UZipDotNet/DeflateZipFile.cs", "NewFileName.txt");
        //        def.Compress("../UZipDotNet/DeflateMethod.cs", "DeflateMethod.cs");

        //        // save archive
        //        def.Save();
        //    }

        //}


        static void compress(string filepath)
        {
            // 这是你要压缩的整个源文件夹路径
            string folderToCompress = @"C:\Users\lojinkai\Downloads\UZipDotNet-master\test";

            Console.WriteLine("start");
            try
            {
                if (File.Exists(filepath))
                {
                    File.Delete(filepath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ignore : " + ex);
            }

            if (Directory.Exists(folderToCompress))
            {
                using (var def = new DeflateZipFile(filepath))
                {
                    // 1. 自动获取文件夹下的所有文件
                    string[] allFiles = Directory.GetFiles(folderToCompress, "*.*", SearchOption.AllDirectories);

                    // 用 Uri 来安全计算相对路径（适配你目前的 .NET 4.5.2）
                    string safeFolderPath = folderToCompress.EndsWith("\\") ? folderToCompress : folderToCompress + "\\";
                    Uri folderUri = new Uri(safeFolderPath);

                    // 2. 靠这个循环自动搞定所有文件，不需要再手动写任何单个文件了！
                    foreach (string file in allFiles)
                    {
                        Uri fileUri = new Uri(file);
                        string relativePath = Uri.UnescapeDataString(folderUri.MakeRelativeUri(fileUri).ToString());
                        relativePath = relativePath.Replace("\\", "/");

                        Console.WriteLine($"Compressing: {relativePath}");

                        // 核心：全自动打包
                        def.Compress(file, relativePath);
                    }

                    // 3. 统一保存
                    def.Save();
                }
                Console.WriteLine("Folder compression completed successfully!");
            }
            else
            {
                Console.WriteLine($"Error: Folder not found: {folderToCompress}");
            }
        }


        static void de(string filepath, string tempunzip)
        {
            // un-compress files
            using (var inf = new InflateZipFile(filepath))
            {
                inf.Decompress(inf.ZipDir[0], tempunzip, null, true, true);
                inf.Decompress(inf.ZipDir[1], tempunzip, null, true, true);
            }
            Console.WriteLine("end");

        }

            
        }
    }
