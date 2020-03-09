/*
Copyright 2011 Drew Robson
   Licensed under the MIT License, see License.txt.
   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
   AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
   LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
   OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
   SOFTWARE.
*/

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MHApi.DrewsClasses {
    public class Axes : Panel {
        [Category("Layout")]
        public double XMin {
            get { return (double)GetValue(XMinProperty); }
            set { SetValue(XMinProperty, value); }
        }
        public static readonly DependencyProperty XMinProperty = DependencyProperty.Register("XMin", typeof(double), typeof(Axes), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange));

        [Category("Layout")]
        public double XMax {
            get { return (double)GetValue(XMaxProperty); }
            set { SetValue(XMaxProperty, value); }
        }
        public static readonly DependencyProperty XMaxProperty = DependencyProperty.Register("XMax", typeof(double), typeof(Axes), new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsArrange));

        [Category("Layout")]
        public double YMin {
            get { return (double)GetValue(YMinProperty); }
            set { SetValue(YMinProperty, value); }
        }
        public static readonly DependencyProperty YMinProperty = DependencyProperty.Register("YMin", typeof(double), typeof(Axes), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange));

        [Category("Layout")]
        public double YMax {
            get { return (double)GetValue(YMaxProperty); }
            set { SetValue(YMaxProperty, value); }
        }
        public static readonly DependencyProperty YMaxProperty = DependencyProperty.Register("YMax", typeof(double), typeof(Axes), new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsArrange));

        [AttachedPropertyBrowsableForChildrenAttribute]
        [Category("Layout")]
        public static double GetX(DependencyObject obj) { return (double)obj.GetValue(XProperty); }
        public static void SetX(DependencyObject obj, double value) { obj.SetValue(XProperty, value); }
        public static readonly DependencyProperty XProperty = DependencyProperty.RegisterAttached("X", typeof(double), typeof(Axes), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsParentArrange));

        [AttachedPropertyBrowsableForChildrenAttribute]
        [Category("Layout")]
        public static double GetY(DependencyObject obj) { return (double)obj.GetValue(YProperty); }
        public static void SetY(DependencyObject obj, double value) { obj.SetValue(YProperty, value); }
        public static readonly DependencyProperty YProperty = DependencyProperty.RegisterAttached("Y", typeof(double), typeof(Axes), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsParentArrange));

        [AttachedPropertyBrowsableForChildrenAttribute]
        [Category("Layout")]
        public static double GetAxesWidth(DependencyObject obj) { return (double)obj.GetValue(AxesWidthProperty); }
        public static void SetAxesWidth(DependencyObject obj, double value) { obj.SetValue(AxesWidthProperty, value); }
        public static readonly DependencyProperty AxesWidthProperty = DependencyProperty.RegisterAttached("AxesWidth", typeof(double), typeof(Axes), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsParentArrange));

        [AttachedPropertyBrowsableForChildrenAttribute]
        [Category("Layout")]
        public static double GetAxesHeight(DependencyObject obj) { return (double)obj.GetValue(AxesHeightProperty); }
        public static void SetAxesHeight(DependencyObject obj, double value) { obj.SetValue(AxesHeightProperty, value); }
        public static readonly DependencyProperty AxesHeightProperty = DependencyProperty.RegisterAttached("AxesHeight", typeof(double), typeof(Axes), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsParentArrange));

        public delegate void AxesMouseButtonEventHandler(object sender, AxesMouseButtonEventArgs e);

        public static readonly RoutedEvent AxesMouseLeftButtonDownEvent = EventManager.RegisterRoutedEvent("AxesMouseLeftButtonDown", RoutingStrategy.Bubble, typeof(AxesMouseButtonEventHandler), typeof(Axes));
        public event AxesMouseButtonEventHandler AxesMouseLeftButtonDown {
            add { AddHandler(AxesMouseLeftButtonDownEvent, value); }
            remove { RemoveHandler(AxesMouseLeftButtonDownEvent, value); }
        }

        public static readonly RoutedEvent AxesMouseLeftButtonUpEvent = EventManager.RegisterRoutedEvent("AxesMouseLeftButtonUp", RoutingStrategy.Bubble, typeof(AxesMouseButtonEventHandler), typeof(Axes));
        public event AxesMouseButtonEventHandler AxesMouseLeftButtonUp {
            add { AddHandler(AxesMouseLeftButtonUpEvent, value); }
            remove { RemoveHandler(AxesMouseLeftButtonUpEvent, value); }
        }

        public static readonly RoutedEvent AxesMouseMoveEvent = EventManager.RegisterRoutedEvent("AxesMouseMove", RoutingStrategy.Bubble, typeof(AxesMouseButtonEventHandler), typeof(Axes));
        public event AxesMouseButtonEventHandler AxesMouseMove {
            add { AddHandler(AxesMouseMoveEvent, value); }
            remove { RemoveHandler(AxesMouseMoveEvent, value); }
        }

        public static readonly RoutedEvent AxesMouseLeftButtonDoubleClickEvent = EventManager.RegisterRoutedEvent("AxesMouseLeftButtonDoubleClick", RoutingStrategy.Bubble, typeof(AxesMouseButtonEventHandler), typeof(Axes));
        public event AxesMouseButtonEventHandler AxesMouseLeftButtonDoubleClick {
            add { AddHandler(AxesMouseLeftButtonDoubleClickEvent, value); }
            remove { RemoveHandler(AxesMouseLeftButtonDoubleClickEvent, value); }
        }

        public Axes() {
            Background = new SolidColorBrush(Colors.Transparent);
            MouseLeftButtonDown += OnMouseLeftButtonDown;
            MouseLeftButtonUp += OnMouseLeftButtonUp;
            MouseMove += OnMouseMove;
            ClipToBounds = true;
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            CaptureMouse();
            Point position = e.GetPosition(this);
            double xPositionInSourcePixels = XMin + position.X / ActualWidth * (XMax - XMin);
            double yPositionInSourcePixels = YMin + position.Y / ActualHeight * (YMax - YMin);
            if (e.ClickCount == 1)
                RaiseEvent(new AxesMouseButtonEventArgs(AxesMouseLeftButtonDownEvent, xPositionInSourcePixels, yPositionInSourcePixels));
            if (e.ClickCount == 2)
                RaiseEvent(new AxesMouseButtonEventArgs(AxesMouseLeftButtonDoubleClickEvent, xPositionInSourcePixels, yPositionInSourcePixels));
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (!IsMouseCaptured) return;
            ReleaseMouseCapture();
            var position = e.GetPosition(this);
            double xPositionInSourcePixels = XMin + position.X / ActualWidth * (XMax - XMin);
            double yPositionInSourcePixels = YMin + position.Y / ActualHeight * (YMax - YMin);
            RaiseEvent(new AxesMouseButtonEventArgs(AxesMouseLeftButtonUpEvent, xPositionInSourcePixels, yPositionInSourcePixels));
        }

        private void OnMouseMove(object sender, MouseEventArgs e) {
            if (!IsMouseCaptured) return;
            var position = e.GetPosition(this);
            double xPositionInSourcePixels = XMin + position.X / ActualWidth * (XMax - XMin);
            double yPositionInSourcePixels = YMin + position.Y / ActualHeight * (YMax - YMin);
            RaiseEvent(new AxesMouseButtonEventArgs(AxesMouseMoveEvent, xPositionInSourcePixels, yPositionInSourcePixels));
        }

        Size resultSize;
        protected override Size MeasureOverride(Size availableSize) {
            resultSize = new Size(0, 0);
            foreach (UIElement child in Children) {
                child.Measure(availableSize);
                resultSize.Width = Math.Max(resultSize.Width, child.DesiredSize.Width);
                resultSize.Height = Math.Max(resultSize.Height, child.DesiredSize.Height);
            }
            resultSize.Width = double.IsPositiveInfinity(availableSize.Width) ?
                resultSize.Width : availableSize.Width;
            resultSize.Height = double.IsPositiveInfinity(availableSize.Height) ?
                resultSize.Height : availableSize.Height;
            return resultSize;
        }

        protected override Size ArrangeOverride(Size finalSize) {
            foreach (UIElement child in InternalChildren) {
                double xDisplay, yDisplay, width, height;
                if (GetAxesWidth(child) == 0) {
                    width = child.DesiredSize.Width;
                    height = child.DesiredSize.Height;
                    xDisplay = (GetX(child) - XMin) / (XMax - XMin) * resultSize.Width - width / 2;
                    yDisplay = (GetY(child) - YMin) / (YMax - YMin) * resultSize.Height - height / 2;
                } else {
                    width = GetAxesWidth(child) / Math.Abs(XMax - XMin) * resultSize.Width;
                    height = GetAxesHeight(child) / Math.Abs(YMax - YMin) * resultSize.Height;
                    xDisplay = (GetX(child) - XMin) / (XMax - XMin) * resultSize.Width - width / 2;
                    yDisplay = (GetY(child) - YMin) / (YMax - YMin) * resultSize.Height - height / 2;
                }
                child.Arrange(new Rect(xDisplay, yDisplay, width, height));
            }
            return finalSize;
        }
    }

    public class AxesMouseButtonEventArgs : RoutedEventArgs {
        public double XPositionInSourcePixels { get; set; }
        public double YPositionInSourcePixels { get; set; }

        public AxesMouseButtonEventArgs(RoutedEvent routedEvent, double xPositionInSourcePixels, double yPositionInSourcePixels) {
            RoutedEvent = routedEvent;
            XPositionInSourcePixels = xPositionInSourcePixels;
            YPositionInSourcePixels = yPositionInSourcePixels;
        }
    }
}
