using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using System.Text.Json;
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
int hooks=0;
foreach(var type in mod.Types)
foreach(var patch in type.CustomAttributes.Where(a=>a.AttributeType.Name=="HarmonyPatch" && a.ConstructorArguments.Count>=2))
{
 var targetType=((TypeReference)patch.ConstructorArguments[0].Value).Resolve();
 var kind=patch.ConstructorArguments[1];
 var methodName=kind.Type.FullName=="HarmonyLib.MethodType" && Equals(kind.Value,kind.Type.Resolve().Fields.Single(f=>f.Name=="Constructor").Constant)?".ctor":(string)kind.Value;
 var candidates=targetType.Methods.Where(m=>m.Name==methodName);
 if(patch.ConstructorArguments.Count>=3 && patch.ConstructorArguments[2].Value is CustomAttributeArgument[] signatures)
 {var names=signatures.Select(a=>((TypeReference)a.Value).FullName);candidates=candidates.Where(m=>m.Parameters.Select(p=>p.ParameterType.FullName).SequenceEqual(names));}
 var methods=candidates.ToArray();
 if(methods.Length!=1)throw new Exception("Ambiguous or missing Harmony target: "+targetType.Name+"."+methodName);
 var method=methods[0];
 foreach(var handler in type.Methods.Where(m=>m.Name is "Prefix" or "Postfix" or "Finalizer"))
 foreach(var parameter in handler.Parameters)
 {
  var actual=parameter.ParameterType is ByReferenceType byRef ? byRef.ElementType.FullName : parameter.ParameterType.FullName;
  if(parameter.Name=="__instance") {if(actual!=targetType.FullName)throw new Exception("Wrong __instance: "+type.Name);}
  else if(parameter.Name=="__result") {if(actual!=method.ReturnType.FullName)throw new Exception("Wrong __result: "+type.Name);}
  else if(parameter.Name.StartsWith("___")) {if(!targetType.Fields.Any(f=>f.Name==parameter.Name.Substring(3) && f.FieldType.FullName==actual))throw new Exception("Missing injected field: "+type.Name+"."+parameter.Name);}
  else if(!parameter.Name.StartsWith("__") && !method.Parameters.Any(p=>p.Name==parameter.Name && (p.ParameterType is ByReferenceType targetRef ? targetRef.ElementType.FullName : p.ParameterType.FullName)==actual))throw new Exception("Wrong injection: "+type.Name+"."+parameter.Name);
 }
 hooks++;
}
var api=chat.Module;
foreach(var field in new[]{("Ship","m_speed","Ship/Speed"),("Ship","m_rudderValue","System.Single"),("Inventory","m_width","System.Int32")})
 if(!api.Types.Single(t=>t.Name==field.Item1).Fields.Any(f=>f.Name==field.Item2 && f.FieldType.FullName==field.Item3))throw new Exception("Missing reflected field: "+field.Item2);
if(!api.Types.Single(t=>t.Name=="Player").Methods.Any(m=>m.Name=="TakeInput" && m.Parameters.Count==0 && m.ReturnType.FullName=="System.Boolean"))throw new Exception("Player.TakeInput changed");
foreach(var name in new[]{"Awake","Load"})
 if(api.Types.Single(t=>t.Name=="Container").Methods.Count(m=>m.Name==name && m.Parameters.Count==0)!=1)throw new Exception("Cargo deserialization hook changed: "+name);
foreach(var name in new[]{"sail-sea-flax","canopy-sea-flax","ballista-weathered","lantern-weathered"})
{
 var resource=(EmbeddedResource)mod.Resources.Single(r=>r.Name=="Helmsman.Shipwright."+name+".png");
 if(!resource.GetResourceData().SequenceEqual(File.ReadAllBytes(Path.Combine(root,"assets/shipwright",name+".png"))))throw new Exception("Stale ship texture "+name);
}
foreach(var file in Directory.GetFiles(Path.Combine(root,"assets/shipwright/hulls"),"*.png"))
{
 var name="Helmsman.Shipwright.hulls/"+Path.GetFileName(file);
 var resource=(EmbeddedResource)mod.Resources.Single(r=>r.Name==name);
 if(!resource.GetResourceData().SequenceEqual(File.ReadAllBytes(file)))throw new Exception("Stale hull texture "+name);
}
// Every native cargo key/RPC access must pass through the scoped transpiler. This
// catches added game methods, not merely the currently expected method count.
var containerType=api.Types.Single(t=>t.Name=="Container");
int scopedKeys=0,scopedRpcs=0;
foreach(var method in containerType.Methods.Where(m=>m.HasBody))
foreach(var instruction in method.Body.Instructions)
{
 bool key=instruction.Operand is FieldReference f && f.DeclaringType.Name=="ZDOVars" && new[]{"s_items","s_inUse","s_addedDefaultItems"}.Contains(f.Name);
 bool rpc=instruction.OpCode==Mono.Cecil.Cil.OpCodes.Ldstr && instruction.Operand is string s && s.StartsWith("RPC_",StringComparison.Ordinal);
 if(!key&&!rpc)continue;
 bool covered=!method.IsStatic && (new[]{"Awake","UpdateUseVisual","Interact","Save","Load","StackAll","TakeAll"}.Contains(method.Name)||method.Name.StartsWith("RPC_",StringComparison.Ordinal));
 if(!covered)throw new Exception("Unscoped ship-hold access in Container."+method.Name);
 if(key)scopedKeys++;if(rpc)scopedRpcs++;
}
if(scopedKeys!=6 || scopedRpcs<15)throw new Exception($"Cargo key/RPC access patterns changed: {scopedKeys}/{scopedRpcs}");
Console.WriteLine($"PASS: all {scopedKeys} cargo save keys and {scopedRpcs} RPC names are covered by hold isolation.");
var tick=api.Types.Single(t=>t.Name=="Ship").Methods.Single(m=>m.Name=="CustomFixedUpdate");
int crewSites=tick.Body.Instructions.Count(i=>i.Operand is MethodReference m && m.Name=="get_Count" && m.DeclaringType is GenericInstanceType g && g.ElementType.FullName=="System.Collections.Generic.List`1" && g.GenericArguments.Single().Name=="Player");
if(crewSites!=2)throw new Exception("Empty-crew physics pattern changed: "+crewSites);
using var manifest=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"packaging/manifest.json")));
var version=manifest.RootElement.GetProperty("version_number").GetString()!;
using var core=ModuleDefinition.ReadModule(Path.Combine(root,"src/Helmsman.Core/bin/Release/netstandard2.1/Helmsman.Core.dll"));
var plugin=mod.Types.Single(t=>t.Name=="Plugin").CustomAttributes.Single(a=>a.AttributeType.Name=="BepInPlugin");
if((string)plugin.ConstructorArguments[2].Value!=version || mod.Assembly.Name.Version!=new Version(version+".0") || core.Assembly.Name.Version!=mod.Assembly.Name.Version)throw new Exception("Plugin, Core and manifest versions differ");
Console.WriteLine($"PASS: {members} game/Unity/Jotunn members; {hooks} Harmony targets; private fields and two crew-count sites; embedded assets and {version} DLL versions match.");
