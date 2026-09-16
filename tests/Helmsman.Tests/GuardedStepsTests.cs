using System;
using System.Collections;
using Helmsman.Core;

internal static class GuardedStepsTests
{
    internal static void Run(Action<bool,string> check)
    {
        bool disposed=false;int errors=0;
        var expected=new InvalidOperationException("Terrain query failed");
        IEnumerator Planning()
        {
            try { yield return "first frame";throw expected; }
            finally { disposed=true; }
        }
        var guarded=GuardedSteps.Run(Planning(),error=>{check(ReferenceEquals(error,expected),"Planning preserves the diagnostic exception");errors++;});
        check(guarded.MoveNext() && (string)guarded.Current=="first frame","Guard preserves incremental planning yields");
        check(!guarded.MoveNext() && errors==1 && disposed,"A later-frame planning failure stops and notifies its owner once");
        check(!guarded.MoveNext() && errors==1,"Failed planning never resumes");
        disposed=false;guarded=GuardedSteps.Run(Planning(),_=>errors++);
        guarded.MoveNext();((IDisposable)guarded).Dispose();
        check(disposed && errors==1,"Cancelling planning disposes the iterator without reporting a false error");
        guarded=GuardedSteps.Run(Array.Empty<object>().GetEnumerator(),_=>errors++);
        check(!guarded.MoveNext() && errors==1,"Successful empty planning does not report a failure");
    }
}
