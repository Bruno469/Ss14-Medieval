using Content.Shared._Andromeda.Smelter.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Andromeda.Smelter;

[Serializable, NetSerializable]
public enum SmelterUiKey
{
    Key,
}

[Serializable, NetSerializable]
public enum SmelterVisualState : byte
{
    Base,
    Working
}

[Serializable, NetSerializable]
public enum SmelterVisuals : byte
{
    State
}

[Serializable, NetSerializable]
public sealed class SmelterUiStartSmeltingMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class SmelterUiClickRecipeMessage(string recipe)
    : BoundUserInterfaceMessage
{
    public readonly string Recipe = recipe;
}

[Serializable, NetSerializable]
public sealed class SmelterUiRecipesState(List<SmelterUiRecipeSelected> recipes, ProtoId<SmelterRecipePrototype>? selectedRecipe, int currectfuelamount, int maxfuelamount, NetEntity uid) : BoundUserInterfaceState
{
    public readonly ProtoId<SmelterRecipePrototype>? SelectedRecipe = selectedRecipe;
    public readonly List<SmelterUiRecipeSelected> Recipes = recipes;
    public readonly int CurrentFuelAmount = currectfuelamount;
    public readonly int MaxFuelAmount = maxfuelamount;
    public readonly NetEntity Entity = uid;
}

[Serializable, NetSerializable]
public readonly struct SmelterUiRecipeSelected(ProtoId<SmelterRecipePrototype> protoId, bool meltable)
    : IEquatable<SmelterUiRecipeSelected>
{
    public readonly ProtoId<SmelterRecipePrototype> ProtoId = protoId;
    public readonly bool Meltable = meltable;

    public int CompareTo(SmelterUiRecipeSelected other)
    {
        return Meltable.CompareTo(other.Meltable);
    }

    public override bool Equals(object? obj)
    {
        return obj is SmelterUiRecipeSelected other && Equals(other);
    }

    public bool Equals(SmelterUiRecipeSelected other)
    {
        return ProtoId.Id == other.ProtoId.Id;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ProtoId, Meltable);
    }

    public override string ToString()
    {
        return $"{ProtoId} ({Meltable})";
    }

    public static int CompareTo(SmelterUiRecipeSelected left, SmelterUiRecipeSelected right)
    {
        return right.CompareTo(left);
    }
}
