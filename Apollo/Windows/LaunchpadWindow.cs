﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaColor = Avalonia.Media.Color;
using SolidColorBrush = Avalonia.Media.SolidColorBrush;
using Avalonia.Threading;

using Apollo.Components;
using Apollo.Core;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Windows {
    public class LaunchpadWindow: Window {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        Launchpad _launchpad;
        LaunchpadGrid Grid;

        private void UpdateTopmost(bool value) => Topmost = value;

        public LaunchpadWindow(Launchpad launchpad) {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif
            
            UpdateTopmost(Preferences.AlwaysOnTop);
            Preferences.AlwaysOnTopChanged += UpdateTopmost;

            _launchpad = launchpad;

            Grid = this.Get<LaunchpadGrid>("Grid");

            for (int i = 0; i < 100; i++)
                Grid.SetColor(LaunchpadGrid.SignalToGrid(i), new Color(0).ToScreenBrush());
        }

        private void PadChanged(int index, bool state) => _launchpad.HandleMessage(new Signal(_launchpad, (byte)LaunchpadGrid.GridToSignal(index), new Color((byte)(state? 63 : 0))));
        private void PadPressed(int index) => PadChanged(index, true);
        private void PadReleased(int index) => PadChanged(index, false);

        public void SignalRender(Signal n) => Dispatcher.UIThread.InvokeAsync(() => {
            Grid.SetColor(LaunchpadGrid.SignalToGrid(n.Index), n.Color.ToScreenBrush());
        });

        public static void Create(Launchpad launchpad, Window owner) {
            if (launchpad.Window == null) {
                launchpad.Window = new LaunchpadWindow(launchpad) {Owner = owner};
                launchpad.Window.Show();
                launchpad.Window.Owner = null;
            } else {
                launchpad.Window.WindowState = WindowState.Normal;
                launchpad.Window.Activate();
            }
        }
    }
}
