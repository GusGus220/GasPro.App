using System.Threading.Tasks;
using GasPro.Services;

namespace GasPro.App.Services.Handlers
{
    public class ClickHandler : ISystemCommandHandler
    {
        private readonly PiperSpeechService _speech;
        private readonly WindowsControlService _windows;

        public ClickHandler(PiperSpeechService speech, WindowsControlService windows)
        {
            _speech = speech;
            _windows = windows;
        }

        public bool CanHandle(string command) => command.Contains("haz clic") || command.Contains("haz click");

        public Task HandleAsync(string command)
        {
            _speech.SpeakAsync("Clic hecho.");
            _windows.LeftClick();
            return Task.CompletedTask;
        }
    }
}