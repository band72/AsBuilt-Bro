using System.Windows.Controls;

namespace RCS.Geo.Wpf.Views;

public partial class CoordinateTransformControl : UserControl
{
    public CoordinateTransformControl()
    {
        InitializeComponent();
    }
}

public class NullToCollapsedConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value == null) return System.Windows.Visibility.Collapsed;
        if (value is string s && string.IsNullOrWhiteSpace(s)) return System.Windows.Visibility.Collapsed;
        return System.Windows.Visibility.Visible;
    }

    public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new System.NotImplementedException();
    }
}
