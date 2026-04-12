using System.Windows;

namespace RCS.Cogo.Wpf.Views;

/// <summary>
/// Minimal modal dialog that asks the user for a starting point number
/// for the sequential renumber operation.
/// </summary>
public partial class RenumberDialog : Window
{
    public int StartNumber { get; private set; } = 1;

    public RenumberDialog()
    {
        // Build the UI entirely in code so no XAML/BAML file is needed
        Title           = "Renumber Points";
        Width           = 340;
        Height          = 160;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode      = ResizeMode.NoResize;
        Background      = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(0x1A, 0x1E, 0x2A));

        var grid = new System.Windows.Controls.Grid { Margin = new Thickness(16) };
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });

        var label = new System.Windows.Controls.TextBlock
        {
            Text       = "Renumber all points sequentially.\nStart from point number:",
            Foreground = System.Windows.Media.Brushes.Silver,
            FontSize   = 13,
            Margin     = new Thickness(0, 0, 0, 12),
            TextWrapping = System.Windows.TextWrapping.Wrap
        };
        System.Windows.Controls.Grid.SetColumnSpan(label, 2);
        grid.Children.Add(label);

        var numBox = new System.Windows.Controls.TextBox
        {
            Text            = "1",
            FontSize        = 14,
            Padding         = new Thickness(6, 4, 6, 4),
            Background      = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x26, 0x2C, 0x40)),
            Foreground      = System.Windows.Media.Brushes.White,
            BorderBrush     = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3E, 0x95, 0xD5)),
            BorderThickness = new Thickness(1),
            Margin          = new Thickness(0, 0, 8, 12)
        };
        System.Windows.Controls.Grid.SetRow(numBox, 1);
        grid.Children.Add(numBox);

        var btnOk = new System.Windows.Controls.Button
        {
            Content         = "Renumber",
            Padding         = new Thickness(12, 6, 12, 6),
            Background      = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1A, 0x55, 0x90)),
            Foreground      = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(1),
            BorderBrush     = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3E, 0x95, 0xD5)),
            IsDefault       = true
        };
        System.Windows.Controls.Grid.SetRow(btnOk, 2);
        System.Windows.Controls.Grid.SetColumn(btnOk, 0);

        var btnCancel = new System.Windows.Controls.Button
        {
            Content         = "Cancel",
            Padding         = new Thickness(12, 6, 12, 6),
            Background      = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2D, 0x2D, 0x3A)),
            Foreground      = System.Windows.Media.Brushes.Silver,
            BorderThickness = new Thickness(1),
            BorderBrush     = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x44, 0x44, 0x55)),
            IsCancel        = true,
            Margin          = new Thickness(8, 0, 0, 0)
        };
        System.Windows.Controls.Grid.SetRow(btnCancel, 2);
        System.Windows.Controls.Grid.SetColumn(btnCancel, 1);

        btnOk.Click += (_, _) =>
        {
            if (int.TryParse(numBox.Text, out int n) && n >= 0)
            { StartNumber = n; DialogResult = true; }
            else
                System.Windows.MessageBox.Show("Please enter a valid non-negative integer.", "Invalid Input",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
        };

        btnCancel.Click += (_, _) => DialogResult = false;

        grid.Children.Add(numBox);
        grid.Children.Add(btnOk);
        grid.Children.Add(btnCancel);
        Content = grid;

        Loaded += (_, _) => { numBox.SelectAll(); numBox.Focus(); };
    }
}
