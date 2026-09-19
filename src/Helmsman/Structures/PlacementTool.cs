#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jotunn.Managers;
using UnityEngine;

namespace Helmsman.Structures
{
    public sealed partial class PlacementTool:MonoBehaviour
    {
        private string[] files=Array.Empty<string>();
        private string error="",label="",dataWarning="";
        private bool browser,confirm,busy,inputBlocked,preview,locked,hit;
        private Blueprint blueprint;
        private Dictionary<string,GameObject> prefabs;
        private Ghost ghost;
        private Vector3 anchor;
        private float yaw,height,terrainHeight,blend=12f;
        private bool shape=true;
        private readonly List<ZDOID> lastPieces=new List<ZDOID>();
        private TerrainPlan lastTerrain;
        private VegetationPlan lastVegetation;
        private long worldUid;
        private ZDOID sourceTotem;
        private Vector3 Position=>anchor+Vector3.up*height;
        private Quaternion Rotation=>Quaternion.Euler(0,yaw,0);
        public void Open(ZDOID totem)
        {
            if(busy)return;
            sourceTotem=totem;
            ResetPreview();browser=true;error="";filePage=0;menuState="";SetInput(true);
            try{files=ImportPaths.Find(StructureImports.Instance.ResolvedImportFolder);}
            catch(Exception e){error=e.Message;}
        }
        private void SetInput(bool value){if(inputBlocked==value)return;inputBlocked=value;GUIManager.BlockInput(value);}
        private void Select(string path)
        {
            try
            {
                if(new FileInfo(path).Length>12_000_000)throw new InvalidOperationException("File is larger than 12 MB.");
                var build=Blueprint.Parse(File.ReadAllText(path));
                build.ValidatePlacementSize();
                var lookup=new Dictionary<string,GameObject>();var missing=new List<string>();var rejected=new List<string>();
                foreach(var name in build.Pieces.Select(p=>p.Prefab).Distinct())
                {
                    var p=ZNetScene.instance.GetPrefab(name);
                    if(!p){missing.Add(name);continue;}
                    var components=p.GetComponentsInChildren<MonoBehaviour>(true);
                    bool spawner=components.Any(c=>c&&(c.GetType().Name=="SpawnArea"||c.GetType().Name=="CreatureSpawner"||c.GetType().Name=="SpawnSystem"));
                    string reason=PrefabPolicy.Rejection(p.GetComponent<ZNetView>()!=null,p.GetComponentInChildren<Character>(true)!=null,
                        spawner,p.GetComponentInChildren<TerrainOp>(true)!=null||p.GetComponentInChildren<TerrainModifier>(true)!=null,p.GetComponent<ItemDrop>()!=null);
                    if(reason!=null)rejected.Add(name+" ("+reason+")");
                    else lookup.Add(name,p);
                }
                if(missing.Count>0||rejected.Count>0)
                {
                    var messages=new List<string>();
                    if(missing.Count>0)messages.Add("Not found in the loaded game: "+string.Join(", ",missing.Take(15))+". These may require another mod or a different game version.");
                    if(rejected.Count>0)messages.Add("Unsupported objects: "+string.Join(", ",rejected.Take(15))+".");
                    throw new InvalidOperationException(string.Join("\n",messages)+" Nothing was placed.");
                }
                ghost?.Dispose();ghost=null;
                int unsupported=build.Pieces.Count(p=>p.HasExtendedData||(p.Data.Length>0&&lookup[p.Prefab].GetComponent<TextReceiver>()==null));
                dataWarning=unsupported>0?$"Geometry import: {unsupported} objects contain additional data (such as inventory or equipment) that is not restored.":"";
                Ghost.FitFoundation(build,lookup);
                ghost=new Ghost(build,lookup);blueprint=build;prefabs=lookup;
                label=Path.GetFileName(path);yaw=0;height=0;terrainHeight=0;locked=false;hit=false;preview=true;browser=false;error="";SetInput(false);
            }
            catch(Exception e){error=Path.GetFileName(path)+": "+e.Message;StructureImports.Report(error);}
        }
        private void Update()
        {
            if(!Player.m_localPlayer || !ZNetScene.instance)
            {
                if(!busy){ResetPreview();browser=false;confirm=false;SetInput(false);lastPieces.Clear();lastTerrain=null;lastVegetation=null;}
                return;
            }
            if(!StructureImports.Allowed&&!busy){ResetPreview();browser=false;confirm=false;SetInput(false);return;}
            if(HandleBackInput())return;
            if(!preview||busy)return;
            if(!confirm)
            {
                if(ZInput.GetKeyDown(KeyCode.F))locked=!locked;
                if(!locked)
                {
                    var camera=Camera.main;
                    if(camera && Physics.Raycast(camera.transform.position,camera.transform.forward,out var rayHit,100f,LayerMask.GetMask("terrain")))
                    {anchor=rayHit.point;hit=true;}else hit=false;
                }
                UpdatePositioningKeys();
            }
            ghost.Draw(Position,Rotation,hit);
            if(shape&&hit)ghost.DrawTerrain(blueprint,Position,Rotation,blend,terrainHeight);
        }
        private IEnumerator Place()
        {
            if(busy||!preview||!hit)yield break;
            error="";TerrainPlan terrain=null;VegetationPlan vegetation=null;
            var placementTotem=sourceTotem;
            try
            {
                if(!StructureImports.Allowed)throw new InvalidOperationException("Admin access required.");
                if(Vector3.Distance(Player.m_localPlayer.transform.position,Position)>110)throw new InvalidOperationException("Move closer to the placement.");
                foreach(var p in blueprint.Pieces)
                {
                    var at=Position+Rotation*new Vector3(p.X,p.Y,p.Z);
                    if(!Heightmap.FindHeightmap(at))throw new InvalidOperationException("The whole build must be in loaded terrain.");
                    if(Location.IsInsideNoBuildLocation(at))throw new InvalidOperationException("Build overlaps a no-build area.");
                    if(!PrivateArea.CheckAccess(at,0,false,true))throw new InvalidOperationException("Build overlaps a protected ward.");
                }
                var source=ZNetScene.instance.FindInstance(sourceTotem);
                var yard=source?source.GetComponent<Shipyard>():null;
                if(!yard)throw new InvalidOperationException("The puffin bench is unavailable.");
                string problem=yard.GetComponent<StructureConstruction>().Validate(blueprint,Position,Rotation,Player.m_localPlayer);
                if(problem.Length>0)throw new InvalidOperationException(problem);
                terrain=new TerrainPlan(Position,blueprint.Radius+blend);terrain.RequestOwnership();
                vegetation=new VegetationPlan(blueprint,Position,Rotation);vegetation.RequestOwnership();
            }
            catch(Exception e){error=e.Message;yield break;}
            busy=true;SetInput(true);
            float deadline=Time.realtimeSinceStartup+5f;
            while((!terrain.Owned||!vegetation.Owned)&&Time.realtimeSinceStartup<deadline)yield return null;
            bool prepared=false;
            try{if(terrain!=null)terrain.Prepare(blueprint,Rotation,blend,terrainHeight,shape);if(!vegetation.Owned)throw new InvalidOperationException("Vegetation ownership was not granted.");prepared=true;}
            catch(Exception e){error=e.Message;}
            if(!prepared){busy=false;yield break;}
            // Replace the previous undo only once preflight succeeds.
            lastPieces.Clear();lastTerrain=terrain;lastVegetation=vegetation;worldUid=ZNet.instance.GetWorldUID();
            bool failed=false;
            try{terrain?.Apply();vegetation?.Apply();}catch(Exception e){error=e.Message;failed=true;}
            if(!failed)
            {
                try
                {
                    var benchObject=ZNetScene.instance.FindInstance(placementTotem);
                    var bench=benchObject?benchObject.GetComponent<Shipyard>():null;
                    if(!bench)throw new InvalidOperationException("The puffin workbench is no longer loaded.");
                    string result=bench.GetComponent<StructureConstruction>().Queue(blueprint,label,Position,Rotation,Player.m_localPlayer);
                    if(result.Length>0)throw new InvalidOperationException(result);
                }
                catch(Exception e){error=e.Message;failed=true;}
            }
            if(failed)
            {
                try{UndoNow();error+=" Placement rolled back.";}catch(Exception e){error+=" Rollback incomplete: "+e.Message+". Keep this session open and use Undo.";}
                StructureImports.Report(error);
            }
            else
            {
                // The workbench remains the persistent job owner and supply inventory.
                lastPieces.Clear();lastTerrain=null;lastVegetation=null;
                StructureImports.Report("Construction queued at the puffin bench. Supply materials through Quartermaster or the bench inventory. Unsupported pieces will wait for foundations.");
                ResetPreview();confirm=false;
            }
            busy=false;SetInput(confirm||browser);
        }
        private IEnumerator UndoLast()
        {
            if(busy)yield break;
            busy=true;error="";
            try{lastTerrain?.RequestOwnership();foreach(var id in lastPieces){var go=ZNetScene.instance.FindInstance(id);if(go)go.GetComponent<ZNetView>().ClaimOwnership();}}
            catch(Exception e){error=e.Message;busy=false;yield break;}
            yield return null;
            try{UndoNow();StructureImports.Report("Removed the last imported structure and restored its terrain.");}
            catch(Exception e){error=e.Message;}
            busy=false;
        }
        private void ResetPreview(){ghost?.Dispose();ghost=null;preview=false;locked=false;blueprint=null;prefabs=null;ResetPositioningInput();}
        private void OnDestroy(){DestroyMenu();ResetPreview();SetInput(false);}
    }
}
