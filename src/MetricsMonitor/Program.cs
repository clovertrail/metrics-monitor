using McMaster.Extensions.CommandLineUtils;
using System.Threading.Tasks;

namespace MetricsMonitor
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await CommandLineApplication.ExecuteAsync<CommandLineOptions>(args);
        }
    }
}
