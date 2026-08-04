using GasPro.Services; // Servicios compartidos (PiperSpeechService, WindowsControlService)
using System;
using System.Threading.Tasks;

namespace GasPro.App.Services.Handlers
{
    public class OnOffHandler : ISystemCommandHandler
    {
        private readonly PiperSpeechService _speech;
        private readonly WindowsControlService _windows;

        // TU CONSTRUCTOR (Igualito al de tu SpotifyHandler)
        public OnOffHandler(PiperSpeechService speech, WindowsControlService windows)
        {
            _speech = speech;
            _windows = windows;
        }

        public bool CanHandle(string comando)
        {
            return comando.Contains("apaga el monitor") ||
                   comando.Contains("apaga la pantalla") ||
                   (comando.Contains("apágate en") && comando.Contains("minutos")) ||
                   comando.Contains("cancela el apagado") ||
                   comando.Contains("aborta el apagado") ||
                   comando.Contains("apaga el pc") || comando.Contains("apaga la computadora") || comando.Contains("apaga el sistema") ||
                   comando.Contains("modo dormir") || comando.Contains("apaga las luces") || comando.Contains("apaga la luz") ||
                   comando.Contains("reinicia el pc") || comando.Contains("reinicia la computadora") || comando.Contains("reinicia el sistema") || comando.Contains("reinicia") ||
                   comando.Contains("modo siesta") || comando.Contains("date un break") || comando.Contains("modo descanso") || comando.Contains("modo siesta");
        }

        public Task HandleAsync(string comando)
        {
            // A. Caso: Apagar Monitor
            if (comando.Contains("apaga el monitor") || comando.Contains("apaga la pantalla"))
            {
                _speech.SpeakAsync("Apagando los monitores, señor. Mueva el ratón para despertar la pantalla.");
                _windows.ApagarMonitor();
                return Task.CompletedTask;
            }

            // B. Caso: Cancelar Apagado
            else if (comando.Contains("cancela el apagado") || comando.Contains("aborta el apagado"))
            {
                _speech.SpeakAsync("Apagado automático cancelado, señor.");
                _windows.CancelarApagado();
                return Task.CompletedTask;
            }

            // C. Caso: Apagado Programado (Con Matemática)
            else if (comando.Contains("apágate en") && comando.Contains("minutos"))
            {
                string[] palabras = comando.Split(' ');
                int minutos = 0;

                foreach (var palabra in palabras)
                {
                    if (int.TryParse(palabra, out int num)) { minutos = num; break; }

                    if (palabra == "diez") minutos = 10;
                    if (palabra == "quince") minutos = 15;
                    if (palabra == "veinte") minutos = 20;
                    if (palabra == "treinta") minutos = 30;
                    if (palabra == "cuarenta") minutos = 40;
                    if (palabra == "cincuenta") minutos = 50;
                    if (palabra == "sesenta") minutos = 60;
                }

                if (minutos > 0)
                {
                    _speech.SpeakAsync($"Entendido. Programando el apagado del sistema en {minutos} minutos.");
                    _windows.ApagarPCProgramado(minutos);
                    return Task.CompletedTask;
                }
                else
                {
                    _speech.SpeakAsync("No entendí cuántos minutos dijo, señor. Por favor repita el comando.");
                    return Task.CompletedTask;
                }
            }

            else if (comando.Contains("apaga el pc") || comando.Contains("apaga la computadora") || comando.Contains("apaga el sistema"))
            {
                _speech.SpeakAsync("Apagando el sistema, señor. Hasta luego.");
                _windows.ApagarPCProgramado(0); // Apagado inmediato
                return Task.CompletedTask;
            }

            else if (comando.Contains("modo dormir") || comando.Contains("apaga las luces") || comando.Contains("apaga la luz"))
            {
                _speech.SpeakAsync("Activando modo dormir, señor. Hasta mañana.");
                _windows.ModoDormir();
                return Task.CompletedTask;
            }

            else if (comando.Contains("reinicia el pc") || comando.Contains("reinicia la computadora") || comando.Contains("reinicia el sistema") || comando.Contains("reinicia"))
            {
                _speech.SpeakAsync("Reiniciando el sistema, señor. Hasta luego.");
                _windows.ReiniciarPC();
                return Task.CompletedTask;
            }

            else if (comando.Contains("modo siesta") || comando.Contains("date un break") || comando.Contains("modo descanso"))
            {
                _speech.SpeakAsync("Activando modo siesta, señor. Hasta mañana.");
                _windows.ModoSiesta();
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
    }
}