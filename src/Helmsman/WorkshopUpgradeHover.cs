using Jotunn.Managers;
using UnityEngine;

namespace Helmsman;

// Cosmetic connection uses the vanilla prefab, with Helmsman's own workshop rules.
// A StationExtension would also alter vanilla station levels and placement rules.
public sealed class WorkshopUpgradeHover : MonoBehaviour, Hoverable
{
    private Piece piece=null!;
    private ZNetView view=null!;
    private GameObject? connection;
    private Shipyard? bench;
    private float nextLookup,expires;
    private void Awake(){piece=GetComponent<Piece>();view=GetComponent<ZNetView>();}
    public float GetHoverOffset()=>0;
    public string GetHoverName()=>piece?Localization.instance.Localize(piece.m_name):"Shipwright upgrade";
    public string GetHoverText()
    {
        if(!view||!view.IsValid()||!Player.m_localPlayer||!PrivateArea.CheckAccess(transform.position,0,false,true)){Clear();return GetHoverName();}
        if(Time.time>=nextLookup){bench=WorkshopRange.Bench(Player.m_localPlayer,transform.position);nextLookup=Time.time+.5f;}
        if(bench&&bench.Ready)
        {
            if(!connection)
            {
                var original=PrefabManager.Instance.GetPrefab("piece_workbench_ext3");
                var extension=original?original.GetComponent<StationExtension>():null;
                if(extension&&extension.m_connectionPrefab)
                    connection=Instantiate(extension.m_connectionPrefab);
            }
            if(connection)
            {
                var from=transform.TransformPoint(new Vector3(0,1.055f,0));
                var to=bench.transform.TransformPoint(new Vector3(0,1.055f,0));
                var direction=to-from;
                connection.transform.position=from;
                connection.transform.rotation=direction.sqrMagnitude>.0001f?Quaternion.LookRotation(direction):Quaternion.identity;
                connection.transform.localScale=new Vector3(1,1,direction.magnitude);
                expires=Time.time+1;
            }
        }
        else Clear();
        return GetHoverName();
    }
    private void Update(){if(connection&&Time.time>expires)Clear();}
    private void Clear(){if(connection)Destroy(connection);connection=null;}
    private void OnDisable(){Clear();bench=null;nextLookup=0;}
    private void OnDestroy()=>Clear();
}
