using System.Threading.Tasks;
using GasPro.Services;

namespace GasPro.App.Services.Handlers
{
    public class BrowserHandler : ISystemCommandHandler
    {
        private readonly PiperSpeechService _speech;
        private readonly WindowsControlService _windows;

        public BrowserHandler(PiperSpeechService speech, WindowsControlService windows)
        {
            _speech = speech;
            _windows = windows;
        }

        public bool CanHandle(string command) => command.Contains("chrome") || command.Contains("google") || command.Contains("navegador");

        public Task HandleAsync(string command)
        {
            _speech.SpeakAsync("Abriendo el navegador.");
            _windows.OpenApplication("https://www.google.com");
            return Task.CompletedTask;
        }
    }
}