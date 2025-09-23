using System.Threading.Tasks;
using ProseFlow.Core.Interfaces;

namespace ProseFlow.UI.Services.ActiveWindow;

public class DefaultActiveWindowTracker : IActiveWindowTracker
{
    public Task<string> GetActiveWindowProcessNameAsync()
    {
        return Task.FromResult("unknown");
    }
}