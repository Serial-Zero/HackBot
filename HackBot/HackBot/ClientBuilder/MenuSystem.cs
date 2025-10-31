using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HackBot.Modules;

namespace HackBot.ClientBuilder
{
    public class MenuSystem
    {
        private List<ModuleOption> _options;
        private int _selectedIndex = 0;
        private bool _debugEnabled = true;
        private bool _webhookEnabled = false;

        public MenuSystem(List<IModule> modules)
        {
            _options = modules.Select(m => new ModuleOption 
            { 
                Module = m, 
                Selected = false 
            }).ToList();
        }

        public async Task<(HashSet<string> SelectedModules, bool DebugEnabled, bool WebhookEnabled)> ShowMenuAsync()
        {
            ConsoleKey key;
            do
            {
                Console.Clear();
                RenderMenu();
                
                var keyInfo = Console.ReadKey(true);
                key = keyInfo.Key;

                switch (key)
                {
                    case ConsoleKey.UpArrow:
                        if (_selectedIndex > 0)
                            _selectedIndex--;
                        break;
                    case ConsoleKey.DownArrow:
                        if (_selectedIndex < _options.Count + 1)
                            _selectedIndex++;
                        break;
                    case ConsoleKey.Spacebar:
                        if (_selectedIndex == _options.Count)
                        {
                            _debugEnabled = !_debugEnabled;
                            if (_debugEnabled && _webhookEnabled)
                            {
                                _webhookEnabled = false;
                            }
                        }
                        else if (_selectedIndex == _options.Count + 1)
                        {
                            _webhookEnabled = !_webhookEnabled;
                            if (_webhookEnabled && _debugEnabled)
                            {
                                _debugEnabled = false;
                            }
                        }
                        else
                        {
                            _options[_selectedIndex].Selected = !_options[_selectedIndex].Selected;
                        }
                        break;
                }
            } while (key != ConsoleKey.Enter);

            return (_options
                .Where(opt => opt.Selected)
                .Select(opt => opt.Module.Name)
                .ToHashSet(), _debugEnabled, _webhookEnabled);
        }

        private void RenderMenu()
        {
            Console.WriteLine("Select modules to include (use arrow keys, space to toggle, Enter to confirm):\n");
            
            for (int i = 0; i < _options.Count; i++)
            {
                var option = _options[i];
                var checkbox = option.Selected ? "[✓]" : "[ ]";
                var indicator = i == _selectedIndex ? "> " : "  ";
                
                Console.ForegroundColor = i == _selectedIndex ? ConsoleColor.Yellow : ConsoleColor.White;
                Console.WriteLine($"{indicator}{checkbox} {option.Module.Name} - {option.Module.Description}");
                Console.ResetColor();
            }
            
            if (_options.Count > 0)
            {
                Console.WriteLine();
            }
            
            var debugCheckbox = _debugEnabled ? "[✓]" : "[ ]";
            var debugIndicator = _selectedIndex == _options.Count ? "> " : "  ";
            Console.ForegroundColor = _selectedIndex == _options.Count ? ConsoleColor.Yellow : ConsoleColor.White;
            Console.WriteLine($"{debugIndicator}{debugCheckbox} Debug - Enable debug output");
            Console.ResetColor();
            
            var webhookCheckbox = _webhookEnabled ? "[✓]" : "[ ]";
            var webhookIndicator = _selectedIndex == _options.Count + 1 ? "> " : "  ";
            Console.ForegroundColor = _selectedIndex == _options.Count + 1 ? ConsoleColor.Yellow : ConsoleColor.White;
            Console.WriteLine($"{webhookIndicator}{webhookCheckbox} Webhook - Send data to Discord webhook");
            Console.ResetColor();
        }

        private class ModuleOption
        {
            public IModule Module { get; set; }
            public bool Selected { get; set; }
        }
    }
}
