using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Andromeda.Food;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PerishableFoodComponent : Component
{
    [DataField, AutoNetworkedField]
    public float FreshDuration = 15; // Minutes

    [DataField, AutoNetworkedField]
    public float WiltedDuration = 20; // Minutes

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan WiltedAt = TimeSpan.Zero;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan RottenAt = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public PerishableState State = PerishableState.Fresh;
}

public enum PerishableState : byte
{
    Fresh,
    Wilted,
    Rotten
}
