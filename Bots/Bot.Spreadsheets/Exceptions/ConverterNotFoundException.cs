using System;

namespace Bot.Spreadsheets.Exceptions
{
    public sealed class ConverterNotFoundException : Exception
    {
        private string ClassName { get; }
        private string PropertyName { get; }

        public override string Message => $"There is no specified converter for '{ClassName}.{PropertyName}' property. " +
                                          "Please, take a look at ConverterType property of SpreadsheetAttribute.";

        public ConverterNotFoundException(string className, string propertyName)
        {
            ClassName = className;
            PropertyName = propertyName;
        }
    }
}