using System.Threading.Tasks;
using GasPro.Services;

namespace GasPro.App.Services.Handlers
{
    public class VolumeHandler : ISystemCommandHandler
    {
        private readonly PiperSpeechService _speech;
        private readonly WindowsControlService _windows;

        public VolumeHandler(PiperSpeechService speech, WindowsControlService windows)
        {
            _speech = speech;
            _windows = windows;
        }

        public bool CanHandle(string command) => command.Contains("volumen");

        public Task HandleAsync(string command)
        {
            int? target = ParseVolumeFromCommand(command);
            if (target.HasValue)
            {
                _speech.SpeakAsync($"Ajustando el volumen al {target.Value} por ciento.");
                _windows.SetVolume(target.Value);
            }
            else if (command.Contains("baj") || command.Contains("disminu"))
            {
                _speech.SpeakAsync("Bajando el volumen.");
                _windows.ChangeVolumeBy(-20);
            }
            else if (command.Contains("sub") || command.Contains("aument"))
            {
                _speech.SpeakAsync("Subiendo el volumen.");
                _windows.ChangeVolumeBy(20);
            }

            return Task.CompletedTask;
        }

        private int? ParseVolumeFromCommand(string comando)
        {
            int targetVolume = -1;
            string[] palabras = comando.Split(' ');

            foreach (var palabra in palabras)
            {
                string numStr = palabra.Replace("%", "").Trim();
                if (int.TryParse(numStr, out int num)) { targetVolume = num; break; }

                switch (numStr)
                {
                    case "cero": targetVolume = 0; break;
                    case "diez": targetVolume = 10; break;
                    case "veinte": targetVolume = 20; break;
                    case "treinta": targetVolume = 30; break;
                    case "cuarenta": targetVolume = 40; break;
                    case "cincuenta": targetVolume = 50; break;
                    case "sesenta": targetVolume = 60; break;
                    case "setenta": targetVolume = 70; break;
                    case "ochenta": targetVolume = 80; break;
                    case "noventa": targetVolume = 90; break;
                    case "cien":
                    case "ciento": targetVolume = 100; break;
                }
            }

            return targetVolume == -1 ? (int?)null : targetVolume;
        }
    }
}
