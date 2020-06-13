namespace Bot.Spreadsheets
{
    public sealed class SpreadsheetMapperOptions
    {
        public MappingAlignment Alignment { get; set; }
        public int SkipLines { get; set; }
        public string SpreadsheetId { get; set; }
        public string SheetName { get; set; }
    }

    public enum MappingAlignment
    {
        Horizontal,
        Vertical
    }
}