using Avalonia.Controls;
using Avalonia.Interactivity;
using Tmds.DBus.Protocol;

namespace IAFTS.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.CanResize = false;  // Запретить изменение размера
            this.Width = 1500;
            this.Height = 600;
        }
    }
}