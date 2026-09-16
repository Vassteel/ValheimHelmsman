using System;
using UnityEngine;

namespace Helmsman;

// Put the interaction on the bird, leaving the table's native crafting interface intact.
public sealed class BirdInteraction : MonoBehaviour,Interactable,Hoverable
{
    internal Func<string> Label=()=>"Bird";
    internal Func<string> Hint=()=>"";
    internal Func<Humanoid,bool>? Use;
    public string GetHoverName()=>Label();
    public float GetHoverOffset()=>.15f;
    public string GetHoverText()=>Hint();
    public bool Interact(Humanoid user,bool hold,bool alt)=>!hold&&user==Player.m_localPlayer&&(Use?.Invoke(user)??false);
    public bool UseItem(Humanoid user,ItemDrop.ItemData item)=>false;
}
