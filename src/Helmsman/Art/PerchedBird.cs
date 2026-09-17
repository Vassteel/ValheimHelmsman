#nullable enable annotations
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

namespace VikingBirds;

// Authored smooth surfaces with overlapping feather and garment detail. All renderers use private, non-emissive world materials.
// The feet stay at y=0; body, neck, wings and tools have separate animation pivots.
internal sealed class PerchedBird : IDisposable
{
    internal readonly GameObject Root;
    private readonly Transform rig,body,head,leftWing,rightWing,leftFoot,rightFoot;
    private readonly List<Object> assets=new();
    private readonly Dictionary<string,Material> palette=new();
    private readonly Material source;
    private Texture2D? surface;
    private readonly bool owl;
    private readonly bool pelican;
    private readonly float bodyHeight;
    private readonly List<Transform> eyes=new();
    private float rest;
    private readonly GameObject hammer,chisel,needle,sailcloth;
    private GameObject? fishingPole;
    private readonly Vector3 headRest=new(0,.47f,.035f);
    internal Vector3 BeakWorld=>head.TransformPoint(new Vector3(0,.10f,owl?.27f:pelican?.73f:.38f));
    internal PerchedBird(Transform parent,Material worldMaterial,bool burrowingOwl,bool fishingPelican=false)
    {
        source=worldMaterial;owl=burrowingOwl;pelican=fishingPelican;
        bodyHeight=owl?.29f:pelican?.26f:.18f;
        if(pelican)headRest=new Vector3(0,.68f,.035f);
        Root=new GameObject(owl?"Quartermaster burrowing owl":pelican?"Pelican fisherman":"Puffin shipwright");Root.transform.SetParent(parent,false);
        try
        {
        rig=Node("Performance pivot",Root.transform,Vector3.zero);
        body=Node("Body pivot",rig,new Vector3(0,bodyHeight,0));
        head=Node("Neck pivot",body,headRest);
        leftWing=Node("Left wing",body,new Vector3(-.22f,.34f,-.02f));
        rightWing=Node("Right wing",body,new Vector3(.22f,.34f,-.02f));
        leftFoot=Node("Left foot pivot",rig,Vector3.zero);
        rightFoot=Node("Right foot pivot",rig,Vector3.zero);
        var brown=new Color(.28f,.20f,.12f);var cream=new Color(.68f,.63f,.48f);
        var dark=new Color(.085f,.09f,.095f);var orange=new Color(.64f,.25f,.07f);
        var feather=owl?brown:pelican?cream:dark;var feet=owl?new Color(.52f,.43f,.29f):orange;
        for(int side=-1;side<=1;side+=2)
        {
            var foot=side<0?leftFoot:rightFoot;
            if(pelican&&side<0)
            {PegLeg(foot,brown);continue;}
            float leg=owl?.18f:pelican?.16f:.115f;
            Ellipsoid("Leg",foot,new(side*.105f,leg,0),new(.027f,leg,.026f),feet,6,3);
            for(int toe=-1;toe<=1;toe++)
                Rod("Toe",foot,new(side*.105f,.02f,0),new(side*.105f+toe*.047f,.015f,.145f-Math.Abs(toe)*.03f),.014f,feet);
            if(!owl) Webbing(foot,side,feet);
            Rod("Back toe",foot,new(side*.105f,.02f,0),new(side*.13f,.012f,-.07f),.012f,dark);
        }
        Ellipsoid("Body",body,new(0,.29f,-.035f),new(.245f,.34f,.195f),feather,12,7);
        Ellipsoid("Breast",body,new(0,.30f,.09f),new(.19f,.30f,.12f),cream,10,6);
        if(pelican)Ellipsoid("Long white neck",body,new(0,.56f,.015f),new(.075f,.25f,.08f),cream,9,6);
        Ellipsoid("Head",head,new(0,.105f,0),new(.24f,.215f,.19f),feather,12,7);
        for(int side=-1;side<=1;side+=2)
        {
            Transform wing=side<0?leftWing:rightWing;
            Ellipsoid("Folded wing",wing,new(side*.012f,-.12f,-.045f),new(.066f,.22f,.105f),feather,8,5);
            for(int f=0;f<4;f++)
                Ellipsoid("Flight feather",wing,new(side*.005f,-.20f+f*.017f,-.105f+f*.035f),new(.035f,.135f,.029f),feather*.82f,6,3);
            if(!owl) WingFeathers(wing,side,feather);
            Ellipsoid("Face",head,new(side*.113f,.12f,.11f),new(.118f,.142f,.086f),cream,8,5);
            var eye=Node("Blink",head,new(side*(owl?.125f:.16f),.15f,owl?.19f:.18f));eyes.Add(eye);
            Ellipsoid("Eye surround",eye,Vector3.zero,new(owl?.072f:.043f,owl?.077f:.049f,.028f),dark,10,3);
            Ellipsoid("Eye",eye,new(0,0,.024f),new(owl?.053f:.028f,owl?.055f:.032f,.014f),owl?new Color(.63f,.55f,.10f):new Color(.24f,.12f,.045f),10,3);
            Ellipsoid("Pupil",eye,new(0,0,.037f),new(owl?.024f:.021f,owl?.032f:.028f,.009f),new Color(.012f,.015f,.018f),8,3);
            if(owl)
            {
                var brow=Ellipsoid("Cream eyebrow",head,new(side*.115f,.232f,.16f),new(.13f,.025f,.032f),cream,6,3);
                brow.localRotation=Quaternion.Euler(0,0,-side*13);
                for(int f=0;f<7;f++)
                    Ellipsoid("Mottled wing spot",wing,new(side*.068f,-.02f-f*.034f,-.085f+(f%2)*.07f),new(.007f,.012f,.021f),cream,5,2);
            }
        }
        if(owl) Wedge("Beak",head,new(0,.065f,.18f),new(.09f,.11f,.12f),new Color(.4f,.36f,.23f));
        else DetailedBill();
        if(owl)
        {
            var cloth=new Color(.23f,.27f,.16f);
            for(int side=-1;side<=1;side+=2)ClothPanel("Fitted cloth waistcoat",body,side,cloth);
            for(int i=0;i<3;i++)
            {float y=.30f-i*.075f;Ellipsoid("Wooden button",body,new(.008f,y,ChestSurface(.008f,y)+.025f),new(.009f,.009f,.004f),brown,6,2);}
            var leather=new Color(.22f,.13f,.065f);
            var cap=Node("Crooked leather cap",head,new(0,.28f,0));cap.localRotation=Quaternion.Euler(0,0,-9);
            Ellipsoid("Cap crown",cap,Vector3.zero,new(.248f,.09f,.20f),leather,12,4);
            Ellipsoid("Short brim",cap,new(0,-.031f,.16f),new(.25f,.025f,.14f),leather,10,3);
            Ring("Keyring",body,new(.19f,.15f,.17f),.045f,.006f,new Color(.21f,.21f,.18f));
            for(int k=0;k<3;k++)
            {
                var a=new Vector3(.16f+k*.025f,.12f,.18f);
                Rod("Key shaft",body,a,a+new Vector3(.01f,-.085f,0),.007f,dark);
                Rod("Key bit",body,a+new Vector3(.01f,-.075f,0),a+new Vector3(.035f,-.075f,0),.01f,dark);
            }
        }
        else if(pelican)
        {
            var oilcloth=new Color(.32f,.30f,.16f);
            FisherHat(head,oilcloth);
            Rod("Hat cord",head,new(-.19f,.18f,.1f),new(-.1f,-.075f,.14f),.009f,brown);
        }
        else
        {
            var leather=new Color(.31f,.20f,.10f);var wool=new Color(.12f,.17f,.22f);
            ClothPanel("Fitted leather apron",body,0,leather);
            float pocketZ=ChestSurface(.06f,.19f)+.025f;
            Ellipsoid("Apron pocket",body,new(.06f,.19f,pocketZ),new(.047f,.037f,.005f),leather*.8f,4,3);
            Rod("Carpenter pencil",body,new(.075f,.20f,pocketZ+.006f),new(.083f,.30f,pocketZ-.014f),.009f,new Color(.63f,.43f,.15f));
            Ellipsoid("Knit cap",head,new(-.012f,.28f,-.015f),new(.218f,.12f,.185f),wool,12,6);
            Ellipsoid("Folded wool brim",head,new(0,.25f,0),new(.228f,.036f,.189f),wool*1.2f,12,3);
            Ellipsoid("Fuzzy pompom",head,new(-.03f,.405f,-.03f),new(.065f,.060f,.061f),cream,9,5);
            for(int side=-1;side<=1;side+=2)
                Rod("Apron strap",body,new(side*.16f,.38f,.16f),new(side*.10f,.52f,.08f),.018f,leather);
        }
        hammer=Tool("Wooden hammer",head);chisel=Tool("Chisel",head);needle=Tool("Sail needle",head);
        Rod("Handle",hammer.transform,new(0,0,0),new(.23f,0,0),.018f,new Color(.36f,.23f,.10f));
        Ellipsoid("Hammer head",hammer.transform,new(.24f,0,0),new(.043f,.09f,.042f),brown,4,3);
        Rod("Chisel handle",chisel.transform,Vector3.zero,new(0,-.085f,.02f),.024f,brown);
        Rod("Chisel blade",chisel.transform,new(0,-.07f,.02f),new(0,-.20f,.045f),.011f,new Color(.27f,.28f,.27f));
        Rod("Needle",needle.transform,Vector3.zero,new(.16f,0,.06f),.005f,new Color(.35f,.36f,.33f));
        sailcloth=new GameObject("Sailcloth sample");sailcloth.transform.SetParent(leftWing,false);
        Ellipsoid("Linen",sailcloth.transform,new(-.025f,-.05f,.20f),new(.11f,.025f,.15f),cream,6,3);
        if(pelican)
        {
            fishingPole=new GameObject("Fishing pole");fishingPole.transform.SetParent(leftWing,false);
            fishingPole.transform.localPosition=new Vector3(.08f,-.09f,.15f);
            Rod("Ash rod",fishingPole.transform,Vector3.zero,new Vector3(0,.60f,.85f),.012f,brown);
            Rod("Flexible rod tip",fishingPole.transform,new Vector3(0,.60f,.85f),new Vector3(0,.55f,1.1f),.006f,brown);
            Rod("Fishing line",fishingPole.transform,new Vector3(0,.55f,1.1f),new Vector3(0,-.65f,1.35f),.0025f,cream);
            Ellipsoid("Cork float",fishingPole.transform,new Vector3(0,-.63f,1.35f),new Vector3(.018f,.036f,.018f),orange,6,3);
        }
        if(!owl) TailFeathers(feather);
        if(!owl) GarmentDetails();
        SetTool(0);
        MergeStaticParts();
        }
        catch{Dispose();throw;}
    }
    private void Feather(Transform parent,Vector3 root,Vector3 tip,float width,Color color,int side)
    {
        // Smooth lenticular blade with a raised rachis and pointed tip. The wing
        // stays a single animated pivot after material batching.
        var axis=tip-root;var across=new Vector3(0,0,width);
        var normal=new Vector3(side*.009f,0,0);
        var v=new List<Vector3>{root,root+axis*.28f+across,root+axis*.68f+across*.7f,tip,
            root+axis*.68f-across*.7f,root+axis*.28f-across,root+axis*.43f+normal,root+axis*.43f-normal*.4f};
        var faces=new List<int>();
        for(int i=0;i<6;i++)
        {if(side>0)faces.AddRange(new[]{i,6,(i+1)%6,i,(i+1)%6,7});else faces.AddRange(new[]{i,(i+1)%6,6,i,7,(i+1)%6});}
        MeshPart("Layered feather",parent,Vector3.zero,v,faces,color);
    }
    private void WingFeathers(Transform wing,int side,Color color)
    {
        for(int row=0;row<3;row++)for(int f=0;f<5;f++)
        {
            float z=-.115f+f*.036f,y=.035f-row*.065f;
            float x=side*(.053f+.011f*(float)Math.Sin((f+1)*Math.PI/6));
            Feather(wing,new(x,y,z),new(x+side*.005f,y-.115f,z-.018f),.026f,color,side);
        }
        for(int f=0;f<6;f++)
            Feather(wing,new(side*.05f,-.12f,-.125f+f*.037f),new(side*.035f,-.345f+f*.017f,-.16f+f*.04f),.026f,color*.82f,side);
    }
    private void TailFeathers(Color color)
    {
        for(int f=0;f<5;f++)
        {
            var t=Ellipsoid("Tail flight feather",body,new((f-2)*.031f,.045f,-.21f),new(.031f,.12f,.024f),color*.82f,6,5);
            t.localRotation=Quaternion.Euler(30,0,(f-2)*-7);
        }
    }
    private void PegLeg(Transform foot,Color wood)
    {
        const int sides=16;var v=new List<Vector3>();var faces=new List<int>();
        for(int row=0;row<3;row++)for(int i=0;i<sides;i++)
        {
            float y=row==0?0:row==1?.04f:.32f,r=row==1?.034f:.027f,a=i*2*Mathf.PI/sides;
            v.Add(new Vector3(-.105f+r*Mathf.Cos(a),y,r*Mathf.Sin(a)));
        }
        for(int row=0;row<2;row++)for(int i=0;i<sides;i++)
        {int a=row*sides+i,b=row*sides+(i+1)%sides,c=a+sides,d=b+sides;faces.AddRange(new[]{a,b,c,b,d,c});}
        v.Add(new Vector3(-.105f,0,0));v.Add(new Vector3(-.105f,.32f,0));
        for(int i=0;i<sides;i++){int j=(i+1)%sides;faces.AddRange(new[]{48,j,i,49,32+i,32+j});}
        MeshPart("Wooden peg leg",foot,Vector3.zero,v,faces,wood);
    }
    private void Webbing(Transform foot,int side,Color color)
    {
        float x=side*.105f;
        var v=new List<Vector3>{new(x,.024f,.024f),new(x-.047f,.016f,.11f),new(x-.018f,.017f,.128f),
            new(x,.016f,.145f),new(x+.018f,.017f,.128f),new(x+.047f,.016f,.11f)};
        var faces=new List<int>();for(int i=1;i<5;i++)faces.AddRange(new[]{0,i+1,i});
        MeshPart("Webbed foot",foot,Vector3.zero,v,faces,color);
        for(int k=0;k<4;k++)Rod("Foot scale",foot,new(x-.016f,.04f+k*.022f,.018f),new(x+.016f,.04f+k*.022f,.018f),.002f,color*.82f);
    }
    private void DetailedBill()
    {
        var amber=new Color(.70f,.36f,.095f);var seam=new Color(.25f,.15f,.075f);
        if(pelican)
        {
            // Dorsal bill, tapered lower mandible and soft suspended throat pouch.
            Ellipsoid("Long upper bill",head,new(0,.07f,.426f),new(.068f,.045f,.282f),amber,12,8);
            Ellipsoid("Lower mandible",head,new(0,.025f,.405f),new(.061f,.025f,.263f),amber*.9f,12,8);
            Ellipsoid("Throat pouch",head,new(0,-.045f,.35f),new(.075f,.105f,.204f),new Color(.63f,.43f,.24f),12,8);
            for(int side=-1;side<=1;side+=2)
            {
                Rod("Bill mouth seam",head,new(side*.065f,.04f,.23f),new(side*.025f,.038f,.665f),.003f,seam);
                Ellipsoid("Nostril",head,new(side*.063f,.087f,.275f),new(.003f,.007f,.018f),seam,6,4);
                for(int i=0;i<4;i++)Rod("Pouch fold",head,new(side*(.03f+i*.008f),-.10f+i*.015f,.30f),new(side*.022f,-.026f,.52f),.002f,new Color(.56f,.37f,.20f));
            }
            Ellipsoid("Bill hook",head,new(0,.058f,.699f),new(.018f,.034f,.026f),amber,8,6);
        }
        else
        {
            // Puffin bill is a deep, laterally compressed curved wedge, with
            // alternating keratin bands and a continuous mouth seam.
            var v=new List<Vector3>();var faces=new List<int>();const int rows=12,segments=24;
            for(int row=0;row<=rows;row++)
            {
                float t=row/(float)rows,z=.175f+t*.235f;
                float width=.068f*(1-t*t)+.002f,height=.112f*(1-t)+.003f;
                for(int i=0;i<segments;i++)
                {float a=i*2*Mathf.PI/segments;v.Add(new(width*Mathf.Cos(a),.065f+height*Mathf.Sin(a)-.025f*t,z));}
            }
            for(int row=0;row<rows;row++)for(int i=0;i<segments;i++)
            {int a=row*segments+i,b=row*segments+(i+1)%segments,c=a+segments,d=b+segments;faces.AddRange(new[]{a,c,b,b,c,d});}
            MeshPart("Curved puffin bill",head,Vector3.zero,v,faces,new Color(.79f,.25f,.055f));
            for(int row=2;row<=6;row+=2)
            {
                float t=row/(float)rows,z=.175f+t*.235f,width=.068f*(1-t*t)+.003f,height=.112f*(1-t)+.004f;
                for(int i=0;i<12;i++)
                {
                    float a=i*2*Mathf.PI/12,b=(i+1)*2*Mathf.PI/12;
                    Rod("Bill keratin band",head,new(width*Mathf.Cos(a),.065f+height*Mathf.Sin(a)-.025f*t,z),new(width*Mathf.Cos(b),.065f+height*Mathf.Sin(b)-.025f*t,z),.003f,amber);
                }
            }
            for(int side=-1;side<=1;side+=2)
            {
                Rod("Bill mouth seam",head,new(side*.067f,.028f,.19f),new(side*.004f,.038f,.407f),.003f,seam);
                Ellipsoid("Bill rosette",head,new(side*.069f,.035f,.181f),new(.009f,.019f,.018f),amber,7,5);
                Rod("Puffin eye accent",head,new(side*.178f,.121f,.197f),new(side*.17f,.08f,.197f),.005f,seam);
            }
        }
    }
    private void GarmentDetails()
    {
        if(pelican)
        {
            var cloth=new Color(.32f,.30f,.16f);
            for(int i=0;i<32;i++)
            {
                float a=i*2*Mathf.PI/32,b=(i+.55f)*2*Mathf.PI/32;
                Rod("Hat brim stitch",head,new(.303f*Mathf.Cos(a),.244f,.249f*Mathf.Sin(a)),new(.303f*Mathf.Cos(b),.244f,.249f*Mathf.Sin(b)),.0018f,cloth*1.2f);
            }
            for(int side=-1;side<=1;side+=2)
            {
                Rod("Hat band",head,new(side*.25f,.265f,-.04f),new(side*.24f,.265f,.066f),.009f,cloth*.8f);
                Ellipsoid("Hat eyelet",head,new(side*.245f,.295f,.018f),new(.003f,.009f,.009f),new Color(.27f,.24f,.16f),6,4);
            }
        }
        else
        {
            var leather=new Color(.31f,.20f,.10f);var thread=new Color(.50f,.37f,.22f);var wool=new Color(.12f,.17f,.22f);
            for(int side=-1;side<=1;side+=2)
            {
                for(int i=0;i<17;i++)
                {
                    float y=.085f+i*.021f;float x=side*(.14f+.043f*(float)Math.Sin((y-.085f)/.38f*Math.PI));
                    Rod("Apron stitch",body,new(x,y,ChestSurface(x,y)+.022f),new(x,y+.009f,ChestSurface(x,y+.009f)+.022f),.0018f,thread);
                }
                Ellipsoid("Apron strap rivet",body,new(side*.15f,.395f,ChestSurface(side*.15f,.395f)+.025f),new(.007f,.007f,.004f),thread,6,4);
            }
            for(int i=0;i<40;i++)
            {
                float a=i*2*Mathf.PI/40;
                Rod("Wool cuff rib",head,new(.227f*Mathf.Cos(a),.235f,.189f*Mathf.Sin(a)),new(.227f*Mathf.Cos(a),.266f,.189f*Mathf.Sin(a)),.0025f,wool*1.2f);
            }
            for(int i=0;i<24;i++)
            {
                float y=1-2*(i+.5f)/24,a=i*2.399963f,r=(float)Math.Sqrt(1-y*y);
                Ellipsoid("Pompom tuft",head,new(-.03f+.058f*r*Mathf.Cos(a),.405f+.054f*y,-.03f+.054f*r*Mathf.Sin(a)),new(.023f,.023f,.023f),new Color(.68f,.63f,.48f),6,4);
            }
        }
    }
    private void FisherHat(Transform parent,Color color)
    {
        // One continuous cloth shell: no intersecting crown/brim ellipsoids.
        float[] y={.224f,.238f,.258f,.328f,.398f,.418f};
        float[] radius={.30f,.32f,.255f,.23f,.16f,0};
        const int sides=32;var vertices=new List<Vector3>();var faces=new List<int>();var tex=new List<Vector2>();
        for(int row=0;row<y.Length;row++)for(int side=0;side<=sides;side++)
        {tex.Add(new Vector2(side/(float)sides,row/(float)(y.Length-1)));float a=side*2*Mathf.PI/sides;vertices.Add(new Vector3(radius[row]*Mathf.Cos(a),y[row],radius[row]*.82f*Mathf.Sin(a)));}
        for(int row=0;row<y.Length-1;row++)for(int side=0;side<sides;side++)
        {int a=row*(sides+1)+side,b=a+1,c=a+sides+1,d=b+sides+1;faces.AddRange(new[]{a,b,c,b,d,c});}
        MeshPart("Fisherman's cloth hat",parent,Vector3.zero,vertices,faces,color,tex);
    }
    private static float ChestSurface(float x,float y)
    {
        float Surface(float rx,float ry,float cy,float cz,float rz)
            =>cz+rz*(float)Math.Sqrt(Math.Max(0,1-x*x/(rx*rx)-(y-cy)*(y-cy)/(ry*ry)));
        return Math.Max(Surface(.245f,.34f,.29f,-.035f,.195f),Surface(.19f,.30f,.30f,.09f,.12f));
    }
    private void ClothPanel(string name,Transform parent,int side,Color color)
    {
        var v=new List<Vector3>();var faces=new List<int>();var tex=new List<Vector2>();
        float[] ys={.045f,.09f,.14f,.20f,.26f,.32f,.38f,.425f,.47f};
        float[] outer={.115f,.15f,.177f,.188f,.19f,.184f,.17f,.15f,.12f};
        float[] inner={.012f,.01f,.008f,.008f,.008f,.02f,.035f,.067f,.103f};
        const int columns=20;
        for(int row=0;row<ys.Length;row++)for(int col=0;col<=columns;col++)
        {
            float t=col/(float)columns;
            float x=side==0?(-outer[row]+2*outer[row]*t):side*(inner[row]+(outer[row]-inner[row])*t);
            tex.Add(new Vector2(t,row/(float)(ys.Length-1)));
            float y=ys[row];v.Add(new Vector3(x,y,ChestSurface(x,y)+.018f));
        }
        for(int row=0;row<ys.Length-1;row++)for(int col=0;col<columns;col++)
        {
            int a=row*(columns+1)+col,b=a+1,c=a+columns+1,d=c+1;
            // MeshPart reverses its input winding. Both panels must face outward.
            if(side<0)faces.AddRange(new[]{a,b,c,b,d,c});else faces.AddRange(new[]{a,c,b,b,c,d});
        }
        MeshPart(name,parent,Vector3.zero,v,faces,color,tex);
    }
    private void MergeStaticParts()
    {
        var pivots=new HashSet<Transform>{Root.transform,rig,body,head,leftWing,rightWing,leftFoot,rightFoot,hammer.transform,chisel.transform,needle.transform,sailcloth.transform};
        foreach(var eye in eyes)pivots.Add(eye);
        if(fishingPole)pivots.Add(fishingPole.transform);
        var groups=Root.GetComponentsInChildren<MeshRenderer>(true).GroupBy(r=>
        {
            var pivot=r.transform.parent;while(pivot&&!pivots.Contains(pivot))pivot=pivot.parent;
            return (Pivot:pivot,Material:r.sharedMaterial);
        }).ToArray();
        foreach(var group in groups)
        {
            var parts=group.ToArray();if(parts.Length<2||!group.Key.Pivot)continue;
            var parent=group.Key.Pivot;
            var merged=new Mesh{name="Bird combined surface"};
            merged.CombineMeshes(parts.Select(r=>new CombineInstance {mesh=r.GetComponent<MeshFilter>().sharedMesh,
                transform=parent.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray(),true,true);
            assets.Add(merged);
            var node=Node("Combined bird surface",parent,Vector3.zero);
            node.gameObject.AddComponent<MeshFilter>().sharedMesh=merged;
            var target=node.gameObject.AddComponent<MeshRenderer>();target.sharedMaterial=group.Key.Material;
            target.receiveShadows=true;target.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
            foreach(var renderer in parts)
            {
                var filter=renderer.GetComponent<MeshFilter>();var old=filter.sharedMesh;
                Object.DestroyImmediate(renderer);Object.DestroyImmediate(filter);assets.Remove(old);Object.DestroyImmediate(old);
            }
        }
    }
    private static Transform Node(string name,Transform parent,Vector3 position)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;}
    private GameObject Tool(string name,Transform parent)
    {var t=Node(name,parent,new Vector3(0,.07f,.30f));return t.gameObject;}
    internal void Fishing(bool active){if(fishingPole)fishingPole.SetActive(active);}
    internal void SetTool(int tool)
    {hammer.SetActive(tool==1);chisel.SetActive(tool==2);needle.SetActive(tool==3);sailcloth.SetActive(tool==3);}
    internal void Pose(float pitch,float yaw,float roll,float bodyPitch,float crouch,float wing=0)
    {
        rig.localPosition=Vector3.zero;rig.localRotation=Quaternion.identity;
        leftFoot.localPosition=rightFoot.localPosition=Vector3.zero;
        body.localPosition=new Vector3(0,bodyHeight-Mathf.Clamp(crouch,0,.13f),0);
        body.localRotation=Quaternion.Euler(bodyPitch,0,0);
        head.localPosition=headRest;head.localRotation=Quaternion.Euler(pitch,yaw,roll);
        leftWing.localRotation=Quaternion.Euler(-wing,0,wing*.4f);
        rightWing.localRotation=Quaternion.Euler(wing*.3f,0,-wing*.4f);
    }
    internal void Stage(Vector3 offset,float yaw,float tuck)
    {
        rig.localPosition=offset;rig.localRotation=Quaternion.Euler(0,yaw,0);
        leftFoot.localPosition=rightFoot.localPosition=Vector3.up*tuck;
    }
    internal void Idle(float time,bool fishing=false)
    {
        // Short actions separated by quiet holds, rather than constant head bobbing.
        float t=time%23f;
        float Pulse(float start,float duration)=>t>start&&t<start+duration?Mathf.Sin((t-start)/duration*Mathf.PI):0;
        float glance=Pulse(2,3)-Pulse(9,4);
        float preen=fishing?0:Pulse(16,2.8f);
        float settle=Pulse(20,1.4f)*Mathf.Sin(time*15);
        head.localRotation*=Quaternion.Euler(preen*32,glance*(pelican?24:38)+preen*65,glance*-9);
        head.localPosition+=new Vector3(0,.005f*Mathf.Sin(time*1.7f),pelican?.016f*glance:0);
        body.localPosition+=Vector3.up*(.006f*Mathf.Sin(time*1.7f));
        body.localRotation*=Quaternion.Euler(0,0,glance*(pelican?1.5f:3));
        leftWing.localRotation*=Quaternion.Euler(0,0,preen*18+settle*(fishing?1:5));
        rightWing.localRotation*=Quaternion.Euler(0,0,-settle*(fishing?2:7));
    }
    internal void Waddle(float phase,float amount)
    {
        float step=Mathf.Sin(phase),lift=Mathf.Abs(step);
        leftFoot.localPosition=new Vector3(0,Mathf.Max(0,step)*.075f,-Mathf.Cos(phase)*.055f)*amount;
        rightFoot.localPosition=new Vector3(0,Mathf.Max(0,-step)*.075f,Mathf.Cos(phase)*.055f)*amount;
        body.localPosition+=new Vector3(step*.025f,lift*.025f,0)*amount;
        body.localRotation*=Quaternion.Euler(7*amount,0,-step*11*amount);
        head.localRotation*=Quaternion.Euler(-5*amount,0,step*7*amount);
        leftWing.localRotation*=Quaternion.Euler(0,0,10*amount);
        rightWing.localRotation*=Quaternion.Euler(0,0,-10*amount);
    }
    internal void CloseEyes(float amount)
    {foreach(var eye in eyes)eye.localScale=new Vector3(1,1-.97f*Mathf.Clamp(amount,0,1),1);}
    internal void Rest(bool sleeping,float time,float delta)
    {
        rest=Mathf.MoveTowards(rest,sleeping?1:0,delta*(sleeping?.7f:5));
        float blink=time%5.3f>5.12f?Mathf.Sin((time%5.3f-5.12f)/.18f*Mathf.PI):0;
        CloseEyes(Mathf.Max(rest,blink));
        if(rest<=0)return;
        head.localRotation=Quaternion.Slerp(head.localRotation,Quaternion.Euler(34,owl?85:115,-12),rest);
        body.localPosition+=Vector3.up*(-.055f+.004f*Mathf.Sin(time*1.5f))*rest;
    }
    internal void Probes(Transform anchor)
    {foreach(var renderer in Root.GetComponentsInChildren<Renderer>(true))renderer.probeAnchor=anchor;}
    private Texture2D Surface()
    {
        if(surface)return surface;
        using var resource=typeof(PerchedBird).Assembly.GetManifestResourceStream("VikingBirds.detail.png");
        if(resource==null)return Texture2D.whiteTexture;
        using var stream=new MemoryStream();resource.CopyTo(stream);
        var loaded=new Texture2D(2,2,TextureFormat.RGBA32,true);
        if(!loaded.LoadImage(stream.ToArray())){Object.Destroy(loaded);return Texture2D.whiteTexture;}
        loaded.name="Bird detailed plumage";loaded.filterMode=FilterMode.Trilinear;loaded.wrapMode=TextureWrapMode.Repeat;
        loaded.Apply(true,true);surface=loaded;assets.Add(surface);return surface;
    }
    private Material Paint(Color color,int tile=-1,bool polished=false)
    {
        string key=ColorUtility.ToHtmlStringRGB(color)+" tile "+tile+(polished?" polished":"");
        if(palette.TryGetValue(key,out var found))return found;
        var m=new Material(source.shader){name="Bird matte "+key,color=color};
        if(m.HasProperty("_MainTex"))m.SetTexture("_MainTex",tile<0?Texture2D.whiteTexture:Surface());
        foreach(var p in new[]{"_EmissionColor","_EmissiveColor","_NoiseGlowColor"})if(m.HasProperty(p))m.SetColor(p,Color.black);
        foreach(var p in new[]{"_NoiseGlowEnabled","_Glossiness","_Metallic","_MetalGloss","_TriplanarMap","_ValueNoise","_ValueNoiseVertex","_AddRain","_AddSnow"})if(m.HasProperty(p))m.SetFloat(p,0);
        if(polished&&m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.45f);
        if(m.HasProperty("_MoveableObject"))m.SetFloat("_MoveableObject",1);
        m.DisableKeyword("_EMISSION");m.DisableKeyword("NOISEGLOW");m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        assets.Add(m);palette[key]=m;return m;
    }
    private static int SurfaceTile(string name)
    {
        name=name.ToLowerInvariant();
        if(name.Contains("wool")||name.Contains("knit")||name.Contains("pompom"))return 1;
        if(name.Contains("hat")||name.Contains("linen")||name.Contains("cloth"))return 2;
        if(name.Contains("apron")||name.Contains("leather")||name.Contains("peg")||name.Contains("cap crown")||name.Contains("short brim"))return 3;
        if(name.Contains("feather")||name=="body"||name=="breast"||name=="head"||name=="face"||name.Contains("neck")||name=="folded wing")return 0;
        return -1;
    }
    private Transform MeshPart(string name,Transform parent,Vector3 position,List<Vector3> vertices,List<int> triangles,Color color,List<Vector2>? coordinates=null,List<Vector3>? authoredNormals=null)
    {
        // Weld only coincident positions within this authored surface. Ellipsoid
        // poles then shade continuously; separate feather/garment edges stay sharp.
        var unique=new List<Vector3>();var map=new int[vertices.Count];
        var lookup=new Dictionary<(int,int,int),int>();
        for(int i=0;i<vertices.Count;i++)
        {
            if(coordinates!=null){map[i]=unique.Count;unique.Add(vertices[i]);continue;}
            var v=vertices[i];var key=((int)Math.Round(v.x*1000000),(int)Math.Round(v.y*1000000),(int)Math.Round(v.z*1000000));
            if(!lookup.TryGetValue(key,out int at)){at=unique.Count;lookup.Add(key,at);unique.Add(v);}map[i]=at;
        }
        var indices=new List<int>();
        for(int i=0;i<triangles.Count;i+=3)
        {
            int a=map[triangles[i]],b=map[triangles[i+2]],c=map[triangles[i+1]];
            if(a!=b&&b!=c&&c!=a&&Vector3.Cross(unique[b]-unique[a],unique[c]-unique[a]).sqrMagnitude>1e-16f)indices.AddRange(new[]{a,b,c});
        }
        int tile=SurfaceTile(name);
        float loU=unique.Min(v=>v.x+v.z*.65f),hiU=unique.Max(v=>v.x+v.z*.65f);
        float loV=unique.Min(v=>v.y+v.z*.25f),hiV=unique.Max(v=>v.y+v.z*.25f);
        var uv=unique.Select(v=>new Vector2((tile%2)*.5f+.018f+.464f*(v.x+v.z*.65f-loU)/Math.Max(.0001f,hiU-loU),
            (1-tile/2)*.5f+.018f+.464f*(v.y+v.z*.25f-loV)/Math.Max(.0001f,hiV-loV))).ToArray();
        if(coordinates!=null)uv=coordinates.Select(v=>new Vector2((tile%2)*.5f+.018f+.464f*v.x,(1-tile/2)*.5f+.018f+.464f*v.y)).ToArray();
        var mesh=new Mesh{name=name,vertices=unique.ToArray(),triangles=indices.ToArray(),uv=uv};
        mesh.RecalculateNormals();if(authoredNormals!=null)mesh.normals=authoredNormals.ToArray();mesh.RecalculateBounds();assets.Add(mesh);
        var t=Node(name,parent,position);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=t.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=Paint(color,tile,name=="Pupil"||name=="Eye");
        renderer.receiveShadows=true;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;return t;
    }
    private Transform Ellipsoid(string name,Transform parent,Vector3 position,Vector3 size,Color color,int sides,int rings,bool fine=true)
    {
        sides = fine?Math.Max(16,sides*3):8;
        rings = fine?Math.Max(8,rings*2):3;
        var v=new List<Vector3>();var faces=new List<int>();var tex=new List<Vector2>();var normals=new List<Vector3>();
        for(int j=0;j<=rings;j++)for(int i=0;i<=sides;i++)
        {
            float a=i*2*Mathf.PI/sides,b=j*Mathf.PI/rings;
            var n=new Vector3(Mathf.Sin(b)*Mathf.Cos(a),Mathf.Cos(b),Mathf.Sin(b)*Mathf.Sin(a));
            v.Add(Vector3.Scale(size,n));normals.Add(new Vector3(n.x/size.x,n.y/size.y,n.z/size.z).normalized);
            tex.Add(new Vector2(i/(float)sides,1-j/(float)rings));
        }
        for(int j=0;j<rings;j++)for(int i=0;i<sides;i++)
        {int a=j*(sides+1)+i,b=a+1,c=a+sides+1,d=b+sides+1;faces.AddRange(new[]{a,c,b,b,c,d});}
        return MeshPart(name,parent,position,v,faces,color,tex,normals);
    }

    private void Rod(string name,Transform parent,Vector3 a,Vector3 b,float radius,Color color)
    {var t=Ellipsoid(name,parent,(a+b)*.5f,new Vector3(radius,(b-a).magnitude*.5f,radius),color,6,3,false);t.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
    private void Wedge(string name,Transform parent,Vector3 position,Vector3 size,Color color)
    {
        var v=new List<Vector3>{new(-size.x*.5f,size.y*.5f,0),new(size.x*.5f,size.y*.5f,0),new(-size.x*.5f,-size.y*.5f,0),new(size.x*.5f,-size.y*.5f,0),new(0,-size.y*.3f,size.z)};
        var faces=new List<int>();
        int[] original={0,1,4,1,3,4,3,2,4,2,0,4,0,2,1,1,2,3};
        for(int i=0;i<original.Length;i+=3){int a=original[i],b=original[i+1],c=original[i+2],m=v.Count;v.Add((v[a]+v[b])*.5f);faces.AddRange(new[]{a,m,c,m,b,c});}
        MeshPart(name,parent,position,v,faces,color);
    }
    private void Ring(string name,Transform parent,Vector3 center,float radius,float wire,Color color)
    {for(int i=0;i<10;i++){float a=i*2*Mathf.PI/10,b=(i+1)*2*Mathf.PI/10;Rod(name,parent,center+new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*radius,center+new Vector3(Mathf.Cos(b),Mathf.Sin(b),0)*radius,wire,color);}}
    public void Dispose(){if(Root)Object.Destroy(Root);foreach(var asset in assets)if(asset)Object.Destroy(asset);assets.Clear();palette.Clear();}
}
