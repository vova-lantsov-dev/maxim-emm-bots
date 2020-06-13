using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Bot.Spreadsheets.Annotations;
using Bot.Spreadsheets.Exceptions;
using Google.Apis.Sheets.v4;
using Microsoft.Extensions.Logging;

namespace Bot.Spreadsheets
{
    public sealed class SpreadsheetMapper<TMapTo> where TMapTo : class, new()
    {
        private readonly SheetsService _sheetsService;
        private readonly ILogger<SpreadsheetMapper<TMapTo>> _logger;

        public SpreadsheetMapper(SheetsService sheetsService, ILogger<SpreadsheetMapper<TMapTo>> logger)
        {
            _sheetsService = sheetsService;
            _logger = logger;
        }

        public async Task<List<TMapTo>> MapAsync(SpreadsheetMapperOptions options, CancellationToken ct)
        {
            try
            {
                var range = await _sheetsService.Spreadsheets.Values
                    .Get(options.SpreadsheetId, $"{options.SheetName}!$A$1:$YY")
                    .ExecuteAsync(ct);
                return options.Alignment switch
                {
                    MappingAlignment.Horizontal => MapHorizontal(range.Values, options),
                    MappingAlignment.Vertical => MapVertical(range.Values, options),
                    _ => throw new NotSupportedException(
                        $"Specified mapping alignment is not supported: {options.Alignment}")
                };
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private static List<TMapTo> MapVertical(IList<IList<object>> values, SpreadsheetMapperOptions options)
        {
            var result = new List<TMapTo>();
            
            for (var column = options.SkipLines; column < values.Max(it => it.Count); column++)
            {
                
            }

            return result;
        }
        
        private static List<TMapTo> MapHorizontal(IList<IList<object>> values, SpreadsheetMapperOptions options)
        {
            var result = new List<TMapTo>();
            var properties = typeof(TMapTo).GetProperties()
                .Select(p => new {Value = p, Attr = p.GetCustomAttribute<SpreadsheetAttribute>()})
                .Where(p => p.Attr != null)
                .ToArray();
            
            for (var row = options.SkipLines; row < values.Count; row++)
            {
                var temp = values[row];
                var resultItem = new TMapTo();
                
                for (var cell = 0; cell < temp.Count; cell++)
                {
                    var property = properties.FirstOrDefault(p => p.Attr!.Position == cell);
                    if (property == null)
                        continue;
                    
                    var cellValue = temp[cell].ToString();

                    {
                        var converterType = property.Attr!.ConverterType;
                        if (converterType != null)
                        {
                            var converterResultType = converterType.GenericTypeArguments[0];
                            if (converterResultType != property.Value.PropertyType)
                                throw new ConverterTypeMismatchException(nameof(TMapTo), property.Value.Name,
                                    converterType.Name, converterResultType.Name);
                            
                            var converter = Activator.CreateInstance(converterType);
                            var convertCall = Expression.Call(Expression.Constant(converter, converterType), "Convert",
                                null, Expression.Constant(cellValue, typeof(string)));
                            var convertLambda = Expression.Lambda(
                                typeof(Func<>).MakeGenericType(converterResultType),
                                convertCall);
                            var convertResult = convertLambda.Compile().DynamicInvoke();
                            property.Value.SetValue(resultItem, convertResult);
                            continue;
                        }
                    }

                    if (property.Value.PropertyType == typeof(string))
                    {
                        property.Value.SetValue(resultItem, cellValue);
                    }

                    throw new ConverterNotFoundException(nameof(TMapTo), property.Value.Name);
                }
                
                result.Add(resultItem);
            }

            return result;
        }
    }
}