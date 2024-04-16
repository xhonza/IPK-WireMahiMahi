using Gtk;

public class MainWindow : Window
{
    private MainWindowController controller;

    public MainWindow() : base("WireMahiMahi")
    {
        controller = new MainWindowController();
        Add(controller.MainBox);

        SetDefaultSize(controller.Width, controller.Height);
        SetPosition(WindowPosition.Center);
        DeleteEvent += delegate { Application.Quit(); };

        ShowAll();
    }
}
