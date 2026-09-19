using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using Jotunn.Managers;
using UnityEngine;
namespace Helmsman.Structures;
// Integration facade: one Helmsman plugin, one configuration and one patch owner.
internal sealed class StructureImports
{
    internal static StructureImports Instance=null!;
    internal ConfigEntry<string> ImportFolder=null!;
    internal PlacementTool Tool=null!;
    internal string ResolvedImportFolder=>ImportPaths.Normalize(ImportFolder.Value,Path.DirectorySeparatorChar=='\\');
    internal static void Initialize(Helmsman.Plugin host,ConfigFile config)
    {
        var downloads=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads","Valheim Buildings");
        Instance=new StructureImports();
        Instance.ImportFolder=config.Bind("Structures","ImportFolder",Directory.Exists(downloads)?downloads:Path.Combine(Paths.ConfigPath,"PoiTotem","Imports"),"Local .blueprint and .vbuild files for puffin construction. Original files are never changed.");
        Directory.CreateDirectory(Instance.ResolvedImportFolder);
        Instance.Tool=host.gameObject.AddComponent<PlacementTool>();
    }
    internal static bool Allowed=>Player.m_localPlayer&&SynchronizationManager.Instance.PlayerIsAdmin;
    internal static void Report(string message){Helmsman.Plugin.Instance.Record(message);Helmsman.Plugin.Message(message);}
}
