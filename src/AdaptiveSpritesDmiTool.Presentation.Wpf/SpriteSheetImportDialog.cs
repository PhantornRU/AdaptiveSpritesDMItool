using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Domain.Configurations;
using AdaptiveSpritesDmiTool.Domain.Documents;
using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;
using Control = System.Windows.Controls.Control;
using Image = System.Windows.Controls.Image;
using Orientation = System.Windows.Controls.Orientation;
using ScrollViewer = System.Windows.Controls.ScrollViewer;
using StackPanel = System.Windows.Controls.StackPanel;
using TextBox = System.Windows.Controls.TextBox;
using Brushes = System.Windows.Media.Brushes;
using HorizontalAlignmentValue = System.Windows.HorizontalAlignment;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

internal sealed class SpriteSheetImportDialog : Window
{
    private readonly SpriteImage _preview;
    private readonly TextBox _cellWidth;
    private readonly TextBox _cellHeight;
    private readonly TextBox _marginLeft;
    private readonly TextBox _marginTop;
    private readonly TextBox _horizontalSpacing;
    private readonly TextBox _verticalSpacing;
    private readonly TextBox _columns;
    private readonly TextBox _rows;
    private readonly TextBox _framesPerDirection;
    private readonly TextBox _directionOrder;
    private readonly ComboBox _readingOrder;
    private readonly TextBlock _validationText;
    private readonly Canvas _gridOverlay;
    private readonly Button _okButton;

    public SpriteSheetImportDialog(SpriteImage preview, string stateName)
    {
        ArgumentNullException.ThrowIfNull(preview);
        _preview = preview;
        Title = $"Sprite sheet - {stateName}";
        Width = 980;
        Height = 700;
        MinWidth = 780;
        MinHeight = 560;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;

        var defaultCellWidth = preview.Width >= 32 && preview.Width % 32 == 0 ? 32 : preview.Width;
        var defaultCellHeight = preview.Height >= 32 && preview.Height % 32 == 0 ? 32 : preview.Height;
        _cellWidth = CreateNumberBox(defaultCellWidth);
        _cellHeight = CreateNumberBox(defaultCellHeight);
        _marginLeft = CreateNumberBox(0);
        _marginTop = CreateNumberBox(0);
        _horizontalSpacing = CreateNumberBox(0);
        _verticalSpacing = CreateNumberBox(0);
        _columns = CreateNumberBox(Math.Max(1, preview.Width / defaultCellWidth));
        _rows = CreateNumberBox(Math.Max(1, preview.Height / defaultCellHeight));
        _framesPerDirection = CreateNumberBox(Math.Max(1, (preview.Width / defaultCellWidth) * (preview.Height / defaultCellHeight)));
        _directionOrder = new TextBox { Text = SpriteDirection.South.ToString(), Margin = new Thickness(0, 2, 0, 7) };
        _readingOrder = new ComboBox
        {
            ItemsSource = Enum.GetValues<SpriteSheetReadingOrder>(),
            SelectedItem = SpriteSheetReadingOrder.RowsFirst,
            Margin = new Thickness(0, 2, 0, 7)
        };

        var form = new Grid { Margin = new Thickness(16) };
        form.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        form.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        AddFormField(form, 0, 0, "Cell width", _cellWidth);
        AddFormField(form, 0, 1, "Cell height", _cellHeight);
        AddFormField(form, 1, 0, "Left margin", _marginLeft);
        AddFormField(form, 1, 1, "Top margin", _marginTop);
        AddFormField(form, 2, 0, "Horizontal spacing", _horizontalSpacing);
        AddFormField(form, 2, 1, "Vertical spacing", _verticalSpacing);
        AddFormField(form, 3, 0, "Columns", _columns);
        AddFormField(form, 3, 1, "Rows", _rows);
        AddFormField(form, 4, 0, "Frames per direction", _framesPerDirection);
        AddFormField(form, 4, 1, "Reading order", _readingOrder);
        AddFormField(form, 5, 0, "Direction order (comma-separated)", _directionOrder, columnSpan: 2);

        _validationText = new TextBlock
        {
            Margin = new Thickness(16, 0, 16, 8),
            Foreground = Brushes.IndianRed,
            TextWrapping = TextWrapping.Wrap
        };

        var bitmap = new SpriteImageBitmapSourceFactory().Create(preview);
        var previewGrid = new Grid
        {
            Width = preview.Width,
            Height = preview.Height,
            SnapsToDevicePixels = true
        };
        previewGrid.Children.Add(new Image
        {
            Source = bitmap,
            Width = preview.Width,
            Height = preview.Height,
            Stretch = Stretch.Fill,
            SnapsToDevicePixels = true
        });
        _gridOverlay = new Canvas
        {
            Width = preview.Width,
            Height = preview.Height,
            IsHitTestVisible = false
        };
        previewGrid.Children.Add(_gridOverlay);

        var viewbox = new Viewbox
        {
            Stretch = Stretch.Uniform,
            StretchDirection = StretchDirection.DownOnly,
            Margin = new Thickness(16),
            Child = previewGrid
        };

        _okButton = new Button
        {
            Content = "Import",
            IsDefault = true,
            MinWidth = 90,
            Margin = new Thickness(6, 0, 0, 0)
        };
        _okButton.Click += (_, _) => Accept();
        var cancelButton = new Button
        {
            Content = "Cancel",
            IsCancel = true,
            MinWidth = 90
        };
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignmentValue.Right,
            Margin = new Thickness(16, 8, 16, 16),
            Children = { cancelButton, _okButton }
        };

