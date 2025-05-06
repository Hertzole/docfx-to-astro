using System.Threading.Tasks;
using ConsoleAppFramework;

namespace DocfxToAstro;

public static class Program
{
	private static async Task Main(string[] args)
	{
		await ConsoleApp.RunAsync(args, Commands.Generate);
	}
}