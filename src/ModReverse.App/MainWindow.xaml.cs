using System.Windows;
using ModReverse.App.ViewModels;

namespace ModReverse.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
