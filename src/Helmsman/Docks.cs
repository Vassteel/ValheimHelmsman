using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Helmsman;

[Serializable]
public sealed class Berth
{
    public int version = 1;
    public string name = "Unnamed dock";
    public Vector3 position;
    public float heading;
    public bool reverseDeparture;
    public float reverseDistance = 30;
    public bool configured;
    public string shipType = "Karve";
    public Vector3 Forward => Quaternion.Euler(0,heading,0)*Vector3.forward;
    public Vector3 Approach => position - Forward*35;
    public Vector3 Exit => position + Forward*(reverseDeparture ? -reverseDistance : 35);
    public Berth Copy() => JsonUtility.FromJson<Berth>(JsonUtility.ToJson(this));
    public bool ValidData => version == 1 && !string.IsNullOrWhiteSpace(shipType) && !string.IsNullOrWhiteSpace(name) && name.Length <= 48 &&
        Finite(position.x) && Finite(position.y) && Finite(position.z) && Finite(heading) &&
        Finite(reverseDistance) && reverseDistance >= 16 && reverseDistance <= 80;
    private static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
}

public sealed class DockRecord
{
    public ZDOID Id;
    public Berth Berth = null!;
    public Vector3 MarkerPosition;
}

public sealed class DockDirectory : MonoBehaviour
{
    internal static readonly int DataKey = "helmsman_berth_v1".GetStableHashCode();
    internal List<DockRecord> Records { get; private set; } = new List<DockRecord>();
    private long world;

    private IEnumerator Start()
    {
        while (true)
        {
            if (!ZNet.instance || ZDOMan.instance == null || !Player.m_localPlayer)
            { Records.Clear(); world = 0; yield return new WaitForSeconds(1); continue; }
            var currentWorld = ZNet.instance.GetWorldUID();
            if (currentWorld != world) { Records.Clear(); world = currentWorld; }
            var scanWorld = world;
            var zdos = new List<ZDO>(); int cursor = 0;
            while (ZDOMan.instance != null && ZNet.instance && ZNet.instance.GetWorldUID() == world &&
                !ZDOMan.instance.GetAllZDOsWithPrefabIterative(Plugin.DockPrefab, zdos, ref cursor)) yield return null;
            if (!ZNet.instance || ZNet.instance.GetWorldUID() != scanWorld || !Player.m_localPlayer)
            { Records.Clear(); continue; }
            var next = new List<DockRecord>();
            var seen = new HashSet<ZDOID>();
            foreach (var zdo in zdos)
            {
                if (!seen.Add(zdo.m_uid)) continue;
                var berth = Read(zdo);
                if (berth != null && berth.configured) next.Add(new DockRecord { Id=zdo.m_uid, Berth=berth, MarkerPosition=zdo.GetPosition() });
            }
            Records = next;
            yield return new WaitForSeconds(3);
        }
    }

    internal static Berth? Read(ZDO? zdo)
    {
        if (zdo == null || !zdo.IsValid()) return null;
        var json = zdo.GetString(DataKey, "");
        if (json.Length == 0 || json.Length > 4096) return null;
        try { var b = JsonUtility.FromJson<Berth>(json); return b != null && b.ValidData ? b : null; }
        catch (ArgumentException) { return null; }
    }

    internal static DockRecord? Resolve(ZDOID id)
    {
        var zdo = ZDOMan.instance?.GetZDO(id);
        if (zdo == null || zdo.GetPrefab() != Plugin.DockPrefab.GetStableHashCode()) return null;
        var b = Read(zdo);
        return b != null && b.configured ? new DockRecord {Id=id,Berth=b,MarkerPosition=zdo.GetPosition()} : null;
    }
}

public sealed class DockMarker : MonoBehaviour, Interactable, Hoverable
{
    private ZNetView view = null!;
    private GullGuide? gull;
    internal ZDOID Id => view.GetZDO().m_uid;
    internal Berth Settings => DockDirectory.Read(view ? view.GetZDO() : null) ?? new Berth
    { position=transform.position+transform.forward*12, heading=transform.eulerAngles.y };
    internal bool Ready => view && view.IsValid();

    private void Awake() { view = GetComponent<ZNetView>(); }
    private IEnumerator Start()
    {
        while (!Ready || !Player.m_localPlayer) yield return new WaitForSeconds(.5f);
        if(!GetComponent<SummonBeacon>())gameObject.AddComponent<SummonBeacon>();
        while(Ready)
        {
            if(!gull)gull=GullGuide.Traveller(Id);
            if(!gull)gull=GullGuide.Create(transform.position+Vector3.up*2,this,null);
            yield return new WaitForSeconds(1);
        }
    }
    internal GullGuide CallGuide(Ship ship)
    {
        if(!gull)gull=GullGuide.Create(transform.position+Vector3.up*2,this,null);
        gull.ReserveDock(Id);gull.Visit(ship);return gull;
    }
    internal GullGuide FetchGuide(Vector3 target)
    {
        if(!gull)gull=GullGuide.Create(transform.position+Vector3.up*2,this,null);
        gull.Fetch(target);return gull;
    }
    internal GullGuide BoardGuide(Voyage voyage)
    {
        if(!gull)gull=GullGuide.Create(transform.position+Vector3.up*2,this,null);
        gull.Board(voyage);return gull;
    }
    private void OnDestroy() { if(gull && !gull.IsTravelling)Destroy(gull.gameObject); }
    public string GetHoverName() => "Dock Ward — " + Settings.name;
    public float GetHoverOffset() => 0;
    public string GetHoverText() => Localization.instance.Localize(GetHoverName()+"\n[<color=yellow><b>$KEY_Use</b></color>] Configure berth");
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || !Ready) return false;
        if (!Plugin.Solo) { Plugin.Message("Dock configuration currently supports solo worlds."); return false; }
        Plugin.Instance.UI.OpenDock(this); return true;
    }
    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

    internal bool Save(Berth berth)
    {
        if (!Plugin.Solo || !Ready || !berth.ValidData) return false;
        if (Vector3.Distance(Player.m_localPlayer.transform.position, transform.position) > 10) return false;
        view.ClaimOwnership();
        if (!view.IsOwner()) return false;
        view.GetZDO().Set(DockDirectory.DataKey, JsonUtility.ToJson(berth));
        return true;
    }
}
