using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using Helmsman.Core;
using UnityEngine;
namespace Helmsman;
internal sealed class ShipyardAudio:IDisposable
{
    private static ConfigEntry<float> volume=null!,voice=null!;
    private static readonly Dictionary<(YardSound,int),AudioClip> clips=new();
    private readonly AudioSource source,slide;private float nextVoice;
    internal static void Configure(ConfigFile c)
    {
        volume=c.Bind("Audio","ShipyardVolume",.45f,new ConfigDescription("Local spatial construction, launch and reset audio.",new AcceptableValueRange<float>(0,1)));
        voice=c.Bind("Audio","PuffinVolume",.4f,new ConfigDescription("Local puffin greetings and quiet calls.",new AcceptableValueRange<float>(0,1)));
    }
    internal ShipyardAudio(Transform parent)
    {
        var go=new GameObject("Shipyard spatial audio");go.transform.SetParent(parent,false);source=go.AddComponent<AudioSource>();
        source.playOnAwake=false;source.spatialBlend=1;source.dopplerLevel=0;source.rolloffMode=AudioRolloffMode.Linear;source.minDistance=2;source.maxDistance=24;
        var native=ZNetScene.instance?ZNetScene.instance.GetPrefab("sfx_seagull_idle"):null;
        var mixer=native?native.GetComponentInChildren<AudioSource>(true):null;if(mixer)source.outputAudioMixerGroup=mixer.outputAudioMixerGroup;
        slide=go.AddComponent<AudioSource>();slide.playOnAwake=false;slide.loop=true;slide.spatialBlend=1;slide.dopplerLevel=0;slide.rolloffMode=AudioRolloffMode.Linear;slide.minDistance=3;slide.maxDistance=32;slide.outputAudioMixerGroup=source.outputAudioMixerGroup;slide.volume=0;
    }
    internal void Play(YardSound kind,float gain=1)
    {
        if(!source||!Player.m_localPlayer||(source.transform.position-Player.m_localPlayer.transform.position).sqrMagnitude>576)return;
        bool call=kind==YardSound.PuffinMurmur||kind==YardSound.PuffinGreeting;
        float level=(call?voice:volume)?.Value??0;if(level<=0||call&&Time.time<nextVoice)return;
        if(call)nextVoice=Time.time+(kind==YardSound.PuffinGreeting?12:90);
        var clip=Clip(kind,kind==YardSound.Creak?UnityEngine.Random.Range(0,4):0);
        source.pitch=UnityEngine.Random.Range(.97f,1.03f);source.PlayOneShot(clip,level*Mathf.Clamp01(gain));
    }
    private static AudioClip Clip(YardSound kind,int variant=0)
    {
        var key=(kind,variant);
        if(!clips.TryGetValue(key,out var clip)||!clip){var data=ShipyardAudioSynth.Create(kind,variant);clip=AudioClip.Create("Helmsman "+kind+variant,data.Length,1,ShipyardAudioSynth.Rate,false);clip.SetData(data,0);clips[key]=clip;}
        return clip;
    }
    internal void Sliding(float intensity)
    {
        bool near=Player.m_localPlayer&&(source.transform.position-Player.m_localPlayer.transform.position).sqrMagnitude<1024;
        float target=near?Mathf.Clamp01(intensity)*(volume?.Value??0):0;
        slide.volume=Mathf.MoveTowards(slide.volume,target,Time.deltaTime*1.8f);
        if(target>0&&!slide.isPlaying){slide.clip=Clip(YardSound.WoodSlide);slide.Play();}
        if(target==0&&slide.volume<=.001f&&slide.isPlaying)slide.Stop();
    }
    public void Dispose(){if(source)UnityEngine.Object.Destroy(source.gameObject);}
    internal static void Release(){foreach(var clip in clips.Values)if(clip)UnityEngine.Object.Destroy(clip);clips.Clear();}
}
