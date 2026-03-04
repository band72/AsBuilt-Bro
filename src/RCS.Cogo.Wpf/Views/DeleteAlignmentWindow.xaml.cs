using System.Collections;
using System.Linq;
using System.Windows;
using RCS.Data.Entities;

namespace RCS.Cogo.Wpf.Views
{
    public partial class DeleteAlignmentWindow : Window
    {
        public object? SelectedItem { get; private set; }
        
        public DeleteAlignmentWindow(string title, IEnumerable items)
        {
            InitializeComponent();
            TitleBlock.Text = title;
            
            // Convert to a projection that ListBox can bind "Name" to
            var displayList = new System.Collections.Generic.List<dynamic>();
            foreach(var item in items)
            {
                if (item is RCS.Data.Entities.HorizontalAlignment ha)
                    displayList.Add(new { Name = ha.AlignmentName, Original = item });
                else if (item is ProfileAlignment pa)
                    displayList.Add(new { Name = pa.ProfileName, Original = item });
            }
            
            ItemsListBox.ItemsSource = displayList;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (ItemsListBox.SelectedItem != null)
            {
                var selection = (dynamic)ItemsListBox.SelectedItem;
                SelectedItem = selection.Original;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please select an item to delete.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
