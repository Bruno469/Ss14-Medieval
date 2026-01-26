using Content.Shared._Andromeda.Smelter.Prototypes;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Andromeda.Smelter.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class SmelterComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<ProtoId<SmelterRecipePrototype>> Recipes = new();

    [DataField]
    public bool IsSmelting = false;


    [DataField, AutoNetworkedField]
    public int MaxFuelAmount = 100;

    [DataField, AutoNetworkedField]
    public int CurrentFuelAmount = 0;

    [DataField]
    public EntProtoId FuelType = "Coal";

    [DataField, AutoNetworkedField]
    public string? SelectedRecipe;

    [DataField]
    public SoundSpecifier MeltSound { get; set; } = new SoundPathSpecifier("/Audio/Ambience/Objects/fireplace.ogg");
    public EntityUid? AudioStream;

    [DataField]
    public EntProtoId? Vfx;
}
