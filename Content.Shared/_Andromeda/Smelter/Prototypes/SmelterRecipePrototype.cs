using Content.Shared.Materials;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Andromeda.Smelter.Prototypes;

[Prototype("SmelterRecipe")]
public sealed partial class SmelterRecipePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
    [DataField("smelterTime")]
    public float SmelterTime = 2.0f;

    [DataField]
    public SoundSpecifier? OverrideSmelterSound;

    [DataField("fuelNeeded", required: true)]
    public int FuelNeeded = 1;

    [DataField]
    public Dictionary<ProtoId<MaterialPrototype>, int> Materials = new();

    [DataField("result", required: true)]
    public EntProtoId Result;

    [DataField("resultCount")]
    public int ResultCount = 1;
}
