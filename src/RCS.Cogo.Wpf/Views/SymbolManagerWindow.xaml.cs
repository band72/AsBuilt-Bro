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
            bool isEntity = false;

            if (item is string s)
            {
                templateKey = s;
            }
            else if (item is RCS.Data.Entities.SymbolManagerEntity entity)
            {
                templateKey = entity.Symbol;
                isEntity = true;
            }

            if (!string.IsNullOrEmpty(templateKey) && container is FrameworkElement fe)
            {
                if (templateKey.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase) || templateKey.EndsWith(".jpg", System.StringComparison.OrdinalIgnoreCase))
                {
                    return fe.TryFindResource(isEntity ? "ImageSymbolTemplateEntity" : "ImageSymbolTemplateString") as DataTemplate;
                }
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
