namespace Content.Shared._Andromeda.Fuel;

[RegisterComponent]
public sealed partial class FuelComponent : Component
{
    [DataField("FuelAmount", required: true)]
    public int FuelAmount;
}
