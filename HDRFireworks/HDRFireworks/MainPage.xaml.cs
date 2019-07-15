using Microsoft.Graphics.Canvas;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

using Windows.Foundation;
using Windows.Graphics.DirectX;
using Windows.Graphics.Display;
using Windows.System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace HDRFireworks
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        static float _defaultMetersPerDip = 1.0f;
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void DebugWrite(string text)
        {
            console.Text += "\n" + text;
        }

        private void LayoutPanel_Loaded(object sender, RoutedEventArgs e)
        {
            DebugWrite(layoutPanel.ActualWidth + " " + layoutPanel.ActualHeight);

            CreateDeviceIndependentResources();
            CreateDeviceDependentResources();
            CreatePanelSizeDependentResources();

            Run();
        }

        private void CreateDeviceIndependentResources()
        {
            _dispInfo = DisplayInformation.GetForCurrentView();
            _acInfo = _dispInfo.GetAdvancedColorInfo();
            _stopwatch = new Stopwatch();
            _rng = new Random();
            _drawables = new List<BasicDrawable>();

            _stopwatch.Start();
        }

        private void CreateDeviceDependentResources()
        {
            _device = new CanvasDevice();
        }

        private void CreatePanelSizeDependentResources()
        {
            _panelWidth = (float)layoutPanel.ActualWidth;
            _panelHeight = (float)layoutPanel.ActualHeight;

            // Regardless of display AC type, use the same render code.
            DirectXPixelFormat fmt = DirectXPixelFormat.R16G16B16A16Float;
            int numBuffers = 2;

            if (_swapChain == null)
            {
                _swapChain = new CanvasSwapChain(
                    _device,
                    _panelWidth,
                    _panelHeight,
                    _dispInfo.LogicalDpi,
                    fmt,
                    numBuffers,
                    CanvasAlphaMode.Ignore);
            }
            else
            {
                _swapChain.ResizeBuffers(
                    _panelWidth,
                    _panelHeight,
                    _dispInfo.LogicalDpi,
                    fmt,
                    numBuffers);
            }

            swapChainPanel.SwapChain = _swapChain;
        }

        private void ReleaseDeviceDependentResources()
        {

        }

        private void Render()
        {
            using (var ds = _swapChain.CreateDrawingSession(Windows.UI.Color.FromArgb(0, 0, 0, 0)))
            {
                foreach (var item in _drawables)
                {
                    item.Render(ds, new Vector4());
                }
            }

            _swapChain.Present(1);
        }

        private void Update()
        {
            if (_rng.NextDouble() <= 0.05)
            {
                var item = new BasicDrawable();
                var pos = new Vector2((float)_rng.NextDouble() * _panelWidth / _defaultMetersPerDip,
                                      (float)_rng.NextDouble() * _panelHeight / _defaultMetersPerDip);

                item.Initialize(_stopwatch.ElapsedMilliseconds, pos, _rng, _defaultMetersPerDip);

                _drawables.Add(item);
            }

            foreach (var item in _drawables)
            {
                item.Update(_stopwatch.ElapsedMilliseconds);
            }

            _drawables.RemoveAll(x => x.CanDispose == true);
        }

        private void Run()
        {
            IAsyncAction act = ThreadPool.RunAsync((workItem) =>
            {
                while (true)
                {
                    Update();
                    Render();
                }
            });
        }

        float _panelWidth;
        float _panelHeight;
        CanvasSwapChain _swapChain;
        CanvasDevice _device;
        DisplayInformation _dispInfo;
        AdvancedColorInfo _acInfo;
        Stopwatch _stopwatch;
        Random _rng;
        List<BasicDrawable> _drawables;
    }
}
