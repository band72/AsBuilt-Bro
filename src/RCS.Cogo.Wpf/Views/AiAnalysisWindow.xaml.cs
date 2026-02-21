using System.Collections.Generic;
using System.Windows;
using RCS.Cogo.AI;

namespace RCS.Cogo.Wpf.Views;

public partial class AiAnalysisWindow : Window
{
    public List<AiAnalysisResult> Results { get; }

    public AiAnalysisWindow(List<AiAnalysisResult> results)
    {
        InitializeComponent();
        Results = results;
        DataContext = this;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
