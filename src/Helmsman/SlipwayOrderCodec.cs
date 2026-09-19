using System;
using System.IO;
using Helmsman.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Helmsman;

// Encode the ship payload explicitly: the live Unity serializer omitted this
// nested field while retaining the slipway wrapper, producing a busy empty job.
internal static class SlipwayOrderCodec
{
    private static JObject Parse(string raw)
    {
        if(string.IsNullOrEmpty(raw)||raw.Length>8192)throw new InvalidDataException("Invalid slipway save size");
        return JObject.Parse(raw);
    }
    internal static SlipwayOrder Read(string raw)
    {
        var json=Parse(raw);
        if(json["order"] is not JObject ship)throw new InvalidDataException("Missing ship-order payload");
        var state=JsonUtility.FromJson<SlipwayOrder>(raw);
        state.order=JsonUtility.FromJson<ConstructionOrder>(ship.ToString(Formatting.None));
        if(!state.Valid||ShipConstruction.Find(state.order.blueprint)?.UsesSlipway!=true)
            throw new InvalidDataException("Invalid ship-order payload");
        return state;
    }
    internal static string Write(SlipwayOrder state)
    {
        var json=JObject.Parse(JsonUtility.ToJson(state));
        json["order"]=JObject.Parse(JsonUtility.ToJson(state.order));
        string raw=json.ToString(Formatting.None);
        var check=Read(raw); // Verify the actual serializer before reserving/spending.
        if(check.order.blueprint!=state.order.blueprint||check.order.recipe!=state.order.recipe||check.bench!=state.bench||check.supplied!=state.supplied)
            throw new InvalidDataException("Ship-order serialization lost data");
        return raw;
    }
    internal static string Bench(string raw)=>(string?)Parse(raw)["bench"]??"";
    internal static bool EmptyBrokenBlueprint(string raw,out string bench)
    {
        bench="";
        try
        {
            var json=Parse(raw);
            if(json["order"]!=null||(bool?)json["awaitingMaterials"]!=true||(string?)json["supplied"]!="")return false;
            foreach(string field in new[]{"launchStarted","launchPaused","launchPauseTicks","crewCalledAt","constructionStarted","supportPaused","supportPauseTicks"})
                if((long?)json[field]!=0)return false;
            bench=(string?)json["bench"]??"";return bench.Length>0;
        }
        catch{return false;}
    }
}
