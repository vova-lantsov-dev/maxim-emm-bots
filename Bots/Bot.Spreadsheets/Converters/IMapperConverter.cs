namespace Bot.Spreadsheets.Converters
{
    public interface IMapperConverter<out TOutputType>
    {
        TOutputType Convert(string value);
    }
}