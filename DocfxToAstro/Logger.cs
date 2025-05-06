using System;

namespace DocfxToAstro;

internal sealed class Logger
{
	private readonly bool verbose;

	public Logger(bool verbose)
	{
		this.verbose = verbose;
	}

	public void WriteInfo(string message)
	{
		Console.WriteLine(message);
	}
	
	public void WriteDebug(string message)
	{
		if (verbose)
		{
			Console.WriteLine(message);
		}
	}
}