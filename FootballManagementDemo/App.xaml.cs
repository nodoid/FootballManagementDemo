namespace FootballManagementDemo;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Initialize the application window and root page using the recommended
        // CreateWindow pattern instead of setting MainPage directly (MainPage.set is obsolete).
        return new Window(new NavigationPage(new MainPage()));
    }
}
