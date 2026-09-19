#nullable disable
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Helmsman.Structures
{
    internal sealed class TerrainPlan
    {
        private static readonly FieldInfo Level=Field("m_levelDelta"),Smooth=Field("m_smoothDelta"),Modified=Field("m_modifiedHeight"),Operations=Field("m_operations"),LastPoint=Field("m_lastOpPoint"),LastRadius=Field("m_lastOpRadius");
        private static readonly FieldInfo Paint=Field("m_paintMask"),PaintModified=Field("m_modifiedPaint");
        private static readonly MethodInfo Save=AccessTools.Method(typeof(TerrainComp),"Save"),CheckLoad=AccessTools.Method(typeof(TerrainComp),"CheckLoad"),BaseHeight=AccessTools.Method(typeof(Heightmap),"GetWorldBaseHeight");
        private static FieldInfo Field(string name)=>AccessTools.Field(typeof(TerrainComp),name)??throw new MissingFieldException("TerrainComp",name);
        private sealed class Node {internal int Index;internal float OldLevel,OldSmooth,NewLevel;internal bool OldModified;}
        private sealed class PaintNode {internal int Index;internal Color Old,Next;internal bool Modified;}
        private sealed class Tile
        {
            internal readonly List<PaintNode> Paints=new List<PaintNode>();
            internal Heightmap Map;internal TerrainComp Compiler;internal bool Applied;internal readonly List<Node> Nodes=new List<Node>();
        }
        private readonly List<Tile> tiles=new List<Tile>();
        private readonly Vector3 center;private readonly float radius;
        private bool applied;
        internal TerrainPlan(Vector3 position,float extent)
        {
            center=position;radius=extent;
            // Require all affected zones loaded, including map edges, before touching terrain.
            for(float z=-extent;z<=extent+8;z+=8)for(float x=-extent;x<=extent+8;x+=8)
                if(!Heightmap.FindHeightmap(position+new Vector3(Mathf.Min(x,extent),0,Mathf.Min(z,extent))))throw new InvalidOperationException("Move closer: the full terrain area must be loaded.");
            var maps=new List<Heightmap>();Heightmap.FindHeightmap(position,extent,maps);
            foreach(var map in maps)tiles.Add(new Tile{Map=map,Compiler=map.GetAndCreateTerrainCompiler()});
            if(tiles.Count==0)throw new InvalidOperationException("No loaded terrain here.");
        }
        internal void RequestOwnership(){foreach(var t in tiles)t.Compiler.GetComponent<ZNetView>().ClaimOwnership();}
        internal bool Owned
        {
            get{foreach(var t in tiles){if(!t.Compiler)return false;var view=t.Compiler.GetComponent<ZNetView>();if(!view||!view.IsValid()||!view.IsOwner())return false;}return true;}
        }
        internal void Prepare(Blueprint blueprint,Quaternion rotation,float width,float terrainHeight,bool shape)
        {
            if(!Owned)throw new InvalidOperationException("Terrain ownership was not granted.");
            var inverse=Quaternion.Inverse(rotation);
            foreach(var tile in tiles)
            {
                CheckLoad.Invoke(tile.Compiler,null);
                var map=tile.Map;map.Poke();
                var levels=(float[])Level.GetValue(tile.Compiler);var smooth=(float[])Smooth.GetValue(tile.Compiler);var flags=(bool[])Modified.GetValue(tile.Compiler);
                for(int z=0;z<=map.m_width;z++)for(int x=0;x<=map.m_width;x++)
                {
                    var world=map.transform.position+new Vector3((x-map.m_width*.5f)*map.m_scale,0,(z-map.m_width*.5f)*map.m_scale);
                    var local=inverse*(world-center);float distance=Geometry.Distance(new Point(local.x,local.z),blueprint.Hull);
                    if(distance>=width)continue;
                    if(!PrivateArea.CheckAccess(world,0,false,true))throw new InvalidOperationException("Terrain overlaps a protected ward.");
                    float old=map.GetHeight(x,z)+map.transform.position.y;
                    if(distance==0)
                    {
                        int pi=z*(map.m_width+1)+x;
                        var paints=(Color[])Paint.GetValue(tile.Compiler);var modifiedPaint=(bool[])PaintModified.GetValue(tile.Compiler);
                        var current=map.GetPaintMask(x,z);
                        // Preserve existing paving/cultivation; cleared dirt suppresses native grass.
                        if(current.r<=.5f&&current.g<=.5f&&current.b<=.5f)
                        {var cleared=Heightmap.m_paintMaskDirt;cleared.a=current.a;tile.Paints.Add(new PaintNode{Index=pi,Old=paints[pi],Modified=modifiedPaint[pi],Next=cleared});}
                    }
                    if(!shape)continue;
                    float desired=Geometry.FoundationHeight(old,center.y+terrainHeight,distance,width);
                    var args=new object[]{world,0f};
                    if(!(bool)BaseHeight.Invoke(map,args))throw new InvalidOperationException("Could not read base terrain.");
                    if(Math.Abs(desired-(float)args[1])>7.95f)throw new InvalidOperationException("Terrain would exceed Valheim's 8 m terrain limit. Adjust height or choose gentler ground.");
                    int index=z*(map.m_width+1)+x;
                    float next=levels[index]+smooth[index]+desired-old;
                    if(Math.Abs(next)>8f)throw new InvalidOperationException("Terrain adjustment exceeds supported height range.");
                    if(Math.Abs(desired-old)<.001f)continue;
                    tile.Nodes.Add(new Node{Index=index,OldLevel=levels[index],OldSmooth=smooth[index],OldModified=flags[index],NewLevel=next});
                }
            }
        }
        internal void Apply()
        {
            if(!Owned)throw new InvalidOperationException("Terrain ownership changed before placement.");
            applied=true;
            foreach(var tile in tiles)
            {
                var levels=(float[])Level.GetValue(tile.Compiler);var smooth=(float[])Smooth.GetValue(tile.Compiler);var flags=(bool[])Modified.GetValue(tile.Compiler);
                foreach(var n in tile.Nodes){levels[n.Index]=n.NewLevel;smooth[n.Index]=0;flags[n.Index]=true;}
                var paints=(Color[])Paint.GetValue(tile.Compiler);var paintFlags=(bool[])PaintModified.GetValue(tile.Compiler);
                foreach(var n in tile.Paints){paints[n.Index]=n.Next;paintFlags[n.Index]=true;}
                tile.Applied=true;Commit(tile);
            }
        }
        internal void AssertUndoSafe()
        {
            if(!applied)return;
            if(!Owned)throw new InvalidOperationException("Undo needs ownership of the original terrain; move back to the placement.");
            foreach(var tile in tiles)
            {
                if(!tile.Applied)continue;
                CheckLoad.Invoke(tile.Compiler,null);
                var levels=(float[])Level.GetValue(tile.Compiler);var smooth=(float[])Smooth.GetValue(tile.Compiler);var flags=(bool[])Modified.GetValue(tile.Compiler);
                var paints=(Color[])Paint.GetValue(tile.Compiler);var paintFlags=(bool[])PaintModified.GetValue(tile.Compiler);
                foreach(var n in tile.Paints)if(paints[n.Index]!=n.Next||!paintFlags[n.Index])throw new InvalidOperationException("Ground paint changed after placement; Undo stopped.");
                foreach(var n in tile.Nodes)if(levels[n.Index]!=n.NewLevel||smooth[n.Index]!=0||!flags[n.Index])throw new InvalidOperationException("Terrain was edited after placement. Undo stopped to preserve those edits.");
            }
        }
        internal void Undo()
        {
            if(!applied)return;
            AssertUndoSafe();
            foreach(var tile in tiles)
            {
                if(!tile.Applied)continue;
                var levels=(float[])Level.GetValue(tile.Compiler);var smooth=(float[])Smooth.GetValue(tile.Compiler);var flags=(bool[])Modified.GetValue(tile.Compiler);
                foreach(var n in tile.Nodes){levels[n.Index]=n.OldLevel;smooth[n.Index]=n.OldSmooth;flags[n.Index]=n.OldModified;}
                var paints=(Color[])Paint.GetValue(tile.Compiler);var paintFlags=(bool[])PaintModified.GetValue(tile.Compiler);
                foreach(var n in tile.Paints){paints[n.Index]=n.Old;paintFlags[n.Index]=n.Modified;}
                Commit(tile);
            }
            applied=false;
        }
        private void Commit(Tile tile)
        {
            Operations.SetValue(tile.Compiler,(int)Operations.GetValue(tile.Compiler)+1);
            LastPoint.SetValue(tile.Compiler,center);LastRadius.SetValue(tile.Compiler,radius);
            Save.Invoke(tile.Compiler,new object[]{false});tile.Map.Poke();
            if(ClutterSystem.instance)ClutterSystem.instance.ResetGrass(center,radius);
        }
    }
}
