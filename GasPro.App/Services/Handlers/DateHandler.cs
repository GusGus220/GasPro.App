using System;
using System.Threading.Tasks;
using GasPro.Services;

namespace GasPro.App.Services.Handlers
{
    public class DateHandler : ISystemCommandHandler
    {
        private readonly PiperSpeechService _speech;

        public DateHandler(PiperSpeechService speech)
        {
            _speech = speech;
        }

        public bool CanHandle(string command) => command.Contains("fecha") || command.Contains("día es hoy") || command.Contains("dia es hoy");

        public Task HandleAsync(string command)
        {
            string fechaFormateada = DateTime.Now.ToString("dddd, d 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-ES"));
            string mensajeFecha = $"Hoy es {fechaFormateada}";
            _speech.SpeakAsync(mensajeFecha);
            return Task.CompletedTask;
        }
    }
}