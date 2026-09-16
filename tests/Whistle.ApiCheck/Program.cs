using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
var root=Path.GetFullPath(args[0]);var game=args[1];
var resolver=new DefaultAssemblyResolver();
foreach(var dir in new[]{Path.Combine(game,"valheim_Data/Managed"),Path.Combine(game,"BepInEx/core"),Path.Combine(game,"BepInEx/plugins/ValheimModding-Jotunn"),Path.Combine(root,"src/Helmsman/bin/Release/netstandard2.1")})resolver.AddSearchDirectory(dir);
using var mod=ModuleDefinition.ReadModule(Path.Combine(root,"src/Helmsman/bin/Release/netstandard2.1/ValheimHelmsman.dll"),new ReaderParameters{AssemblyResolver=resolver});
var chat=resolver.Resolve(new AssemblyNameReference("assembly_valheim",new Version(0,0,0,0))).MainModule.Types.Single(t=>t.Name=="Chat");
if(!chat.Fields.Any(f=>f.Name=="m_hideTimer" && f.FieldType.FullName=="System.Single"))throw new Exception("Chat visibility field changed");
int members=0;foreach(var r in mod.GetMemberReferences())
{
 if(!(r.DeclaringType.Namespace.StartsWith("UnityEngine")||r.DeclaringType.Namespace.StartsWith("Jotunn")||r.DeclaringType.Namespace==""))continue;
 if(r is MethodReference m && m.Resolve()==null)throw new Exception("Unresolved "+r);
 if(r is FieldReference f && f.Resolve()==null)throw new Exception("Unresolved "+r);
 members++;
}
foreach(var name in new[]{"icon.png","model.json","model.bin"})
{
 var resource=(EmbeddedResource)mod.Resources.Single(r=>r.Name=="Helmsman.Gullcall."+name);
 if(!resource.GetResourceData().SequenceEqual(File.ReadAllBytes(Path.Combine(root,"assets/gullcall",name))))throw new Exception("Stale embedded "+name);
}
var hook=mod.Types.Single(t=>t.Name=="UseGullcallWhistle").Methods.Single(m=>m.Name=="Prefix");
var target=resolver.Resolve(new AssemblyNameReference("assembly_valheim",new Version(0,0,0,0))).MainModule.Types.Single(t=>t.Name=="Humanoid").Methods.Single(m=>m.Name=="UseItem");
if(target.ReturnType.FullName!="System.Void")throw new Exception("Unexpected UseItem return type");
foreach(var p in hook.Parameters.Where(p=>!p.Name.StartsWith("__")))
 if(!target.Parameters.Any(t=>t.Name==p.Name && t.ParameterType.FullName==p.ParameterType.FullName))throw new Exception("Invalid injected parameter "+p.Name);
Console.WriteLine($"PASS: {members} game/Unity/Jotunn members resolve; UseItem hook matches; embedded model/icon match assets.");
