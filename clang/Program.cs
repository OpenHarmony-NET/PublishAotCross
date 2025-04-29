using System.Diagnostics;
using System.Runtime.InteropServices;

// 检查 zig 是否在 PATH 中
if (!IsCommandAvailable("zig"))
{
    Console.Error.WriteLine("Error: zig is not on the PATH. Install zig and make sure it's on PATH. Follow instructions at https://github.com/MichalStrehovsky/PublishAotCross.");
    Environment.Exit(1);
}

// 获取传入的参数
var argsList = args.ToList();

// 处理 .NET 8 Preview 6 的问题
argsList = argsList.Select(arg => arg.Replace("'-Wl,-rpath,$ORIGIN'", "-Wl,-rpath,$ORIGIN")).ToList();

// 处理 zig 不支持的参数，直接从命令行中移除
argsList = argsList.Select(arg => arg.Replace("--discard-all", "--as-needed"))
           .Select(arg => arg.Replace("-Wl,-pie", ""))
           .Select(arg => arg.Replace("-pie", ""))
           .Select(arg => arg.Replace("-Wl,-e0x0", ""))
           .ToList();

// 解决 zig 链接器丢弃可执行文件必要部分的问题
argsList.Insert(0, "-Wl,-u,__Module");

// 运行 zig cc
RunZigCC(argsList);

static bool IsCommandAvailable(string command)
{
    try
    {
        string checkCommand = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which";

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = checkCommand,
                Arguments = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        process.WaitForExit();
        return process.ExitCode == 0;
    }
    catch
    {
        return false;
    }
}

static void RunZigCC(List<string> arguments)
{
    try
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "zig",
                Arguments = "cc " + string.Join(" ", arguments),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();

        // 输出 zig cc 的结果
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Console.WriteLine(output);
        if (!string.IsNullOrEmpty(error))
        {
            Console.Error.WriteLine(error);
        }

        Environment.Exit(process.ExitCode);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error running zig cc: {ex.Message}");
        Environment.Exit(1);
    }
}