using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Sonar.AutoSwitch.ViewModels;

namespace Sonar.AutoSwitch.Pages;

public partial class Home : UserControl
{
    // ponytail: evaluated once on page load; good enough — user knows the exe name if not listed
    public static IEnumerable<string> ProcessNames =>
        Process.GetProcesses().Select(p => p.ProcessName).Distinct().OrderBy(x => x);

    public Home()
    {
        InitializeComponent();
        DataContext = HomeViewModel.LoadHomeViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
