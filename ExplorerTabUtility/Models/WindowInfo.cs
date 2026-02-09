using SHDocVw;
using System.Diagnostics;
using System.Threading;

namespace ExplorerTabUtility.Models;

public class WindowInfo
{
    private static int _orderCounter;

    public long CreatedAt { get; } = Stopwatch.GetTimestamp();
    public int Order { get; } = Interlocked.Increment(ref _orderCounter);
    public string? Location { get; set; }
    public string? Name { get; set; }
    public DWebBrowserEvents2_OnQuitEventHandler? OnQuitHandler { get; set; }
    public DWebBrowserEvents2_NavigateComplete2EventHandler? OnNavigateHandler { get; set; }
}