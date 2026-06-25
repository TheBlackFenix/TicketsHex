using Microsoft.Extensions.Options;

namespace TicketsHex.API.Middelwares.ExceptionHandling
{
    public class ExceptionMessageResolver
    {
        private readonly IOptionsMonitor<ExceptionHandlingOptions> _optionsMonitor;

        public ExceptionMessageResolver(IOptionsMonitor<ExceptionHandlingOptions> optionsMonitor)
        {
            _optionsMonitor = optionsMonitor;
        }

        public ExceptionMessageOptions Resolve(Exception exception)
        {
            var options = _optionsMonitor.CurrentValue;

            var exceptionKey = GetExceptionKey(exception);

            if (options.Mappings.TryGetValue(exceptionKey, out var mappedOptions))
                return mappedOptions;

            var exceptionTypeName = exception.GetType().Name;

            if (options.Mappings.TryGetValue(exceptionTypeName, out var directOptions))
                return directOptions;

            return options.Default;
        }

        private static string GetExceptionKey(Exception exception)
        {
            return exception switch
            {
                System.Data.Common.DbException => "DbException",
                System.Data.DataException => "DataException",
                ArgumentNullException => "ArgumentNullException",
                ArgumentException => "ArgumentException",
                FileNotFoundException => "FileNotFoundException",
                UnauthorizedAccessException => "UnauthorizedAccessException",
                IOException => "IOException",
                InvalidOperationException => "InvalidOperationException",
                _ => exception.GetType().Name
            };
        }
    }
}
