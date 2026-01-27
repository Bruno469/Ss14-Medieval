using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Andromeda.Smelter;

[Serializable, NetSerializable]
public sealed partial class SmelterDoAfterEvent : SimpleDoAfterEvent
{
    public readonly string RecipeId;

    public SmelterDoAfterEvent(string recipeId)
    {
        RecipeId = recipeId;
    }
}
