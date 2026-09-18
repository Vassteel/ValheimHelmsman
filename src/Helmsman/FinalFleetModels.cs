using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace Helmsman;

// Authored Blender geometry; native Ship, ZNetView and cargo serialization remain intact.
internal static partial class FinalFleetModels
{
    private sealed class Part {internal string Group="";internal Mesh Mesh=null!;internal Material Material=null!;}
    private sealed class Model {internal Spec Spec=null!;internal readonly List<Part> Parts=new();}
    private static readonly Dictionary<string,Model> cache=new();
    private static readonly List<Object> owned=new();
    internal static bool Has(string name)=>new[]{"MercantShip","BigCargoShip","WarShip","HerculeShip","LittleBoat","HelmsmanCurrach","HelmsmanDugout","HelmsmanFinewoodKayak","HelmsmanTandemKayak"}.Contains(name);
    internal static bool PaddleCraft(string name)=>name is "HelmsmanDugout" or "HelmsmanFinewoodKayak" or "HelmsmanTandemKayak";
    private static Vector3 V(float[] values)
    {if(values.Length!=3||values.Any(v=>float.IsNaN(v)||float.IsInfinity(v)))throw new InvalidDataException("Invalid final fleet vector");return new Vector3(values[0],values[1],values[2]);}
    private static int Count(BinaryReader r,int max)
    {int n=r.ReadInt32();if(n<0||n>max)throw new InvalidDataException("Invalid final fleet count");return n;}
    private static byte[] Bytes(BinaryReader r,int max)
    {int n=Count(r,max);var b=r.ReadBytes(n);if(b.Length!=n)throw new EndOfStreamException();return b;}
    private static string Text(BinaryReader r,int max=1024)=>Encoding.UTF8.GetString(Bytes(r,max));
    private static float Number(BinaryReader r)
    {var n=r.ReadSingle();if(float.IsNaN(n)||float.IsInfinity(n)||Math.Abs(n)>100000)throw new InvalidDataException("Invalid final fleet coordinate");return n;}
    private static Model Load(string name)
    {
        if(cache.TryGetValue(name,out var existing))return existing;
        using var stream=typeof(Plugin).Assembly.GetManifestResourceStream("Helmsman.Ships.final."+name+".bin.gz")??throw new InvalidDataException("Missing final ship "+name);
        using var zip=new GZipStream(stream,CompressionMode.Decompress);using var r=new BinaryReader(zip);
        if(Encoding.ASCII.GetString(r.ReadBytes(4))!="HMF1")throw new InvalidDataException("Unknown final fleet format");
        var model=new Model{Spec=ReadSpecification(Text(r,1000000),name)};
        int nm=Count(r,128);var materials=new Material[nm];
        for(int i=0;i<nm;i++)
        {
            string label=Text(r);var png=Bytes(r,1024*1024);var color=new Color(Number(r),Number(r),Number(r),Number(r));
            // Read the packed study image to keep HMF compatibility; use the running
            // game's corresponding wood/cloth texture and shader for the playable ship.
            var mat=ImportedShipMaterials.FleetMaterial(label,color);owned.Add(mat);
            materials[i]=mat;
        }
        int parts=Count(r,256);
        for(int p=0;p<parts;p++)
        {
            string group=Text(r);int material=Count(r,nm-1),n=Count(r,1000000);
            var vertices=new Vector3[n];var normals=new Vector3[n];var uv=new Vector2[n];
            for(int i=0;i<n;i++){vertices[i]=new Vector3(Number(r),Number(r),Number(r));normals[i]=new Vector3(Number(r),Number(r),Number(r));uv[i]=new Vector2(Number(r),Number(r));}
            int nt=Count(r,6000000);if(nt%3!=0)throw new InvalidDataException("Invalid final ship triangles");var triangles=new int[nt];
            for(int i=0;i<nt;i++){triangles[i]=r.ReadInt32();if(triangles[i]<0||triangles[i]>=n)throw new InvalidDataException("Invalid final ship index");}
            var mesh=new Mesh{name="Helmsman "+name+" "+group,indexFormat=n>65535?IndexFormat.UInt32:IndexFormat.UInt16};owned.Add(mesh);
            mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateTangents();mesh.RecalculateBounds();
            model.Parts.Add(new Part{Group=group,Mesh=mesh,Material=materials[material]});
        }
        if(r.BaseStream.ReadByte()!=-1)throw new InvalidDataException("Trailing final ship data");cache.Add(name,model);return model;
    }
    private static Transform Node(string name,Transform parent,Vector3 position)
    {var go=new GameObject(name);go.layer=parent.gameObject.layer;go.transform.SetParent(parent,false);go.transform.localPosition=position;return go.transform;}
    private static void Rehome(Transform node,Transform root,Vector3 position)
    {node.SetParent(root,false);node.localPosition=position;node.localRotation=Quaternion.identity;node.localScale=Vector3.one;}
    internal static void Apply(GameObject prefab,string name)
    {
        var model=Load(name);var spec=model.Spec;var ship=prefab.GetComponent<Ship>();var root=prefab.transform;
        var maskMaterial=ImportedShipMaterials.WaterMask();
        var controls=ship.m_shipControlls;var holds=prefab.GetComponentsInChildren<Container>(true);
        // Old meshes/LOD switching/animators and colliders cannot remain as invisible obstacles.
        foreach(var renderer in prefab.GetComponentsInChildren<Renderer>(true)){renderer.enabled=false;renderer.forceRenderingOff=true;}
        foreach(var group in prefab.GetComponentsInChildren<LODGroup>(true))Object.DestroyImmediate(group);
        foreach(var animator in prefab.GetComponentsInChildren<Animator>(true))animator.enabled=false;
        foreach(var cloth in prefab.GetComponentsInChildren<Cloth>(true))
        {cloth.enabled=false;var wind=cloth.GetComponent<GlobalWind>();if(wind){wind.CancelInvoke();wind.enabled=false;}}
        foreach(var light in prefab.GetComponentsInChildren<Light>(true))light.enabled=false;
        foreach(var area in prefab.GetComponentsInChildren<EffectArea>(true))Object.DestroyImmediate(area);
        foreach(var c in prefab.GetComponentsInChildren<Collider>(true))
            if(c!=ship.m_floatCollider && c.name!="OnboardTrigger")Object.DestroyImmediate(c);
        foreach(var chair in prefab.GetComponentsInChildren<Chair>(true))Object.DestroyImmediate(chair);
        foreach(var ladder in prefab.GetComponentsInChildren<Ladder>(true))Object.DestroyImmediate(ladder);
        var visual=Node("Final fleet",root,Vector3.zero);
        var mast=Node("Rig",visual,Vector3.zero);
        var sail=Node("Sail",mast,V(spec.sailPivot));
        var rudder=Node("Steering rudder",visual,V(spec.rudderPivot));
        var paddle=Node("Paddle template",visual,Vector3.zero);
        ship.m_mastObject=mast.gameObject;ship.m_sailObject=sail.gameObject;
        ship.m_rudderObject=null!; // Retired source rudder must not rotate an unrelated hidden hierarchy.
        foreach(var part in model.Parts)
        {
            var parent=part.Group=="paddle"?paddle:part.Group=="sail"?sail:part.Group=="rudder"?rudder:visual;
            var node=Node(part.Group+" "+parent.childCount,parent,part.Group=="sail"?-V(spec.sailPivot):part.Group=="rudder"?-V(spec.rudderPivot):Vector3.zero);
            node.gameObject.AddComponent<MeshFilter>().sharedMesh=part.Mesh;
            var renderer=node.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=part.Material;
            renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
        }
        paddle.gameObject.SetActive(false);
        if(PaddleCraft(name)){var craft=prefab.AddComponent<PaddleCraftRig>();craft.PaddleTemplate=paddle.gameObject;craft.DoubleBlade=name!="HelmsmanDugout";}
        var rig=prefab.AddComponent<FinalShipPresentation>();rig.Sail=sail;rig.Rudder=rudder;rig.Pivot=V(spec.sailPivot);
        rig.SheetHeads=spec.sheets.Select(s=>V(s.head)).ToArray();rig.SheetFeet=spec.sheets.Select(s=>V(s.foot)).ToArray();
        rig.RopeMaterial=ImportedShipMaterials.BoatyardMaterial(new Color(.38f,.29f,.16f),false);owned.Add(rig.RopeMaterial);
        rig.HullRenderers=visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name.StartsWith("hull ")).ToArray();
        rig.Decorations=visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name.StartsWith("decoration ")).Select(r=>r.gameObject).ToArray();
        rig.SailRenderers=sail.GetComponentsInChildren<MeshRenderer>(true);rig.AirHeight=spec.airHeight;
        var motion=prefab.AddComponent<FleetSailMotion>();motion.Kind=spec.name;motion.Sail=sail;motion.Top=spec.sailPivot[1];motion.AuthoredMeshes=sail.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh).ToArray();
        rig.Motion=motion;rig.Sculling=spec.name=="freighter";
        foreach(var solid in spec.colliders)
        {var node=Node("Hull solid",root,V(solid.position));node.gameObject.layer=LayerMask.NameToLayer("vehicle");var box=node.gameObject.AddComponent<BoxCollider>();box.size=V(solid.size);}
        foreach(var solid in spec.hullSolids.Concat(spec.cargoSolids??Array.Empty<HullSolid>()))
        {
            if(!((solid.vertices.Length==24&&solid.triangles.Length==36)||(solid.vertices.Length==18&&solid.triangles.Length==24)))throw new InvalidDataException("Invalid convex ship section");
            var vertices=new Vector3[solid.vertices.Length/3];for(int i=0;i<vertices.Length;i++)vertices[i]=V(solid.vertices.Skip(i*3).Take(3).ToArray());
            var mesh=new Mesh{name="Fitted ship section"};owned.Add(mesh);mesh.vertices=vertices;mesh.triangles=solid.triangles;mesh.RecalculateBounds();
            var node=Node("Keel or cargo solid",root,Vector3.zero);node.gameObject.layer=LayerMask.NameToLayer("vehicle");
            var collider=node.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=mesh;collider.convex=true;
        }
        // Buoyancy/route footprint now matches the replacement, independently of visual cargo.
        Rehome(ship.m_floatCollider.transform,root,new Vector3(0,spec.waterline,0));ship.m_floatCollider.center=Vector3.zero;
        ship.m_floatCollider.size=new Vector3(spec.beam,.5f,spec.length);ship.m_floatCollider.isTrigger=true;
        var onboard=prefab.GetComponentsInChildren<BoxCollider>(true).FirstOrDefault(c=>c.name=="OnboardTrigger");
        if(!onboard)throw new InvalidDataException("Native onboard trigger missing for "+name);
        Rehome(onboard.transform,root,new Vector3(0,spec.walkHeight+.8f,0));onboard.center=Vector3.zero;onboard.size=new Vector3(spec.beam,3.4f,spec.length);onboard.isTrigger=true;
        AddWaterMask(root,spec,maskMaterial);
        var body=prefab.GetComponent<Rigidbody>();body.centerOfMass=new Vector3(0,.25f,0);
        ship.m_stearForceOffset=-spec.length*.35f;ship.m_forceDistance=Mathf.Min(2,spec.length*.16f);
        // Native buoyancy applies mass * (dt * 50) impulses. Account for its
        // equilibrium immersion, otherwise these low soles would sit underwater.
        float immersion=Mathf.Abs(Physics.gravity.y)*ship.m_forceDistance/(50*Mathf.Max(.01f,ship.m_force));
        ship.m_waterLevelOffset=.25f-spec.waterline+immersion;ship.m_disableLevel=-.02f;
        foreach(var point in spec.points)
        {
            var position=V(point.position);var node=Node(point.kind,root,position);node.gameObject.layer=LayerMask.NameToLayer("piece_nonsolid");
            var facing=V(point.facing);if(facing.sqrMagnitude>.1f)node.localRotation=Quaternion.LookRotation(facing);
            if(point.kind=="helm")
            {
                Rehome(controls.transform,root,position);controls.gameObject.SetActive(true);controls.gameObject.layer=LayerMask.NameToLayer("piece_nonsolid");controls.enabled=true;controls.m_attachPoint=node;controls.m_attachAnimation=spec.name=="freighter"?"attach_mast":spec.name=="snekkja"?"sit":"attach_sitship";controls.m_maxUseRange=2.5f;controls.m_detachOffset=new Vector3(0,.1f,.7f);
                var collider=controls.gameObject.AddComponent<BoxCollider>();collider.size=new Vector3(.70f,.65f,.55f);collider.center=new Vector3(0,spec.name=="freighter"?.85f:.25f,0);
                ship.m_controlGuiPos=Node("Helm UI",root,position+Vector3.up);
                if(PaddleCraft(name))controls.m_attachAnimation="sit";
            }
            else if(point.kind=="ladder")
            {
                var ladder=node.gameObject.AddComponent<Ladder>();ladder.m_name="$piece_ship_ladder";ladder.m_useDistance=3;ladder.m_targetPos=Node("Boarding exit",root,V(point.exit));ladder.m_targetPos.localRotation=node.localRotation;node.localRotation=Quaternion.identity;
                var box=node.gameObject.AddComponent<BoxCollider>();box.size=V(point.size);
            }
            else
            {
                var chair=node.gameObject.AddComponent<Chair>();chair.m_name=point.kind=="mast"?"Mast holdfast":"Seat";chair.m_inShip=true;chair.m_attachPoint=node;chair.m_attachAnimation=point.kind=="mast"?"attach_mast":"sit";chair.m_useDistance=2.5f;chair.m_detachOffset=new Vector3(0,.12f,.6f);
                var box=node.gameObject.AddComponent<BoxCollider>();box.size=point.kind=="mast"?new Vector3(.24f,1.2f,.24f):new Vector3(.45f,.12f,.25f);box.center=point.kind=="mast"?Vector3.up*.65f:Vector3.down*.08f;
            }
        }
        // Keep each existing container and slot identity, including old multi-hold cargo.
        // The new interaction is attached to visible packed cargo, without restoring old chest solids.
        for(int i=0;i<holds.Length;i++)
        {
            var hold=holds[i];var position=new Vector3((i%2==0?1:-1)*spec.beam*.17f,(spec.cargoHeight>0?spec.cargoHeight:spec.walkHeight+.06f),-spec.length*.13f+(i/2)*.85f);
            if(PaddleCraft(name))position=new Vector3(0,spec.name=="dugout"?.415f:.68f,spec.name=="tandem"?0:spec.length*.29f);
            if(spec.name=="ceol")position=new Vector3(-.27f,.65f,-1.28f);
            if(spec.name=="currach")position=new Vector3(.32f,.5f,-.8f);
            if(spec.name=="snekkja")position=new Vector3(i%2==0?-.3f:.3f,1.04f,i<2?-6.3f:6.3f);
            if(spec.name=="falkusa")position=new Vector3(.35f,.67f,i==0?.55f:-.6f);
            Rehome(hold.transform,root,position);hold.gameObject.layer=LayerMask.NameToLayer("piece_nonsolid");hold.m_open=null!;hold.m_closed=null!;
            var box=hold.gameObject.AddComponent<BoxCollider>();box.size=new Vector3(.50f,.10f,.50f);
            if(name=="HelmsmanCurrach"){hold.m_width=3;hold.m_height=2;}
        }
        if(name=="HerculeShip")
        {
            var node=Node("Fishing net",root,new Vector3(spec.beam*.57f,0,0));
            node.gameObject.layer=LayerMask.NameToLayer("character_trigger");var catchBox=node.gameObject.AddComponent<BoxCollider>();catchBox.isTrigger=true;catchBox.size=new Vector3(.6f,1.3f,2.2f);
            var net=node.gameObject.AddComponent<ShipFishingNet>();net.Chest=holds[0];
            net.Visual=Node("Deployed net",node,Vector3.zero).gameObject;
            for(int i=0;i<12;i++)AddNetLine(net.Visual.transform,new Vector3(0,.55f,-1+i*.18f),new Vector3(.25f,-.6f,-1+i*.18f),rig.RopeMaterial);
            for(int i=0;i<7;i++)AddNetLine(net.Visual.transform,new Vector3(i*.25f/6,.55f-i*1.15f/6,-1),new Vector3(i*.25f/6,.55f-i*1.15f/6,1),rig.RopeMaterial);
            var handle=Node("Net handle",root,new Vector3(spec.beam*.36f,spec.walkHeight+.3f,0));handle.gameObject.layer=LayerMask.NameToLayer("piece_nonsolid");handle.gameObject.AddComponent<NetInteraction>().Net=net;handle.gameObject.AddComponent<BoxCollider>().size=new Vector3(.22f,.3f,.3f);
        }
    }
    private static void AddWaterMask(Transform root,Spec spec,Material material)
    {
        if(spec.waterMask.Length<4||spec.waterMask.Length%2!=0)throw new InvalidDataException("Missing fitted water mask");
        var vertices=spec.waterMask.Select(V).ToArray();var triangles=new List<int>();
        for(int i=2;i<vertices.Length;i+=2)
        {int a=i-2;triangles.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
        var mesh=new Mesh{name="Fitted interior water mask"};owned.Add(mesh);mesh.vertices=vertices;mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        var node=Node("Interior water mask",root,Vector3.zero);node.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=node.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
    }
    private static void AddNetLine(Transform parent,Vector3 a,Vector3 b,Material material)
    {var node=Node("Net cord",parent,Vector3.zero);var line=node.gameObject.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=2;line.SetPosition(0,a);line.SetPosition(1,b);line.startWidth=line.endWidth=.012f;line.sharedMaterial=material;}
    internal static void ConfigureStyles(GameObject prefab,ShipCosmetics cosmetics)
    {
        if(!prefab.GetComponent<FinalShipPresentation>())return;
        cosmetics.Binding=new ShipCosmeticBinding{prefab=prefab.name};
        Material Choice(string name,Color color){var m=ImportedShipMaterials.BoatyardMaterial(color,false);m.name=name;owned.Add(m);return m;}
        cosmetics.HullStyles=new[]{Choice("Weathered timber",Color.white),Choice("Red ochre",new Color(.65f,.25f,.14f)),Choice("Deep blue",new Color(.21f,.39f,.57f)),Choice("Ochre",new Color(.82f,.62f,.27f)),Choice("Carved timber",Color.white)};
        if(prefab.name!="MercantShip"&&prefab.name!="BigCargoShip")cosmetics.HullStyles=cosmetics.HullStyles.Take(4).ToArray();
        cosmetics.SailStyles=PaddleCraft(prefab.name)?Array.Empty<Material>():new[]{Choice("Original cloth",Color.white),Choice("Unbleached linen",new Color(1,.95f,.8f)),Choice("Red wool",new Color(.65f,.23f,.16f)),Choice("Blue wool",new Color(.37f,.54f,.7f))};
    }
    internal static void Release(){foreach(var obj in owned)if(obj)Object.Destroy(obj);owned.Clear();cache.Clear();}
}

