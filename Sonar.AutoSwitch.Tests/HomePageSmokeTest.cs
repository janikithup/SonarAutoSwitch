using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Sonar.AutoSwitch.Pages;

namespace Sonar.AutoSwitch.Tests;

public class HomePageSmokeTest
{
    [AvaloniaFact]
    public void Home_renders_profile_list_and_exe_field()
    {
        var window = new Window { Width = 600, Height = 450 };
        window.Content = new Home();
        window.Show();
        window.UpdateLayout();

        var home = (Home)window.Content;
        var listBox = home.GetVisualDescendants().OfType<ListBox>().FirstOrDefault();
        var autoComplete = home.GetVisualDescendants().OfType<AutoCompleteBox>().FirstOrDefault();

        Assert.NotNull(listBox);
        Assert.NotNull(autoComplete);
        Assert.True(listBox.Bounds.Width > 0, "Profile list not rendered");
        Assert.True(autoComplete.Bounds.Width > 0, "ExeName field not rendered");
    }

    [AvaloniaFact]
    public void ExeName_autocomplete_has_process_list_and_opens_on_typing()
    {
        var window = new Window { Width = 600, Height = 450 };
        window.Content = new Home();
        window.Show();
        window.UpdateLayout();

        var home = (Home)window.Content;
        var autoComplete = home.GetVisualDescendants().OfType<AutoCompleteBox>().First();

        // ItemsSource must be wired — this catches FindControl returning null or ProcessNames being empty
        Assert.NotNull(autoComplete.ItemsSource);
        var items = autoComplete.ItemsSource!.Cast<string>().ToList();
        Assert.True(items.Count > 0, "ItemsSource is empty — OnLoaded did not wire process list");

        // Ctrl+A selects the current text, then typing replaces it and triggers the filter
        autoComplete.Focus();
        window.KeyPressQwerty(PhysicalKey.A, RawInputModifiers.Control);
        window.KeyTextInput("e"); // matches explorer, everything, etc.
        window.UpdateLayout();

        Assert.True(autoComplete.IsDropDownOpen,
            $"Dropdown did not open. Text='{autoComplete.Text}', Items={items.Count}");
    }
}
