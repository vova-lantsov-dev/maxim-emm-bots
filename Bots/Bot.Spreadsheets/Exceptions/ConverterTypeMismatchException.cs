using System;

namespace Bot.Spreadsheets.Exceptions
{
    public sealed class ConverterTypeMismatchException : Exception
    {
        private string ClassName { get; }
        private string PropertyName { get; }
        private string ConverterName { get; }
        private string ConverterOutputType { get; }


        public override string Message => $"Type of {ClassName}.{PropertyName} != {ConverterName}'s " +
                                          $"result type ({ConverterOutputType})";

        public ConverterTypeMismatchException(
            string className,
            string propertyName,
            string converterName,
            string converterOutputType)
        {
            ClassName = className;
            PropertyName = propertyName;
            ConverterName = converterName;
            ConverterOutputType = converterOutputType;
        }
    }
}