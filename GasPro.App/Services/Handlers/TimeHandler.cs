using System;
using System.Threading.Tasks;
using GasPro.Services;

namespace GasPro.App.Services.Handlers
{
    public class TimeHandler : ISystemCommandHandler
    {
        private readonly PiperSpeechService _speech;

        public TimeHandler(PiperSpeechService speech)
        {
            _speech = speech;
        }

        public bool CanHandle(string command) => command.Contains("hora") || command.Contains("qué hora es") || command.Contains("que hora es");

        public Task HandleAsync(string command)
        {
            string horaFormateada = DateTime.Now.ToString("h:mm tt", new System.Globalization.CultureInfo("es-ES"))
                .Replace("AM", "de la mañana").Replace("PM", "de la tarde");
            string mensajeHora = $"Son las {horaFormateada}";
            _speech.SpeakAsync(mensajeHora);
            return Task.CompletedTask;
        }
    }
}