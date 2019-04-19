﻿using System.Collections.Generic;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using GradientStop = Avalonia.Media.GradientStop;
using LinearGradientBrush = Avalonia.Media.LinearGradientBrush;

using Apollo.Components;
using Apollo.Devices;
using Apollo.Structures;

namespace Apollo.DeviceViewers {
    public class FadeViewer: UserControl {
        public static readonly string DeviceIdentifier = "fade";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Fade _fade;
        Canvas canvas;
        Grid PickerContainer;
        ColorPicker Picker;
        LinearGradientBrush Gradient;

        List<FadeThumb> thumbs = new List<FadeThumb>();

        int? current;

        public FadeViewer(Fade fade) {
            InitializeComponent();

            _fade = fade;
            _fade.Generated += Gradient_Generate;

            Dial Duration = this.Get<Dial>("Duration");
            Duration.UsingSteps = _fade.Mode;
            Duration.Length = _fade.Length;
            Duration.RawValue = _fade.Time;

            this.Get<Dial>("Gate").RawValue = (double)_fade.Gate * 100;
            
            canvas = this.Get<Canvas>("Canvas");
            PickerContainer = this.Get<Grid>("PickerContainer");
            Picker = this.Get<ColorPicker>("Picker");
            Gradient = this.Get<LinearGradientBrush>("Gradient");
            
            thumbs.Add(this.Get<FadeThumb>("ThumbStart"));

            for (int i = 1; i < _fade.Count - 1; i++) {
                FadeThumb thumb = new FadeThumb();
                thumbs.Insert(i, thumb);
                Canvas.SetLeft(thumb, (double)_fade.GetPosition(i) * 186 - 7);

                thumb.Moved += Thumb_Move;
                thumb.Focused += Thumb_Focus;
                thumb.Deleted += Thumb_Delete;

                canvas.Children.Add(thumb);
            }

            thumbs.Add(this.Get<FadeThumb>("ThumbEnd"));

            for (int i = 0; i < _fade.Count; i++)
                thumbs[i].Fill = _fade.GetColor(i).ToBrush();

            Gradient_Generate();
        }

        private void Canvas_MouseDown(object sender, PointerPressedEventArgs e) {
            if (e.MouseButton == MouseButton.Left && e.ClickCount == 2) {
                FadeThumb thumb = new FadeThumb();
                int index;
                double x_center = e.Device.GetPosition(canvas).X;
                double x_left = x_center - 7;

                for (index = 0; index < thumbs.Count; index++) {
                    if (x_left < Canvas.GetLeft(thumbs[index])) {
                        thumbs.Insert(index, thumb);
                        break;
                    }
                }

                Canvas.SetLeft(thumb, x_left);
                thumb.Moved += Thumb_Move;
                thumb.Focused += Thumb_Focus;
                thumb.Deleted += Thumb_Delete;

                _fade.Insert(index, new Color(), (decimal)x_center / 186);
                canvas.Children.Add(thumb);

                if (current != null && index <= current) current++;
                Expand(index);
            }
        }

        private void Expand(int? index) {
            if (current != null) {
                PickerContainer.ColumnDefinitions[0].Width = new GridLength(0);
                thumbs[current.Value].Unselect();

                if (index == current) {
                    current = null;
                    return;
                }
            }

            if (index != null) {
                PickerContainer.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                thumbs[index.Value].Select();

                Picker.SetColor(_fade.GetColor(index.Value));
            }
            
            current = index;
        }

        private void Thumb_Move(FadeThumb sender, VectorEventArgs e) {
            int i = thumbs.IndexOf(sender);

            double left = Canvas.GetLeft(thumbs[i - 1]) + 1;
            double right = Canvas.GetLeft(thumbs[i + 1]) - 1;

            double old = Canvas.GetLeft(sender);
            double x = old + e.Vector.X;

            x = (x < left)? left : x;
            x = (x > right)? right : x;

            _fade.SetPosition(i, (decimal)x / 186);
            Canvas.SetLeft(sender, x);
        }

        private void Thumb_Focus(FadeThumb sender) => Expand(thumbs.IndexOf(sender));

        private void Thumb_Delete(FadeThumb sender) {
            int index = thumbs.IndexOf(sender);

            if (current != null) {
                if (index < current) current--;
                else if (index == current) Expand(null);
            }

            thumbs.Remove(sender);
            _fade.Remove(index);
            canvas.Children.Remove(sender);
        }

        private void Color_Changed(Color color) {
            if (current != null) {
                _fade.SetColor(current.Value, color);
                thumbs[current.Value].Fill = color.ToBrush();
            }
        }

        private void Gradient_Generate() {
            Gradient.GradientStops.Clear();

            for (int i = 0; i < _fade.Count; i++)
                Gradient.GradientStops.Add(new GradientStop(_fade.GetColor(i).ToAvaloniaColor(), (double)_fade.GetPosition(i)));
        }

        private void Duration_Changed(double value) => _fade.Time = (int)value;

        private void Duration_ModeChanged(bool value) => _fade.Mode = value;

        private void Gate_Changed(double value) => _fade.Gate = (decimal)(value / 100);
    }
}