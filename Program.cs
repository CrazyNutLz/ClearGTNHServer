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
            // 从配置文件中读取目录和条件
            string configFilePath = "config.txt";
            if (!File.Exists(configFilePath))
            {
                Console.WriteLine($"Configuration file {configFilePath} does not exist.");
                return;
            }

            var configLines = File.ReadAllLines(configFilePath);

            foreach (var line in configLines)
            {
                if (string.IsNullOrWhiteSpace(line) || !line.Contains("||"))
                {
                    Console.WriteLine($"Invalid configuration line, skipping: {line}");
                    continue;
                }

                var parts = line.Split(new[] { "||" }, 2, StringSplitOptions.None);
                var directoryPath = parts[0].Trim();

                if (!Directory.Exists(directoryPath))
                {
                    Console.WriteLine($"Directory does not exist, skipping: {directoryPath}");
                    continue;
                }

                var criteria = parts[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (criteria.Length == 0)
                {
                    Console.WriteLine($"No valid criteria provided for directory, skipping: {directoryPath}");
                    continue;
                }

                Console.WriteLine($"Searching in: {directoryPath} with criteria: {string.Join(", ", criteria)}");

                await RemoveFilesAsync(directoryPath, criteria);
            }

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


    static async Task RemoveFilesAsync(string directoryPath, params string[] criteria)
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
                    bool matchesCriteria = true;

                    foreach (var criterion in criteria)
                    {
                        if (!fileName.Contains(criterion))
                        {
                            matchesCriteria = false;
                            break;
                        }
                    }

                    if (matchesCriteria)
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        long size = fileInfo.Length;

                        File.Delete(file);

                        Interlocked.Increment(ref deletedFileCount);
                        Interlocked.Add(ref totalSize, size);

                        double sizeKB = size / 1024.0;
                        Console.WriteLine($"Deleted: {file} (Size: {sizeKB:F2} KB)");
                    }
                }));
            }

            var subdirectories = Directory.GetDirectories(directoryPath);
            foreach (var subdirectory in subdirectories)
            {
                tasks.Add(RemoveFilesAsync(subdirectory, criteria));
            }

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred in directory {directoryPath}: {ex.Message}");
        }
    }
}
