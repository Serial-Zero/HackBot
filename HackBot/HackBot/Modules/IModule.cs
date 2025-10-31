using System;
using System.Threading.Tasks;

namespace HackBot.Modules
{
    public interface IModule
    {
        string Name { get; }
        string Description { get; }
        Task ExecuteAsync();
    }
}
