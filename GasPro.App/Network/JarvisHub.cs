using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace GasPro.App.Network
{
    // Esta clase es la antena que recibirá la señal desde tu celular por Wi-Fi
    public class JarvisHub : Hub
    {
        // 🚨 EL PUENTE: Este evento avisará al resto del programa que llegó un audio
        public static event Action<string> OnComandoTacticoRecibido;

        public async Task EnviarComandoAudio(string audioBase64)
        {
            Debug.WriteLine("📡 SEÑAL DE AUDIO RECIBIDA DESDE EL CELULAR");

            try
            {
                byte[] audioBytes = Convert.FromBase64String(audioBase64);

                // Guardamos el audio con un nombre que se sobrescribe cada vez para no llenar el disco duro
                string tempFilePath = Path.Combine(Path.GetTempPath(), "comando_tactico.wav");
                await File.WriteAllBytesAsync(tempFilePath, audioBytes);

                Debug.WriteLine($"✅ Audio desempaquetado en: {tempFilePath}");

                // ⚡ DISPARAMOS LA ALARMA: Le pasamos la ruta del archivo a quien esté escuchando (El Cerebro)
                OnComandoTacticoRecibido?.Invoke(tempFilePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error al procesar el audio interceptado: {ex.Message}");
            }
        }
    }
}