using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;

namespace GasPro.Services
{
    public class WindowsControlService
    {
        // APIs nativas de Windows para teclado y mouse
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);


        // Constantes del Mouse
        private const uint MOUSEEVENTF_LEFTDOWN = 0x02;
        private const uint MOUSEEVENTF_LEFTUP = 0x04;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const uint MOUSEEVENTF_RIGHTUP = 0x10;

        private const byte VK_VOLUME_MUTE = 0xAD;
        private const byte VK_MEDIA_PLAY_PAUSE = 0xB3;
        private const byte VK_TAB = 0x09;
        private const byte VK_RETURN = 0x0D;
        private const byte VK_LWIN = 0x5B;

        public void Mute() => keybd_event(VK_VOLUME_MUTE, 0, 0, 0);
        public void PlayPauseMusic() => keybd_event(VK_MEDIA_PLAY_PAUSE, 0, 0, 0);

        // --- MOVIMIENTO Y CLICS DEL MOUSE ---

        public void MoveMouse(int x, int y)
        {
            SetCursorPos(x, y);
        }

        public void LeftClick()
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        public void ClickAt(int x, int y)
        {
            SetCursorPos(x, y);
            System.Threading.Thread.Sleep(50); // Pequeña pausa para que el cursor llegue
            LeftClick();
        }

        public void SetVolume(int percentage)
        {
            try
            {
                percentage = Math.Max(0, Math.Min(100, percentage));
                var enumerator = new MMDeviceEnumerator();
                var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                device.AudioEndpointVolume.MasterVolumeLevelScalar = percentage / 100f;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[Error de Volumen] {ex.Message}");
            }
        }

        public void ChangeVolumeBy(int amount)
        {
            try
            {
                var enumerator = new MMDeviceEnumerator();
                var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                float currentVol = device.AudioEndpointVolume.MasterVolumeLevelScalar * 100;
                SetVolume((int)Math.Round(currentVol) + amount);
            }
            catch { }
        }

        public void OpenApplication(string appName)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = appName,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[Error de Sistema] No pude abrir {appName}: {ex.Message}");
            }
        }

        internal void SearchAndPlaySpotifyAsync(string cancion)
        {
            throw new NotImplementedException();
        }

        public void OpenAppBySearch(string appName)
        {
            try
            {
                keybd_event(VK_LWIN, 0, 0, 0);
                Thread.Sleep(100);
                keybd_event(VK_LWIN, 0, 2, 0);

                Thread.Sleep(400);

                foreach (char c in appName)
                {
                    byte vk = (byte)char.ToUpper(c);
                    keybd_event(vk, 0, 0, 0);
                    Thread.Sleep(50);
                    keybd_event(vk, 0, 2, 0);
                    Thread.Sleep(50);
                }

                Thread.Sleep(500);
                keybd_event(VK_RETURN, 0, 0, 0);
                Thread.Sleep(100);
                keybd_event(VK_RETURN, 0, 2, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[Error de Teclado] {ex.Message}");
            }
        }
    }
}