public sealed class FinalShipPresentation : MonoBehaviour
{
    public Transform Sail=null!,Rudder=null!;public GameObject[] Decorations=Array.Empty<GameObject>();public Vector3 Pivot;public Vector3[] SheetHeads=Array.Empty<Vector3>(),SheetFeet=Array.Empty<Vector3>();
    public Material RopeMaterial=null!;public MeshRenderer[] HullRenderers=Array.Empty<MeshRenderer>(),SailRenderers=Array.Empty<MeshRenderer>();
    public float AirHeight;public FleetSailMotion? Motion;public bool Sculling;
    private LineRenderer[] sheets=Array.Empty<LineRenderer>();
    private readonly Dictionary<Renderer,Material> originals=new();private readonly List<Material> painted=new();
    private void Start()
    {
        if(ZNet.instance&&ZNet.instance.IsDedicated())return;
        sheets=new LineRenderer[SheetHeads.Length];
        for(int i=0;i<sheets.Length;i++){var go=new GameObject("Working sheet");go.transform.SetParent(transform,false);var line=go.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=9;line.startWidth=line.endWidth=.018f;line.sharedMaterial=RopeMaterial;sheets[i]=line;}
    }
    private void LateUpdate()
    {
        if(!Sail)return;
        var ship=GetComponent<Ship>();if(Rudder&&ship)
        {
            bool rowing=Sculling&&(ship.GetSpeedSetting()==Ship.Speed.Slow||ship.GetSpeedSetting()==Ship.Speed.Back);
            float stroke=rowing?Mathf.Sin(Time.time*2.4f)*5:0;
            Rudder.localRotation=Quaternion.Euler(0,Mathf.Clamp(-8*ship.GetRudderValue()+stroke,-8,8),0);
        }
        for(int i=0;i<sheets.Length;i++)
        {
            var head=Motion?Motion.Furl(SheetHeads[i]):Pivot+Vector3.Scale(SheetHeads[i]-Pivot,Sail.localScale);
            for(int j=0;j<9;j++){float t=j/8f;var p=Vector3.Lerp(head,SheetFeet[i],t);p.y-=Mathf.Sin(t*Mathf.PI)*.09f;sheets[i].SetPosition(j,p);}
        }
    }
    internal void Paint(int hull,int sail,Material[] hullStyles,Material[] sailStyles)
    {
        foreach(var m in painted)if(m)Object.Destroy(m);painted.Clear();
        void Apply(MeshRenderer[] renderers,int choice,Material[] choices)
        {
            foreach(var renderer in renderers)
            {
                if(!originals.TryGetValue(renderer,out var original)){original=renderer.sharedMaterial;originals[renderer]=original;}
                if(choice<=0||choice>=choices.Length){renderer.sharedMaterial=original;continue;}
                var material=new Material(original);material.color=choices[choice].color;renderer.sharedMaterial=material;painted.Add(material);
            }
        }
        foreach(var decoration in Decorations)if(decoration)decoration.SetActive(hull==4);
        Apply(HullRenderers,hull,hullStyles);Apply(SailRenderers,sail,sailStyles);
    }
    private void OnDestroy(){foreach(var material in painted)if(material)Object.Destroy(material);}
}
