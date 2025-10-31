using System;
using System.Diagnostics;
using System.IO;

namespace HackBot.ClientBuilder
{
    public class Compiler
    {
        public bool CompileToExe(string sourceCode, string outputPath, bool debugEnabled = false)
        {
            try
            {
                var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);

                var programPath = Path.Combine(tempDir, "Program.cs");
                File.WriteAllText(programPath, sourceCode);

                var projectPath = Path.Combine(tempDir, "Client.csproj");
                var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  </PropertyGroup>
</Project>";
                File.WriteAllText(projectPath, projectContent);

                var outputDir = Path.GetDirectoryName(outputPath);
                if (string.IsNullOrEmpty(outputDir))
                {
                    outputDir = Directory.GetCurrentDirectory();
                }
                else
                {
                    var fullOutputPath = Path.GetFullPath(outputPath);
                    outputDir = Path.GetDirectoryName(fullOutputPath);
                    if (string.IsNullOrEmpty(outputDir))
                        outputDir = Directory.GetCurrentDirectory();
                }

                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                if (debugEnabled)
                {
                    Console.WriteLine($"=== COMPILER DEBUG INFO ===");
                    Console.WriteLine($"Temp directory: {tempDir}");
                    Console.WriteLine($"Output directory: {outputDir}");
                    Console.WriteLine($"Final executable path: {Path.ChangeExtension(outputPath, ".exe")}");
                    Console.WriteLine();
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"publish \"{projectPath}\" -c Release -o \"{outputDir}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    Directory.Delete(tempDir, true);
                    return false;
                }

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    if (debugEnabled)
                    {
                        Console.WriteLine($"Build failed with exit code {process.ExitCode}");
                        Console.WriteLine($"Build errors:\n{error}");
                        if (!string.IsNullOrEmpty(output))
                            Console.WriteLine($"Build output:\n{output}");
                    }
                    Directory.Delete(tempDir, true);
                    return false;
                }

                if (debugEnabled)
                {
                    Console.WriteLine($"Build succeeded. Output directory: {outputDir}");
                    if (!string.IsNullOrEmpty(output))
                        Console.WriteLine($"Build output:\n{output}");
                }

                var finalExe = Path.ChangeExtension(outputPath, ".exe");
                
                var possibleExePaths = new[]
                {
                    Path.Combine(outputDir, "Client.exe"),
                    Path.Combine(outputDir, "Client.dll"),
                    Path.Combine(tempDir, "bin", "Release", "net9.0", "win-x64", "publish", "Client.exe"),
                    Path.Combine(tempDir, "bin", "Release", "net9.0", "win-x64", "publish", "Client.dll"),
                    Path.Combine(tempDir, "bin", "Release", "net9.0", "win-x64", "Client.exe"),
                    Path.Combine(tempDir, "bin", "Release", "net9.0", "win-x64", "Client.dll"),
                    Path.Combine(tempDir, "bin", "Release", "net9.0", "Client.exe"),
                    Path.Combine(tempDir, "bin", "Release", "net9.0", "Client.dll")
                };

                if (debugEnabled)
                {
                    Console.WriteLine($"Searching for compiled executable...");
                    foreach (var path in possibleExePaths)
                    {
                        var exists = File.Exists(path);
                        Console.WriteLine($"  Checking: {path} (exists: {exists})");
                        if (exists)
                        {
                            var fileInfo = new FileInfo(path);
                            Console.WriteLine($"    Size: {fileInfo.Length} bytes");
                        }
                    }
                }

                string foundExe = null;
                foreach (var possiblePath in possibleExePaths)
                {
                    if (File.Exists(possiblePath))
                    {
                        foundExe = possiblePath;
                        if (debugEnabled)
                            Console.WriteLine($"Found executable at: {foundExe}");
                        break;
                    }
                }

                if (foundExe == null)
                {
                    if (debugEnabled)
                    {
                        Console.WriteLine($"\nCould not find compiled executable.");
                        Console.WriteLine($"Output directory contents:");
                        try
                        {
                            if (Directory.Exists(outputDir))
                            {
                                foreach (var file in Directory.GetFiles(outputDir))
                                {
                                    Console.WriteLine($"  - {file}");
                                }
                            }
                            if (Directory.Exists(Path.Combine(tempDir, "bin", "Release", "net9.0", "win-x64")))
                            {
                                Console.WriteLine($"\nTemp build directory contents:");
                                foreach (var file in Directory.GetFiles(Path.Combine(tempDir, "bin", "Release", "net9.0", "win-x64")))
                                {
                                    Console.WriteLine($"  - {file}");
                                }
                            }
                        }
                        catch (Exception dirEx)
                        {
                            Console.WriteLine($"Error listing directory: {dirEx.Message}");
                        }
                    }
                    Directory.Delete(tempDir, true);
                    return false;
                }

                if (!File.Exists(foundExe))
                {
                    if (debugEnabled)
                        Console.WriteLine($"Error: Found executable path is invalid: {foundExe}");
                    Directory.Delete(tempDir, true);
                    return false;
                }

                if (Path.GetFullPath(foundExe).Equals(Path.GetFullPath(finalExe), StringComparison.OrdinalIgnoreCase))
                {
                    if (debugEnabled)
                        Console.WriteLine($"Source and destination are the same. Skipping copy.");
                    Directory.Delete(tempDir, true);
                    return true;
                }

                if (File.Exists(finalExe))
                    File.Delete(finalExe);
                
                if (debugEnabled)
                    Console.WriteLine($"Copying {foundExe} to {finalExe}");
                
                if (!File.Exists(foundExe))
                {
                    if (debugEnabled)
                        Console.WriteLine($"Error: Source file no longer exists: {foundExe}");
                    Directory.Delete(tempDir, true);
                    return false;
                }
                
                if (foundExe.EndsWith(".dll"))
                {
                    if (debugEnabled)
                        Console.WriteLine($"Warning: Found DLL instead of EXE. Creating a launcher batch file.");
                    
                    var batchFile = finalExe.Replace(".exe", ".bat");
                    var dllName = Path.GetFileName(finalExe.Replace(".exe", ".dll"));
                    var batchContent = $"@echo off\ncd /d \"%~dp0\"\ndotnet \"{dllName}\" %*\n";
                    
                    if (File.Exists(batchFile))
                        File.Delete(batchFile);
                    
                    File.WriteAllText(batchFile, batchContent);
                    
                    File.Copy(foundExe, finalExe.Replace(".exe", ".dll"), true);
                    
                    var runtimeConfigPath = foundExe.Replace(".dll", ".runtimeconfig.json");
                    var runtimeConfigDest = finalExe.Replace(".exe", ".runtimeconfig.json");
                    if (File.Exists(runtimeConfigPath))
                        File.Copy(runtimeConfigPath, runtimeConfigDest, true);
                    
                    var depsPath = foundExe.Replace(".dll", ".deps.json");
                    var depsDest = finalExe.Replace(".exe", ".deps.json");
                    if (File.Exists(depsPath))
                        File.Copy(depsPath, depsDest, true);
                    
                    if (debugEnabled)
                    {
                        Console.WriteLine($"Note: Created Client.bat launcher. Run Client.bat to execute the application.");
                        Console.WriteLine($"      The DLL file is Client.dll and requires .NET runtime to be installed.");
                    }
                }
                else
                {
                    File.Copy(foundExe, finalExe, true);
                }
                
                Directory.Delete(tempDir, true);
                return true;
            }
            catch (Exception ex)
            {
                if (debugEnabled)
                {
                    Console.WriteLine($"Compilation error: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
                return false;
            }
        }
    }
}
