"""Run the shipped material resolver against current/legacy prefab shapes."""
from pathlib import Path
import os,subprocess,tempfile
root=Path(__file__).resolve().parents[2];source=(root/'src/Helmsman/ImportedShipMaterials.cs').read_text()
resolver=source[source.index('    internal static void Prepare()'):source.index('    internal static void Apply(')]
resolver+=source[source.index('    internal static Material BoatyardMaterial('):source.index('    internal static Material FleetMaterial(')]
with tempfile.TemporaryDirectory(prefix='helmsman-materials-') as tmp:
 p=Path(tmp);(p/'Check.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>annotations</Nullable></PropertyGroup></Project>')
 (p/'Resolver.cs').write_text('using System;using System.Linq;using System.Collections.Generic;using UnityEngine;using Jotunn.Managers;namespace Helmsman;internal static class ImportedShipMaterials {internal static Material wood,cloth,waterMask;private static Dictionary<string,Shader> native=new();private static Dictionary<string,Material> effects=new();'+resolver+'}')
 (p/'Stubs.cs').write_text('''namespace UnityEngine {
 public class Object {public static implicit operator bool(Object o)=>o!=null;}
 public class Shader:Object {public string name;public Shader(string n){name=n;}}
 public struct Color {public static Color black=>new();}
 public struct Vector2 {public static Vector2 one=>new();public static Vector2 zero=>new();}
 public class Texture2D:Object {public static Texture2D whiteTexture=new();}
 public enum MaterialGlobalIlluminationFlags {EmissiveIsBlack}
 public class Material:Object {public string name;public Shader shader;public Texture2D mainTexture;public Color color;public Vector2 mainTextureScale,mainTextureOffset;public MaterialGlobalIlluminationFlags globalIlluminationFlags;
 public Material(string n,string s){name=n;shader=new(s);}
 public Material(Material m){name=m.name;shader=m.shader;mainTexture=m.mainTexture;}
 public bool HasProperty(string p)=>true;public void SetFloat(string p,float v){}public void SetColor(string p,Color v){}public void DisableKeyword(string k){}
 }
 public class Renderer {public Material[] sharedMaterials;}
 public class MeshRenderer:Renderer {}
 public class SkinnedMeshRenderer:Renderer {}
 public class ParticleSystemRenderer:Renderer {}
 public class GameObject:Object {public Renderer[] Renderers;public T[] GetComponentsInChildren<T>(bool inactive)=>Renderers.OfType<T>().ToArray();}
 }
 namespace Jotunn.Managers {public class PrefabManager {public static PrefabManager Instance=new();public Dictionary<string,UnityEngine.GameObject> Prefabs=new();public UnityEngine.GameObject GetPrefab(string name)=>Prefabs.GetValueOrDefault(name);}}
 namespace Helmsman {public class Plugin {public static Plugin Instance=new();public void Record(string text){}}}''')
 (p/'Program.cs').write_text('''using Helmsman;using UnityEngine;using Jotunn.Managers;
 int checks=0;void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
 void Reset()=>PrefabManager.Instance.Prefabs.Clear();
 void Add(string prefab,params Material[] materials)=>PrefabManager.Instance.Prefabs[prefab]=new(){Renderers=new Renderer[]{new MeshRenderer{sharedMaterials=materials.Concat(prefab=="VikingShip"||prefab=="Karve"?new[]{new Material("native water mask","Custom/WaterMask")}:Array.Empty<Material>()).ToArray()}}};
 Reset();var hull=new Material("ship_wood_worn","Custom/Static");var sail=new Material("Cloth sail","Custom/Cloth");var bench=new Material("wood","Custom/Piece");
 Add("VikingShip",hull,sail);Add("piece_workbench",bench);ImportedShipMaterials.Prepare();
 Check(ImportedShipMaterials.wood==hull,"Native ship timber wins over workbench material");
 Check(ImportedShipMaterials.cloth==sail,"Cloth sail found without any legacy sail-object component");
 Reset();Add("VikingShip",hull);ImportedShipMaterials.Prepare();Check(ImportedShipMaterials.wood==hull&&ImportedShipMaterials.cloth==hull,"Native world surface supports headless/minimal sail layouts");
 Reset();Add("piece_chest_wood",bench);Add("Karve",new Material("mask","Custom/WaterMask"));ImportedShipMaterials.Prepare();Check(ImportedShipMaterials.wood==bench,"Hull fallback still uses actual Karve water masking");
 Reset();Add("VikingShip",new Material("mask","Custom/WaterMask"),new Material("blob","Custom/ShadowBlob"),hull);ImportedShipMaterials.Prepare();Check(ImportedShipMaterials.wood==hull,"Invisible water masks and shadow surfaces cannot become hull material");
 Reset();Add("VikingShip",hull,sail);var canvas=new Material("sail_white","Custom/Vegetation");Add("Karve",canvas);ImportedShipMaterials.Prepare();Check(ImportedShipMaterials.cloth==canvas,"Plain native Karve canvas wins over painted longship stripes");
 Check(ImportedShipMaterials.WaterMask().shader.name=="Custom/WaterMask","Water occlusion always uses a native mask shader");
 Check(!ImportedShipMaterials.WorldSurface("Custom/Unlit"),"Unlit fallback rejected");
 hull.mainTexture=new Texture2D();
 var timber=ImportedShipMaterials.BoatyardMaterial(new Color(),true);
 Check(timber.mainTexture==hull.mainTexture,"Workshop timber retains vanilla texture without retired embedded hull assets");
 Check(timber!=hull&&timber.shader==hull.shader,"Workshop owns its material but retains native shader");
 var plain=ImportedShipMaterials.BoatyardMaterial(new Color(),false);
 Check(plain.mainTexture==Texture2D.whiteTexture&&hull.mainTexture!=plain.mainTexture,"Plain palette does not alter native timber texture");
 Reset();var clean=new Material("ship_wood","Custom/Static");Add("VikingShip",hull,clean,sail);ImportedShipMaterials.Prepare();
 Check(ImportedShipMaterials.wood==clean,"Exact native boat finish wins when worn material is listed first");
 Reset();Add("VikingShip",hull,sail);Add("Karve",new Material("ship_wood (Instance)","Custom/Static"));ImportedShipMaterials.Prepare();
 Check(ImportedShipMaterials.wood.name=="ship_wood (Instance)","Clean Karve finish remains available across native prefab layouts");
 Console.WriteLine($"PASS: {checks} production maritime material-resolution checks.");''')
 subprocess.run([os.environ.get('DOTNET','/tmp/wildglow-dotnet/dotnet'),'run','--project',str(p/'Check.csproj'),'-c','Release'],check=True)
