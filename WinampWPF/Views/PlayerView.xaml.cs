using System.Windows.Controls;
using System.Windows.Input;

namespace WinampWPF.Views;

public partial class PlayerView : UserControl
{
    public PlayerView()
    {
        InitializeComponent();
    }

    private void ProgressSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Slider slider)
            return;

        if (slider.Maximum <= slider.Minimum)
            return;

        var position = e.GetPosition(slider);
        var ratio = position.X / slider.ActualWidth;
        ratio = Math.Clamp(ratio, 0.0, 1.0);
        var value = slider.Minimum + (slider.Maximum - slider.Minimum) * ratio;
        slider.Value = value;
        e.Handled = true;
    }

    private void ProgressSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Slider slider)
            return;

        if (slider.Maximum <= slider.Minimum)
            return;

        var position = e.GetPosition(slider);
        var ratio = position.X / slider.ActualWidth;
        ratio = Math.Clamp(ratio, 0.0, 1.0);
        var value = slider.Minimum + (slider.Maximum - slider.Minimum) * ratio;
        slider.Value = value;
        e.Handled = true;
    }
}