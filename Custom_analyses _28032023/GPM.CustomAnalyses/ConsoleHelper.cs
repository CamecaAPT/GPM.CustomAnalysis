using System;
using System.Runtime.InteropServices;

namespace GPM.CustomAnalyses;

// Function Console
internal static class ConsoleHelper
{
	[DllImport("kernel32.dll")]
	public static extern IntPtr GetStdHandle(int nStdHandle);

	/// <summary>
	/// Allocates a new console for current process.
	/// </summary>
	[DllImport("kernel32.dll")]
	public static extern bool AllocConsole();

	/// <summary>
	/// Frees the console.
	/// </summary>
	[DllImport("kernel32.dll")]
	public static extern bool FreeConsole();

	public const int STD_OUTPUT_HANDLE = -11;
	public const int MY_CODE_PAGE = 437;
}
