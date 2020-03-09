using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace MHApi.DrewsClasses {
    public partial class EZImage : UserControl {
        [Category("Common Properties")]
        public EZImageSource Source {
            get { return (EZImageSource)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(EZImageSource), typeof(EZImage));

        [Category("Common Properties")]
        public double XMin {
            get { return (double)GetValue(XMinProperty); }
            set { SetValue(XMinProperty, value); }
        }
        public static readonly DependencyProperty XMinProperty = DependencyProperty.Register("XMin", typeof(double), typeof(EZImage));

        [Category("Common Properties")]
        public double XMax {
            get { return (double)GetValue(XMaxProperty); }
            set { SetValue(XMaxProperty, value); }
        }
        public static readonly DependencyProperty XMaxProperty = DependencyProperty.Register("XMax", typeof(double), typeof(EZImage));

        [Category("Common Properties")]
        public double YMin {
            get { return (double)GetValue(YMinProperty); }
            set { SetValue(YMinProperty, value); }
        }
        public static readonly DependencyProperty YMinProperty = DependencyProperty.Register("YMin", typeof(double), typeof(EZImage));

        [Category("Common Properties")]
        public double YMax {
            get { return (double)GetValue(YMaxProperty); }
            set { SetValue(YMaxProperty, value); }
        }
        public static readonly DependencyProperty YMaxProperty = DependencyProperty.Register("YMax", typeof(double), typeof(EZImage));

        [Category("Common Properties")]
        public IEnumerable ItemsSource {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(EZImage));

        [Category("Common Properties")]
        public DataTemplate ItemTemplate {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }
        public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register("ItemTemplate", typeof(DataTemplate), typeof(EZImage));

        [Category("Common Properties")]
        public Style ItemContainerStyle {
            get { return (Style)GetValue(ItemContainerStyleProperty); }
            set { SetValue(ItemContainerStyleProperty, value); }
        }
        public static readonly DependencyProperty ItemContainerStyleProperty = DependencyProperty.Register("ItemContainerStyle", typeof(Style), typeof(EZImage));

        public delegate void EZImageMouseButtonEventHandler(object sender, EZImageMouseButtonEventArgs e);

        public static readonly RoutedEvent EZImageMouseLeftButtonDownEvent = EventManager.RegisterRoutedEvent("EZImageMouseLeftButtonDown", RoutingStrategy.Bubble, typeof(EZImageMouseButtonEventHandler), typeof(Axes));
        public event EZImageMouseButtonEventHandler EZImageMouseLeftButtonDown {
            add { AddHandler(EZImageMouseLeftButtonDownEvent, value); }
            remove { RemoveHandler(EZImageMouseLeftButtonDownEvent, value); }
        }

        public static readonly RoutedEvent EZImageMouseLeftButtonUpEvent = EventManager.RegisterRoutedEvent("EZImageMouseLeftButtonUp", RoutingStrategy.Bubble, typeof(EZImageMouseButtonEventHandler), typeof(Axes));
        public event EZImageMouseButtonEventHandler EZImageMouseLeftButtonUp {
            add { AddHandler(EZImageMouseLeftButtonUpEvent, value); }
            remove { RemoveHandler(EZImageMouseLeftButtonUpEvent, value); }
        }

        public static readonly RoutedEvent EZImageMouseMoveEvent = EventManager.RegisterRoutedEvent("EZImageMouseMove", RoutingStrategy.Bubble, typeof(EZImageMouseButtonEventHandler), typeof(Axes));
        public event EZImageMouseButtonEventHandler EZImageMouseMove {
            add { AddHandler(EZImageMouseMoveEvent, value); }
            remove { RemoveHandler(EZImageMouseMoveEvent, value); }
        }

        public static readonly RoutedEvent EZImageMouseLeftButtonDoubleClickEvent = EventManager.RegisterRoutedEvent("EZImageMouseLeftButtonDoubleClick", RoutingStrategy.Bubble, typeof(EZImageMouseButtonEventHandler), typeof(Axes));
        public event EZImageMouseButtonEventHandler EZImageMouseLeftButtonDoubleClick {
            add { AddHandler(EZImageMouseLeftButtonDoubleClickEvent, value); }
            remove { RemoveHandler(EZImageMouseLeftButtonDoubleClickEvent, value); }
        }

        public EZImage() {
            InitializeComponent();

            DependencyPropertyDescriptor.FromProperty(ItemsSourceProperty, typeof(EZImage)).AddValueChanged(this, delegate {
                overlay.ItemsSource = ItemsSource;
            });

            DependencyPropertyDescriptor.FromProperty(ItemTemplateProperty, typeof(EZImage)).AddValueChanged(this, delegate {
                overlay.ItemTemplate = ItemTemplate;
            });

            DependencyPropertyDescriptor.FromProperty(ItemContainerStyleProperty, typeof(EZImage)).AddValueChanged(this, delegate {
                overlay.ItemContainerStyle = ItemContainerStyle;
            });
        }

        private void Axes_AxesMouseLeftButtonDown(object sender, AxesMouseButtonEventArgs e) {
            RaiseEvent(new EZImageMouseButtonEventArgs(EZImageMouseLeftButtonDownEvent, e.XPositionInSourcePixels, e.YPositionInSourcePixels));
        }

        private void Axes_AxesMouseLeftButtonUp(object sender, AxesMouseButtonEventArgs e) {
            RaiseEvent(new EZImageMouseButtonEventArgs(EZImageMouseLeftButtonUpEvent, e.XPositionInSourcePixels, e.YPositionInSourcePixels));
        }

        private void Axes_AxesMouseMove(object sender, AxesMouseButtonEventArgs e) {
            RaiseEvent(new EZImageMouseButtonEventArgs(EZImageMouseMoveEvent, e.XPositionInSourcePixels, e.YPositionInSourcePixels));
        }

        private void Axes_AxesMouseLeftButtonDoubleClick(object sender, AxesMouseButtonEventArgs e) {
            RaiseEvent(new EZImageMouseButtonEventArgs(EZImageMouseLeftButtonDoubleClickEvent, e.XPositionInSourcePixels, e.YPositionInSourcePixels));
        }
    }

    public class EZImageMouseButtonEventArgs : RoutedEventArgs {
        public double XPositionInSourcePixels { get; set; }
        public double YPositionInSourcePixels { get; set; }

        public EZImageMouseButtonEventArgs(RoutedEvent routedEvent, double xPositionInSourcePixels, double yPositionInSourcePixels) {
            RoutedEvent = routedEvent;
            XPositionInSourcePixels = xPositionInSourcePixels;
            YPositionInSourcePixels = yPositionInSourcePixels;
        }
    }
}
