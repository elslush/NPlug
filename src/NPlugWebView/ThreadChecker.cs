using System;
using System.Diagnostics;
using System.Threading;

namespace NPlugWebView;

public readonly struct ThreadChecker
{
    private readonly int initialThreadId;

    public ThreadChecker()
    {
        initialThreadId = Environment.CurrentManagedThreadId;
    }

    public bool Test(string? failMessage = null, bool exit = false)
    {
        if (initialThreadId == Environment.CurrentManagedThreadId)
        {
            return true;
        }
        if (!string.IsNullOrEmpty(failMessage))
        {
            Debug.WriteLine(failMessage);
        }
        if (exit)
        {
            Environment.Exit(-1);
        }
        return false;
    }
}
