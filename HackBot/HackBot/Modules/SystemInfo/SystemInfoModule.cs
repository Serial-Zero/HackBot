using System;
using System.Threading.Tasks;

namespace HackBot.Modules
{
    public class SystemInfoModule : IModule
    {
        public string Name => "SystemInfo";
        public string Description => "Collect and display system information";

        public async Task ExecuteAsync()
        {
            try
            {
                var info = new System.Text.StringBuilder();
                
                info.AppendLine($"OS Version: {Environment.OSVersion}");
                info.AppendLine($"Machine Name: {Environment.MachineName}");
                info.AppendLine($"User Name: {Environment.UserName}");
                info.AppendLine($"User Domain Name: {Environment.UserDomainName}");
                info.AppendLine($"Processor Count: {Environment.ProcessorCount}");
                info.AppendLine($"System Directory: {Environment.SystemDirectory}");
                info.AppendLine($"Current Directory: {Environment.CurrentDirectory}");
                info.AppendLine($"CLR Version: {Environment.Version}");
                info.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
                info.AppendLine($"64-bit Process: {Environment.Is64BitProcess}");
                
                Console.WriteLine(info.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error collecting system info: {ex.Message}");
            }
        }
    }
}
