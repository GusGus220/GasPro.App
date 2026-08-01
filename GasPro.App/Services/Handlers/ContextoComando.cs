using System;      // 🚀 VITAL: Para que reconozca MainWindow.EstadoIA   
using GasPro.Services;

namespace GasPro.App.Services.Handlers
{
    public class ContextoComando
    {
        public string Comando { get; set; }
        public PiperSpeechService Speech { get; set; }
        public WindowsControlService Windows { get; set; }

        // 👇 AQUÍ ESTÁ LA MAGIA, PONLE EL PREFIJO MainWindow.
        public Action<MainWindow.EstadoIA> CambiarEstado { get; set; }
    }

    public interface IComandoHandler
    {
        bool PuedeManejar(string comando);
        void Ejecutar(ContextoComando contexto);
    }
}