using System.Threading.Tasks;

namespace GasPro.App.Services.Handlers
{
    public interface ISystemCommandHandler
    {
        bool CanHandle(string command);
        Task HandleAsync(string command);
    }
}