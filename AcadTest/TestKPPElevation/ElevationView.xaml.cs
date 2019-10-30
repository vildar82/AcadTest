using System.Windows;

// ReSharper disable once CheckNamespace
namespace AcadTest
{
    /// <summary>
    /// Interaction logic for ElevationView.xaml
    /// </summary>
    public partial class ElevationView
    {
        public ElevationView(ElevationViewModel vm) : base(vm)
        {
            InitializeComponent();
        }

        private void Ok(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}