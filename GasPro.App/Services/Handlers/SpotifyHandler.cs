using System.Threading.Tasks;
using GasPro.Services;

namespace GasPro.App.Services.Handlers
{
    public class SpotifyHandler : ISystemCommandHandler
    {
        private readonly PiperSpeechService _speech;
        private readonly WindowsControlService _windows;

        public SpotifyHandler(PiperSpeechService speech, WindowsControlService windows)
        {
            _speech = speech;
            _windows = windows;
        }

        public bool CanHandle(string command) => command.Contains("spotify");

        public Task HandleAsync(string command)
        {
            _speech.SpeakAsync("Abriendo Spotify.");
            _windows.OpenApplication("spotify:");
            return Task.CompletedTask;
        }
    }
}