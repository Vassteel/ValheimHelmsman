using System;
using System.Collections;

namespace Helmsman.Core;

// Unity logs coroutine exceptions without notifying the voyage that owns the work.
public static class GuardedSteps
{
    public static IEnumerator Run(IEnumerator steps, Action<Exception> failed)
    {
        try
        {
            while (true)
            {
                object? next;
                try
                {
                    if (!steps.MoveNext()) yield break;
                    next = steps.Current;
                }
                catch (Exception error) { failed(error); yield break; }
                yield return next;
            }
        }
        finally { (steps as IDisposable)?.Dispose(); }
    }
}
