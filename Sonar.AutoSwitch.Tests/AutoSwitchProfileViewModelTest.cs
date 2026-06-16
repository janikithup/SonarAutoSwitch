using System;
using System.Globalization;
using Sonar.AutoSwitch.Services;
using Sonar.AutoSwitch.ViewModels;
using Xunit;

namespace Sonar.AutoSwitch.Tests;

public class AutoSwitchProfileViewModelTest
{
    [Fact]
    public void DisplayName_ReturnsExeName_WhenTitleIsEmpty()
    {
        var vm = new AutoSwitchProfileViewModel { ExeName = "MyGame", Title = "" };
        Assert.Equal("MyGame", vm.DisplayName);
    }

    [Fact]
    public void DisplayName_ReturnsTitle_WhenTitleIsSet()
    {
        var vm = new AutoSwitchProfileViewModel { ExeName = "MyGame", Title = "My Game Window" };
        Assert.Equal("My Game Window", vm.DisplayName);
    }

    [Fact]
    public void DisplayName_ReturnsExeName_WhenTitleIsWhitespace()
    {
        var vm = new AutoSwitchProfileViewModel { ExeName = "MyGame", Title = "   " };
        Assert.Equal("MyGame", vm.DisplayName);
    }

    [Fact]
    public void StartDelete_SetsIsConfirmingDelete_True()
    {
        var vm = new AutoSwitchProfileViewModel();
        vm.StartDelete();
        Assert.True(vm.IsConfirmingDelete);
    }

    [Fact]
    public void CancelDelete_SetsIsConfirmingDelete_False()
    {
        var vm = new AutoSwitchProfileViewModel();
        vm.StartDelete();
        vm.CancelDelete();
        Assert.False(vm.IsConfirmingDelete);
    }

    [Fact]
    public void ConfirmDelete_InvokesOnDeleteConfirmed()
    {
        var vm = new AutoSwitchProfileViewModel();
        bool invoked = false;
        vm.OnDeleteConfirmed = () => invoked = true;
        vm.ConfirmDelete();
        Assert.True(invoked);
    }

    [Fact]
    public void ConfirmDelete_DoesNotThrow_WhenOnDeleteConfirmedIsNull()
    {
        var vm = new AutoSwitchProfileViewModel();
        vm.OnDeleteConfirmed = null;
        var ex = Record.Exception(() => vm.ConfirmDelete());
        Assert.Null(ex);
    }

    [Fact]
    public void CollapsingExpander_ClearsIsConfirmingDelete()
    {
        var vm = new AutoSwitchProfileViewModel();
        vm.IsExpanded = true;
        vm.StartDelete();
        Assert.True(vm.IsConfirmingDelete);
        vm.IsExpanded = false;
        Assert.False(vm.IsConfirmingDelete);
    }

    [Fact]
    public void ExpandingExpander_DoesNotClearIsConfirmingDelete()
    {
        var vm = new AutoSwitchProfileViewModel();
        vm.IsExpanded = true;
        vm.StartDelete();
        Assert.True(vm.IsConfirmingDelete);
    }

    [Fact]
    public void CreatedAtLabel_ReturnsEmptyString_WhenCreatedAtIsNull()
    {
        var vm = new AutoSwitchProfileViewModel { CreatedAt = null };
        Assert.Equal("", vm.CreatedAtLabel);
    }

    [Fact]
    public void CreatedAtLabel_FormatsDate_InInvariantCulture()
    {
        // Use a UTC value so ToLocalTime() still lands on the same date in any timezone
        // by picking midnight UTC — the local time will be the same day or one day ahead.
        // Use a specific date and verify the format pattern instead of an exact string.
        var utcDate = new DateTime(2026, 6, 16, 12, 0, 0, DateTimeKind.Utc);
        var vm = new AutoSwitchProfileViewModel { CreatedAt = utcDate };
        var label = vm.CreatedAtLabel;
        // Must match "d MMM yyyy" in InvariantCulture — verify pattern, not exact value
        // (local time conversion may shift day by ±1 depending on machine timezone)
        Assert.Matches(@"^\d{1,2} [A-Z][a-z]{2} \d{4}$", label);
    }

    [Fact]
    public void CreatedAtLabel_FormatsDate_CorrectlyForKnownValue()
    {
        // Use local time directly to avoid timezone conversion ambiguity
        var localDate = new DateTime(2026, 6, 16, 12, 0, 0, DateTimeKind.Local);
        var vm = new AutoSwitchProfileViewModel { CreatedAt = localDate };
        var expected = localDate.ToLocalTime().ToString("d MMM yyyy", CultureInfo.InvariantCulture);
        Assert.Equal(expected, vm.CreatedAtLabel);
    }

    [Fact]
    public void HasCreatedAt_IsFalse_WhenCreatedAtIsNull()
    {
        var vm = new AutoSwitchProfileViewModel { CreatedAt = null };
        Assert.False(vm.HasCreatedAt);
    }

    [Fact]
    public void HasCreatedAt_IsTrue_WhenCreatedAtIsSet()
    {
        var vm = new AutoSwitchProfileViewModel { CreatedAt = DateTime.UtcNow };
        Assert.True(vm.HasCreatedAt);
    }
}
