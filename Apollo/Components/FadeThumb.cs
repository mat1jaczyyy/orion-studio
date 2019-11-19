﻿using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;

using Apollo.DeviceViewers;
using Apollo.Enums;

namespace Apollo.Components {
    public class FadeThumb: UserControl {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Base = this.Get<Thumb>("Thumb");
            DeleteSeparator = this.Get<Separator>("DeleteSeparator");
        }

        public delegate void MovedEventHandler(FadeThumb sender, double change, double? total);
        public event MovedEventHandler Moved;

        public delegate void FadeThumbEventHandler(FadeThumb sender);
        public event FadeThumbEventHandler Focused;
        public event FadeThumbEventHandler Deleted;
        
        public delegate void TypeChangedEventHandler(FadeThumb sender, FadeType type);
        public event TypeChangedEventHandler TypeChanged;

        public FadeViewer Owner;

        ContextMenu ThumbContextMenu;
        List<MenuItem> MenuItems;
        Separator DeleteSeparator;
        Thumb Base;

        public bool NoDelete {
            get => !DeleteSeparator.IsVisible;
            set {
                MenuItems.Last().IsVisible = !value;
                DeleteSeparator.IsVisible = !value;
            }
        }

        public bool NoMenu { get; set; } = false;

        public IBrush Fill {
            get => (IBrush)this.Resources["Color"];
            set => this.Resources["Color"] = value;
        }

        public FadeThumb() {
            InitializeComponent();

            ThumbContextMenu = (ContextMenu)this.Resources["ThumbContextMenu"];
            ThumbContextMenu.AddHandler(MenuItem.ClickEvent, ContextMenu_Click);

            MenuItems = ThumbContextMenu.Items.OfType<MenuItem>().ToList();

            Base.AddHandler(InputElement.PointerPressedEvent, MouseDown, RoutingStrategies.Tunnel);
            Base.AddHandler(InputElement.PointerReleasedEvent, MouseUp, RoutingStrategies.Tunnel);
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            Moved = null;
            Focused = null;
            Deleted = null;

            ThumbContextMenu.RemoveHandler(MenuItem.ClickEvent, ContextMenu_Click);

            Base.RemoveHandler(InputElement.PointerPressedEvent, MouseDown);
            Base.RemoveHandler(InputElement.PointerReleasedEvent, MouseUp);
        }

        bool dragged = false;

        void DragStarted(object sender, VectorEventArgs e) {
            ((Window)this.GetVisualRoot()).Focus();
            dragged = false;
        }

        void DragCompleted(object sender, VectorEventArgs e) {
            if (!dragged) Focused?.Invoke(this);
            else if (change != 0) Moved?.Invoke(this, 0, change);
        }

        void MouseDown(object sender, PointerPressedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton != PointerUpdateKind.LeftButtonPressed) e.Handled = true;
            
            ((Window)this.GetVisualRoot()).Focus();
        }

        void MouseUp(object sender, PointerReleasedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (!NoMenu && MouseButton == PointerUpdateKind.RightButtonReleased) {
                if (Owner == null) throw new InvalidOperationException("FadeThumb doesn't have an Owner");

                MenuItems.ForEach(i => i.Icon = null);
                MenuItems[(int)Owner.GetFadeType(this)].Icon = this.Resources["SelectedIcon"];

                ThumbContextMenu.Open(Base);

                e.Handled = true;
            }
        }

        double change;

        void MouseMove(object sender, VectorEventArgs e) {
            if (!dragged) change = 0;
            change += e.Vector.X;

            dragged = true;
            Moved?.Invoke(this, e.Vector.X, null);
        }
        
        public void Select() => this.Resources["Outline"] = new SolidColorBrush(new Color(255, 255, 255, 255));
        public void Unselect() => this.Resources["Outline"] = new SolidColorBrush(new Color(0, 255, 255, 255));
        
        public void ContextMenu_Click(object sender, EventArgs e) {
            IInteractive item = ((RoutedEventArgs)e).Source;

            if (item is MenuItem selectedItem) {
                string header = selectedItem.Header.ToString();

                if (header == "Delete") Deleted?.Invoke(this);
                else TypeChanged?.Invoke(this, (FadeType)ThumbContextMenu.Items.OfType<MenuItem>().ToList().IndexOf(selectedItem));
            }
        }
    }
}
