using System.Windows;
using System.Windows.Controls;
using RCS.Cogo.Wpf.ViewModels;

namespace RCS.Cogo.Wpf.Views
{
    public class StringToResourceTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            string? templateKey = null;
            if (item is string s)
            {
                templateKey = s;
            }
            else if (item is RCS.Data.Entities.SymbolManagerEntity entity)
            {
                templateKey = entity.Symbol;
            }

            if (!string.IsNullOrEmpty(templateKey) && container is FrameworkElement fe)
            {
                return fe.TryFindResource(templateKey) as DataTemplate;
            }
            return base.SelectTemplate(item, container);
        }
    }

    public partial class SymbolManagerWindow : Window
    {
        public SymbolManagerWindow()
        {
            InitializeComponent();
            DataContext = new SymbolManagerViewModel();
        }
    }
}
