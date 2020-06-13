using System;

namespace Bot.Spreadsheets.Annotations
{
    public sealed class SpreadsheetAttribute : Attribute
    {
        public int Position { get; }
        public Type ConverterType { get; set; }

        public SpreadsheetAttribute(int position)
        {
            Position = position;
        }
    }
}