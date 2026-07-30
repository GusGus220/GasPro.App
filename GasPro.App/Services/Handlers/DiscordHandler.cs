using System.Threading.Tasks;
using GasPro.Services;

namespace GasPro.App.Services.Handlers
{
    public class DiscordHandler : ISystemCommandHandler
    {
        private readonly PiperSpeechService _speech;
        private readonly WindowsControlService _windows;

        public DiscordHandler(PiperSpeechService speech, WindowsControlService windows)
        {
            _speech = speech;
            _windows = windows;
        }

        public bool CanHandle(string command) => command.Contains("abre discord") || command.Contains("discord");

        public Task HandleAsync(string command)
        {
            _speech.SpeakAsync("Abriendo Discord.");
            _ = System.Threading.Tasks.Task.Run(() => _windows.OpenAppBySearch("discord"));
            return Task.CompletedTask;
        }
    }
}