using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Diagnostics;

namespace GasPro.App.Network
{
    // Esta clase es la antena que recibirá la señal desde tu celular por Wi-Fi
    public class JarvisHub : Hub
    {
        // El celular invocará este método mágicamente a través de la red
        public async Task EnviarComandoAudio(string audioBase64)
        {
            Debug.WriteLine("¡Señal de audio recibida desde el celular!");
            // Aquí luego conectaremos el audio con tu Vosk y el Orquestador
        }

        public override Task OnConnectedAsync()
        {
            Debug.WriteLine($"Nuevo dispositivo conectado a Jarvis: {Context.ConnectionId}");
            return base.OnConnectedAsync();
        }
    }
}