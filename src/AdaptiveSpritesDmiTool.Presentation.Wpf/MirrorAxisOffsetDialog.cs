using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;
using Orientation = System.Windows.Controls.Orientation;
using StackPanel = System.Windows.Controls.StackPanel;
using TextBlock = System.Windows.Controls.TextBlock;
using TextBox = System.Windows.Controls.TextBox;
using HorizontalAlignmentValue = System.Windows.HorizontalAlignment;
using WpfMessageBox = System.Windows.MessageBox;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

internal sealed class MirrorAxisOffsetDialog : Window
{
    private readonly ComboBox _directionBox;
    private readonly TextBox _amountBox;
    private readonly int _maximumOffset;

    public MirrorAxisOffsetDialog(int currentOffset, int maximumOffset)
    {
        _maximumOffset = Math.Max(0, maximumOffset);
        Title = "Mirror axis";
        Width = 330;
        Height = 210;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;

        _directionBox = new ComboBox
        {
            Margin = new Thickness(0, 4, 0, 12),
            ItemsSource = new[] { "Centered", "Left", "Right" },
            SelectedItem = currentOffset switch
            {
                < 0 => "Left",
                > 0 => "Right",
                _ => "Centered"
            }
        };
        _directionBox.SelectionChanged += (_, _) => UpdateAmountAvailability();

        _amountBox = new TextBox
        {
            Margin = new Thickness(0, 4, 0, 12),
            Text = Math.Abs(currentOffset).ToString(CultureInfo.InvariantCulture)
        };

        var okButton = new Button
        {
            Content = "Apply",
            IsDefault = true,
            MinWidth = 80,
            Margin = new Thickness(4, 0, 0, 0)
        };
        okButton.Click += (_, _) => ApplySelection();

        var cancelButton = new Button
        {
            Content = "Cancel",
            IsCancel = true,
            MinWidth = 80,
            Margin = new Thickness(4, 0, 0, 0)
        };

        Content = new StackPanel
        {
            Margin = new Thickness(18),
            Children =
            {
                new TextBlock { Text = "Direction" },
                _directionBox,
                new TextBlock { Text = $"Whole pixels (maximum {_maximumOffset})" },
                _amountBox,
                new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignmentValue.Right,
                    Orientation = Orientation.Horizontal,
                    Children = { cancelButton, okButton }
                }
            }
        };

        UpdateAmountAvailability();
    }

    public int SelectedOffset { get; private set; }

    private void UpdateAmountAvailability()
    {
        var centered = string.Equals(_directionBox.SelectedItem as string, "Centered", StringComparison.Ordinal);
        _amountBox.IsEnabled = !centered;
        if (centered)
        {
            _amountBox.Text = "0";
        }
        else if (_amountBox.Text == "0")
        {
            _amountBox.Text = "1";
        }
    }

    private void ApplySelection()
    {
        var direction = _directionBox.SelectedItem as string ?? "Centered";
        if (string.Equals(direction, "Centered", StringComparison.Ordinal))
        {
            SelectedOffset = 0;
            DialogResult = true;
            return;
        }

        if (!int.TryParse(_amountBox.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var amount) ||
            amount <= 0 ||
            amount > _maximumOffset)
        {
            WpfMessageBox.Show(
                this,
                $"Enter a whole number from 1 to {_maximumOffset}.",
                "Mirror axis",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        SelectedOffset = string.Equals(direction, "Left", StringComparison.Ordinal) ? -amount : amount;
        DialogResult = true;
    }
}
