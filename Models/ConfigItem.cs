namespace AdaptiveSpritesDMItool.Models
{
    public record ConfigItem
    {
        public string FileName { get; init; }

        public string FilePath { get; init; }

        public ConfigState State = ConfigState.Saved;

        public ConfigItem(string _fileName, string _filePath)
        {
            FileName = _fileName;
            FilePath = _filePath;
        }
    }

    public enum ConfigState
    {
        Saved,
        NotSaved
    }
}
