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
}
