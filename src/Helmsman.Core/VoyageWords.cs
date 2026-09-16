using System;

namespace Helmsman.Core;

public static class VoyageWords
{
    public static string Obstruction(string reason)
    {
        string lower=reason.ToLowerInvariant();
        if(lower.Contains("water depth"))return "Too shallow ahead, Viking. "+reason;
        if(lower.Contains("not available") || lower.Contains("not loaded"))return "I can't check the waters ahead yet, Viking. "+reason;
        if(lower.Contains("blocked") || lower.Contains("no clear"))return "Our way is blocked, Viking. "+reason;
        if(lower.Contains("no progress"))return "We're making no headway, Viking. I'm checking another course.";
        return "Holding here, Viking. "+reason;
    }
}
