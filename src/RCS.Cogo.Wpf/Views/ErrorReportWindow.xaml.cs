using System;
using System.Windows;
using System.Windows.Controls;
using RCS.Services;

namespace RCS.Cogo.Wpf.Views;

public partial class ErrorReportWindow : Window
{
    private readonly ErrorReporter _errorReporter;

    public ErrorReportWindow()
    {
        InitializeComponent();
        _errorReporter = new ErrorReporter();
    }

    private async void BtnSubmit_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtDescription.Text))
        {
            MessageBox.Show("Please describe the issue before submitting.", "Empty Report", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string severity = (CmbSeverity.SelectedItem as ComboBoxItem)?.Content?.ToString()?.ToLower() ?? "medium";
        var ex = new Exception(TxtDescription.Text);

        BtnSubmit.IsEnabled = false;
        TxtStatus.Text = "Sending report...";
        TxtStatus.Foreground = System.Windows.Media.Brushes.Yellow;

        string machineId = RCS.Cogo.Wpf.Services.NativeSecurityWrapper.GetHardwareFingerprint();
        bool success = await _errorReporter.ReportErrorAsync(ex, severity, machineId);

        if (success)
        {
            TxtStatus.Text = "Report sent successfully!";
            TxtStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
            BtnSubmit.IsEnabled = true;

            MessageBox.Show("Thank you! Your bug report has been attached to the remote server logger.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }
        else
        {
            TxtStatus.Text = "Failed to send report.";
            TxtStatus.Foreground = System.Windows.Media.Brushes.Red;
            BtnSubmit.IsEnabled = true;

            MessageBox.Show("We were unable to connect to the remote server to dispatch your report. Please check your internet connection and try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
