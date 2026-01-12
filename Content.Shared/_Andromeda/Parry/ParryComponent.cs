using Robust.Shared.GameStates;

namespace Content.Shared._Andromeda.Parry;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class ParryComponent : Component
{
    [DataField]
    public bool Parry = false;
    [DataField]
    public int StaminaCost = 15;

    [DataField]
    public float ParryWindow = 0.5f; // 150ms = 0.15f -- 2.0f = 2000ms
}
