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

        public bool CanHandle(string command) =>
        command.Contains("pausa") || command.Contains("reanuda") || command.Contains("reproduce") ||
        command.Contains("siguiente") || command.Contains("retrocede") || command.Contains("anterior");

        public async Task HandleAsync(string command)
        {
            _speech.SpeakAsync("Hecho.");

            if (command.Contains("siguiente"))
            {
                _windows.NextMusic(); // Cambia esto por tu método real
            }
            else if (command.Contains("retrocede") || command.Contains("anterior"))
            {
                _windows.PreviousMusic(); // Primer toque (reinicia la canción)

                // Pausa de 100 milisegundos para que el sistema registre bien el segundo toque
                await Task.Delay(100);

                _windows.PreviousMusic(); // Segundo toque (pasa a la canción anterior)
            }
            else
            {
                // Si no es siguiente ni anterior, es pausa/reproduce
                _windows.PlayPauseMusic();
            }
        }
    }
}