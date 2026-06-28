using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

class FolderComparer
{
    static void Main()
    {
        string originalFolder = @"D:\Study File\degree\Sem5\TPC6323\project
                                \dataset\Fruits and Vegetables Image Recognition Dataset";
        string extractedFolder = @"D:\Study File\degree\Sem5\TPC6323\project
                                \dataset\test\deflate\Fruits and Vegetables Image Recognition Dataset";

        
        bool result = CompareFolders(originalFolder, extractedFolder);



        Console.WriteLine("\n\n\nOriginal Folder/File : "+ originalFolder);
        Console.WriteLine("Extracted Folder/File : "+ extractedFolder);

        Console.WriteLine("\nAfter testing, the results showed : ");
        Console.WriteLine(result
            ? "folders are IDENTICAL."
            : "folders are DIFFERENT.");
    }

    static bool CompareFolders(string folder1, string folder2)
    {
        var files1 = Directory.GetFiles(folder1, "*", SearchOption.AllDirectories)
                              .Select(f => f.Substring(folder1.Length))
                              .OrderBy(f => f)
                              .ToList();

        var files2 = Directory.GetFiles(folder2, "*", SearchOption.AllDirectories)
                              .Select(f => f.Substring(folder2.Length))
                              .OrderBy(f => f)
                              .ToList();

        if (files1.Count != files2.Count)
        {
            Console.WriteLine("Different number of files.");
            return false;
        }

        for (int i = 0; i < files1.Count; i++)
        {
            if (files1[i] != files2[i])
            {
                Console.WriteLine($"Different file name:");
                Console.WriteLine($"{files1[i]}");
                Console.WriteLine($"{files2[i]}");
                return false;
            }
        }

        foreach (var relativePath in files1)
        {
            string file1 = Path.Combine(folder1, relativePath.TrimStart('\\'));
            string file2 = Path.Combine(folder2, relativePath.TrimStart('\\'));

            string hash1 = ComputeSHA256(file1);
            string hash2 = ComputeSHA256(file2);

            if (hash1 != hash2)
            {
                Console.WriteLine($"Mismatch found: {relativePath}");
                return false;
            }

            Console.WriteLine($"Verified: {relativePath}");
        }

        return true;
    }

    static string ComputeSHA256(string filePath)
    {
        using (var sha = SHA256.Create())
        using (var stream = File.OpenRead(filePath))
        {
            byte[] hash = sha.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "");
        }
    }
}