using System.Threading.Tasks;
using ProseFlow.Core.Interfaces.Os;

namespace ProseFlow.UI.Services.ActiveWindow;

public class DefaultActiveWindowTracker : IActiveWindowService
{
    public Task<string> GetActiveWindowProcessNameAsync()
    {
        return Task.FromResult("unknown");
    }
}