using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using GasPro.Services;
using GasPro.App.Services.Handlers;

namespace GasPro.App
{
    public partial class MainWindow : Window
    {
        private GasOrchestrator _cerebroGasPro;

        public enum EstadoIA { Reposo, Escuchando, Pensando, Hablando }
        private EstadoIA _estadoActual = EstadoIA.Reposo;

        private DispatcherTimer _timerMotorGrafico;
        private GasPro.App.Rendering.NeuronalRenderer _renderer;

        public MainWindow()
        {
            InitializeComponent();
            _renderer = new GasPro.App.Rendering.NeuronalRenderer(100);

            _timerMotorGrafico = new DispatcherTimer();
            _timerMotorGrafico.Interval = TimeSpan.FromMilliseconds(16);
            _timerMotorGrafico.Tick += MotorGrafico_Tick;
            _timerMotorGrafico.Start();

            this.Loaded += IniciarInteligenciaArtificial;
        }



        private void MotorGrafico_Tick(object sender, EventArgs e)
        {
            // Actualizamos la animación delegando en el renderer
            _renderer.Update(0.05f, _estadoActual);
            // Aseguramos que la superficie se repinte
            LienzoNeuronal.InvalidateVisual();
        }

        private void LienzoNeuronal_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            SKCanvas canvas = e.Surface.Canvas;
            float ancho = e.Info.Width;
            float alto = e.Info.Height;

            // Delegamos todo el dibujo al renderer
            _renderer?.Draw(canvas, ancho, alto);
        }

        public void CambiarEstado(EstadoIA nuevoEstado)
        {
            _estadoActual = nuevoEstado;
            // Informamos al renderer del nuevo estado para que actualice su objetivo visual inmediatamente
            _renderer?.Update(0f, nuevoEstado);
        }

        private async void IniciarInteligenciaArtificial(object sender, RoutedEventArgs e)
        {
            _cerebroGasPro = new GasOrchestrator();

            _cerebroGasPro.OnCambioEstado = (nuevoEstado) => Dispatcher.Invoke(() => CambiarEstado(nuevoEstado));

            try
            {
                string directorioBase = AppDomain.CurrentDomain.BaseDirectory;
                string rutaLlama = System.IO.Path.Combine(directorioBase, "models", "llama", "Llama-3.2-3B-Instruct-Q4_K_M.gguf");
                string rutaVosk = System.IO.Path.Combine(directorioBase, "models", "vosk");

                // Inicializamos y arrancamos en hilos de background para no bloquear UI
                await Task.Run(async () => await _cerebroGasPro.InitializeAsync(rutaLlama, rutaVosk));
                _ = Task.Run(async () => await _cerebroGasPro.RunAsync());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fatal al encender el núcleo: {ex.Message}");
            }
        }
    }
}