        var left = new DockPanel { Width = 360 };
        DockPanel.SetDock(buttons, Dock.Bottom);
        DockPanel.SetDock(_validationText, Dock.Bottom);
        left.Children.Add(buttons);
        left.Children.Add(_validationText);
        left.Children.Add(new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = form
        });

        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.Children.Add(left);
        Grid.SetColumn(viewbox, 1);
        root.Children.Add(viewbox);
        Content = root;

        foreach (var textBox in new[]
                 {
                     _cellWidth, _cellHeight, _marginLeft, _marginTop, _horizontalSpacing,
                     _verticalSpacing, _columns, _rows, _framesPerDirection, _directionOrder
                 })
        {
            textBox.TextChanged += (_, _) => RefreshValidationAndGrid();
        }

        _readingOrder.SelectionChanged += (_, _) => RefreshValidationAndGrid();
        RefreshValidationAndGrid();
    }

    public SpriteSheetSlicingRecipe? SelectedRecipe { get; private set; }

    private static TextBox CreateNumberBox(int value) =>
        new()
        {
            Text = value.ToString(CultureInfo.InvariantCulture),
            Margin = new Thickness(0, 2, 0, 7)
        };

    private static void AddFormField(
        Grid grid,
        int row,
        int column,
        string label,
        Control control,
        int columnSpan = 1)
    {
        while (grid.RowDefinitions.Count <= row)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        var panel = new StackPanel { Margin = new Thickness(column == 0 ? 0 : 6, 0, column == 0 ? 6 : 0, 0) };
        panel.Children.Add(new TextBlock { Text = label });
        panel.Children.Add(control);
        Grid.SetRow(panel, row);
        Grid.SetColumn(panel, column);
        Grid.SetColumnSpan(panel, columnSpan);
        grid.Children.Add(panel);
    }

    private void RefreshValidationAndGrid()
    {
        if (!TryBuildRecipe(out var recipe, out var error))
        {
            _validationText.Text = error;
            _okButton.IsEnabled = false;
            _gridOverlay.Children.Clear();
            return;
        }

        _validationText.Text = $"{recipe.DirectionOrder.Count * recipe.FramesPerDirection} used cell(s) of {recipe.Columns * recipe.Rows}.";
        _validationText.Foreground = Brushes.LightGray;
        _okButton.IsEnabled = true;
        DrawGrid(recipe);
    }

    private bool TryBuildRecipe(out SpriteSheetSlicingRecipe recipe, out string error)
    {
        recipe = null!;
        error = string.Empty;
        if (!TryReadPositive(_cellWidth, out var cellWidth) ||
            !TryReadPositive(_cellHeight, out var cellHeight) ||
            !TryReadNonNegative(_marginLeft, out var marginLeft) ||
            !TryReadNonNegative(_marginTop, out var marginTop) ||
            !TryReadNonNegative(_horizontalSpacing, out var horizontalSpacing) ||
            !TryReadNonNegative(_verticalSpacing, out var verticalSpacing) ||
            !TryReadPositive(_columns, out var columns) ||
            !TryReadPositive(_rows, out var rows) ||
            !TryReadPositive(_framesPerDirection, out var framesPerDirection))
        {
            error = "Cell size, rows, columns, and frames must be positive whole numbers; margins and spacing may be zero.";
            return false;
        }

        var directionTokens = _directionOrder.Text
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (directionTokens.Length is not 1 and not 4 and not 8 ||
            directionTokens.Any(token => !Enum.TryParse<SpriteDirection>(token, true, out _)))
        {
            error = "Direction order must list exactly 1, 4, or 8 valid directions.";
            return false;
        }

        var directions = directionTokens.Select(static token => Enum.Parse<SpriteDirection>(token, true)).ToArray();
        var canonical = ((SpriteDirectionDepth)directions.Length).GetDirections();
        if (directions.Distinct().Count() != directions.Length || directions.Any(direction => !canonical.Contains(direction)))
        {
            error = "Direction order must contain every direction in the selected 1/4/8 profile exactly once.";
            return false;
        }

        try
        {
            var cellCount = checked(columns * rows);
            var usedCells = checked(directions.Length * framesPerDirection);
            var requiredWidth = checked((long)marginLeft + (long)columns * cellWidth + (long)(columns - 1) * horizontalSpacing);
            var requiredHeight = checked((long)marginTop + (long)rows * cellHeight + (long)(rows - 1) * verticalSpacing);
            if (cellCount > AssetImportLimits.Default.MaximumFramesOrCells || usedCells > cellCount)
            {
                error = $"The recipe uses {usedCells} cell(s), but the grid contains {cellCount}; maximum is {AssetImportLimits.Default.MaximumFramesOrCells}.";
                return false;
            }

            if (requiredWidth > _preview.Width || requiredHeight > _preview.Height)
            {
                error = $"Grid {requiredWidth}x{requiredHeight} extends outside source {_preview.Width}x{_preview.Height}.";
                return false;
            }
        }
        catch (OverflowException)
        {
            error = "Grid arithmetic overflowed.";
            return false;
        }

        recipe = new SpriteSheetSlicingRecipe(
            cellWidth,
            cellHeight,
            marginLeft,
            marginTop,
            horizontalSpacing,
            verticalSpacing,
            columns,
            rows,
            (SpriteSheetReadingOrder)(_readingOrder.SelectedItem ?? SpriteSheetReadingOrder.RowsFirst),
            directions,
            framesPerDirection);
        return true;
    }

    private void DrawGrid(SpriteSheetSlicingRecipe recipe)
    {
        _gridOverlay.Children.Clear();
        var pen = Brushes.DeepSkyBlue;
        for (var column = 0; column <= recipe.Columns; column++)
        {
            var x = recipe.MarginLeft + column * (recipe.CellWidth + recipe.HorizontalSpacing) -
                (column == recipe.Columns ? recipe.HorizontalSpacing : 0);
            _gridOverlay.Children.Add(new Line
            {
                X1 = x,
                X2 = x,
                Y1 = recipe.MarginTop,
                Y2 = recipe.MarginTop + recipe.Rows * recipe.CellHeight + (recipe.Rows - 1) * recipe.VerticalSpacing,
                Stroke = pen,
                StrokeThickness = 1,
                SnapsToDevicePixels = true
            });
        }

        for (var row = 0; row <= recipe.Rows; row++)
        {
            var y = recipe.MarginTop + row * (recipe.CellHeight + recipe.VerticalSpacing) -
                (row == recipe.Rows ? recipe.VerticalSpacing : 0);
            _gridOverlay.Children.Add(new Line
            {
                X1 = recipe.MarginLeft,
                X2 = recipe.MarginLeft + recipe.Columns * recipe.CellWidth + (recipe.Columns - 1) * recipe.HorizontalSpacing,
                Y1 = y,
                Y2 = y,
                Stroke = pen,
                StrokeThickness = 1,
                SnapsToDevicePixels = true
            });
        }
    }

    private void Accept()
    {
        if (!TryBuildRecipe(out var recipe, out _))
        {
            return;
        }

        SelectedRecipe = recipe;
        DialogResult = true;
    }

    private static bool TryReadPositive(TextBox box, out int value) =>
        int.TryParse(box.Text, NumberStyles.None, CultureInfo.InvariantCulture, out value) && value > 0;

    private static bool TryReadNonNegative(TextBox box, out int value) =>
        int.TryParse(box.Text, NumberStyles.None, CultureInfo.InvariantCulture, out value) && value >= 0;
}
