using AdaptiveSpritesDmiTool.Application;
using AdaptiveSpritesDmiTool.Domain.Documents;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

public partial class WorkspaceShellViewModel
{
    public bool IsDmiBatchOutputSelected
    {
        get => _selectedBatchOutputFormats.Contains(WorkspaceBatchOutputFormat.Dmi);
        set => SetBatchOutputFormatSelection(WorkspaceBatchOutputFormat.Dmi, value);
    }

    public bool IsPngBatchOutputSelected
    {
        get => _selectedBatchOutputFormats.Contains(WorkspaceBatchOutputFormat.Png);
        set => SetBatchOutputFormatSelection(WorkspaceBatchOutputFormat.Png, value);
    }

    public bool HasSelectedBatchOutputFormats => _selectedBatchOutputFormats.Count > 0;

    public SpriteDirectionDepth SelectedRasterDirectionDepth
    {
        get => _rasterExportSettings.DirectionDepth;
        set
        {
            if (!Enum.IsDefined(value) || value == _rasterExportSettings.DirectionDepth)
            {
                return;
            }

            _rasterExportSettings = _rasterExportSettings with { DirectionDepth = value };
            OnPropertyChanged();
            PersistWorkspaceSettingsInBackground();
        }
    }

    public SpriteDocumentExportFormat SelectedPngExportLayout
    {
        get => _rasterExportSettings.Layout;
        set
        {
            if (value is not (SpriteDocumentExportFormat.PngSheet or SpriteDocumentExportFormat.PngSequence) ||
                value == _rasterExportSettings.Layout)
            {
                return;
            }

            _rasterExportSettings = _rasterExportSettings with { Layout = value };
            OnPropertyChanged();
            PersistWorkspaceSettingsInBackground();
        }
    }

    private void SetBatchOutputFormatSelection(WorkspaceBatchOutputFormat format, bool selected)
    {
        var currentlySelected = _selectedBatchOutputFormats.Contains(format);
        if (currentlySelected == selected)
        {
            return;
        }

        if (!selected && _selectedBatchOutputFormats.Count == 1)
        {
            StatusMessage = "At least one batch output format must remain selected.";
            OnPropertyChanged(format == WorkspaceBatchOutputFormat.Dmi
                ? nameof(IsDmiBatchOutputSelected)
                : nameof(IsPngBatchOutputSelected));
            return;
        }

        _selectedBatchOutputFormats = selected
            ? _selectedBatchOutputFormats.Append(format).Distinct().OrderBy(static item => item).ToArray()
            : _selectedBatchOutputFormats.Where(item => item != format).ToArray();
        OnPropertyChanged(format == WorkspaceBatchOutputFormat.Dmi
            ? nameof(IsDmiBatchOutputSelected)
            : nameof(IsPngBatchOutputSelected));
        OnPropertyChanged(nameof(HasSelectedBatchOutputFormats));
        PersistWorkspaceSettingsInBackground();
    }
}
