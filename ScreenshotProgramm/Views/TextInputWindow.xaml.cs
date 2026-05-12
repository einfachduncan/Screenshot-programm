using System.Windows;

namespace ScreenshotProgramm.Views;

public partial class TextInputWindow : Window
{
    public TextInputWindow()
    {
        InitializeComponent();
        InputText.Focus();
    }

    public string TextValue => InputText.Text;

    private void OkClicked(object sender, RoutedEventArgs e) => DialogResult = true;

    private void CancelClicked(object sender, RoutedEventArgs e) => DialogResult = false;
}
