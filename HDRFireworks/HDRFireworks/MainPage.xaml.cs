using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Microsoft.Graphics.Canvas;
using Windows.Graphics.DirectX;
using Windows.UI.Core;
using Windows.System.Threading;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace HDRFireworks
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
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
        }

        private void CreateDeviceDependentResources()
        {
            _device = new CanvasDevice();
        }

        private void CreatePanelSizeDependentResources()
        {
            _panelWidth = (float)layoutPanel.ActualWidth;
            _panelHeight = (float)layoutPanel.ActualHeight;

            DirectXPixelFormat fmt;

            switch (_acInfo.CurrentAdvancedColorKind)
            {
                case AdvancedColorKind.HighDynamicRange:
                case AdvancedColorKind.WideColorGamut:
                    fmt = DirectXPixelFormat.R16G16B16A16Float;
                    break;

                default:
                    // Includes AdvancedColorKind.StandardDynamicRange
                    fmt = DirectXPixelFormat.B8G8R8A8UIntNormalized;
                    break;
            }

            if (_swapChain == null)
            {
                _swapChain = new CanvasSwapChain(
                    _device,
                    _panelWidth,
                    _panelHeight,
                    _dispInfo.LogicalDpi,
                    fmt,
                    2,
                    CanvasAlphaMode.Ignore);
            }
            else
            {
                _swapChain.ResizeBuffers(
                    _panelWidth,
                    _panelHeight,
                    _dispInfo.LogicalDpi,
                    fmt,
                    2);
            }

            swapChainPanel.SwapChain = _swapChain;

            _needUpdatePanelSizeResources = false;
        }

        private void ReleaseDeviceDependentResources()
        {

        }

        private void Render()
        {
            using (var ds = _swapChain.CreateDrawingSession(Windows.UI.Color.FromArgb(0, 0, 0, 0)))
            {
                float max = (_acInfo.CurrentAdvancedColorKind == AdvancedColorKind.HighDynamicRange) ? 5.0f : 1.0f;
                _color = (_color <= max) ? _color + 0.02f : 0.0f;

                ds.Clear(new System.Numerics.Vector4(_color, _color, _color, 1.0f));
            }

            _swapChain.Present(1);
        }

        private void Run()
        {
            IAsyncAction act = ThreadPool.RunAsync((workItem) =>
            {
                while (true)
                {
                    Render();
                }
            });
        }

        float _color = 0.0f;
        float _panelWidth;
        float _panelHeight;
        CanvasSwapChain _swapChain;
        CanvasDevice _device;
        DisplayInformation _dispInfo;
        AdvancedColorInfo _acInfo;
        bool _needUpdatePanelSizeResources;


    }
}
