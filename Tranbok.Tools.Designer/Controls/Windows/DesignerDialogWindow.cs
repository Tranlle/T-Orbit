namespace Tranbok.Tools.Designer.Controls.Windows;

public class DesignerDialogWindow : DesignerWindow
{
    public DesignerDialogWindow()
    {
        Width = 640;
        MinWidth = 420;
        SizeToContent = Avalonia.Controls.SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner;
    }
}
