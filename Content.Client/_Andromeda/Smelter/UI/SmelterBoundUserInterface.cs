using Content.Shared._Andromeda.Smelter;
using Content.Shared._Andromeda.Smelter.Prototypes;
using Robust.Client.UserInterface;

namespace Content.Client._Andromeda.Smelter.UI;

public sealed class SmelterBoundUserInterface : BoundUserInterface
{
    private SmelterMenu? _menu;

    public SmelterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SmelterMenu>();
        _menu.OnRecipeSelected  += RecipeSelected;
        _menu.OnSmelter += _ => SendMessage(new SmelterUiStartSmeltingMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case SmelterUiRecipesState recipesState:
                _menu?.UpdateState(recipesState);
                break;
        }
    }
    private void RecipeSelected(string protoId)
    {
        SendMessage(new SmelterUiClickRecipeMessage(protoId));
    }
}
