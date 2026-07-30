using System.Threading.Tasks;
using GasPro.Services;

namespace GasPro.App.Services.Handlers
{
    public class MediaHandler : ISystemCommandHandler
    {
        private readonly PiperSpeechService _speech;
        private readonly WindowsControlService _windows;

        public MediaHandler(PiperSpeechService speech, WindowsControlService windows)
        {
            _speech = speech;
            _windows = windows;
        }

        public bool CanHandle(string command) => command.Contains("pausa") || command.Contains("reanuda") || command.Contains("reproduce");

        public Task HandleAsync(string command)
        {
            _speech.SpeakAsync("Hecho.");
            _windows.PlayPauseMusic();
            return Task.CompletedTask;
        }
    }
}