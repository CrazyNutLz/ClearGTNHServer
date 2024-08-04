using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

class Program
{
    static int deletedFileCount = 0;
    static long totalSize = 0;
    static int directoryCount = 0;

    static async Task Main()
    {
        Stopwatch stopwatch = Stopwatch.StartNew(); // 创建并启动一个Stopwatch实例

        try
        {
            string currentDirectory = @"G:\0---------服务端---------0\GT_New_Horizons_2.3.0_Server\World";
            Console.WriteLine($"Searching in: {currentDirectory}");
            await RemoveFilesAsync(currentDirectory);

            double totalSizeMB = totalSize / (1024.0 * 1024.0);
            Console.WriteLine($"Deleted {deletedFileCount} files with a total size of {totalSizeMB:F2} MB.");
            Console.WriteLine($"Searched through {directoryCount} directories."); // 输出搜索过的文件夹数量
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
        finally
        {
            stopwatch.Stop(); // 停止计时
            Console.WriteLine($"Time elapsed: {stopwatch.Elapsed}"); // 输出运行时间
            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }
        
    }

    static async Task RemoveFilesAsync(string directoryPath)
    {
        try
        {
            Interlocked.Increment(ref directoryCount); // 更新文件夹数量

            var files = Directory.GetFiles(directoryPath);
            var tasks = new List<Task>();

            foreach (var file in files)
            {
                tasks.Add(Task.Run(() =>
                {
                    string fileName = Path.GetFileName(file);
                    if (fileName.Contains("inventory-") && fileName.Contains("-death-") && fileName.Contains(".dat"))
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        long size = fileInfo.Length;

                        File.Delete(file);

                        Interlocked.Increment(ref deletedFileCount);
                        Interlocked.Add(ref totalSize, size);

                        double sizeKB = size / (1024.0);
                        Console.WriteLine($"Deleted: {file} (Size: {sizeKB:F2} KB)");
                    }
                }));
            }

            var subdirectories = Directory.GetDirectories(directoryPath);
            foreach (var subdirectory in subdirectories)
            {
                tasks.Add(RemoveFilesAsync(subdirectory));
            }

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred in directory {directoryPath}: {ex.Message}");
        }
    }
}
