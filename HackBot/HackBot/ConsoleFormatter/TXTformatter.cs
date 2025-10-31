using System;
using System.Text;

namespace ConsoleFormatter
{
    public class TXTformatter
    {
        public static string Format(string content)
        {
            var sb = new StringBuilder();
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            
            var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (!string.IsNullOrEmpty(trimmedLine))
                {
                    sb.AppendLine($"[{timestamp}] [>] | {trimmedLine}");
                }
            }
            
            return sb.ToString();
        }
    }
}
