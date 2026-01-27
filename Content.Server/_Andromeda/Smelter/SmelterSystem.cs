using System.Numerics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Server.Stack;
using Content.Server.Materials;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Content.Shared.Interaction;
using Content.Shared.Materials;
using Content.Shared._Andromeda.Smelter;
using Content.Shared._Andromeda.Smelter.Components;
using Content.Shared._Andromeda.Smelter.Prototypes;
using Content.Shared.UserInterface;
using Content.Shared._Andromeda.Fuel;
using Content.Shared.Tag;
using Content.Shared.DoAfter;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Andromeda.Smelter;

public sealed partial class SmelterSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly MaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;

    private static readonly ProtoId<TagPrototype> FuelTag = "Fuel";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SmelterComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SmelterComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SmelterComponent, BeforeActivatableUIOpenEvent>(OnBeforeUIOpen);
        SubscribeLocalEvent<SmelterComponent, SmelterUiClickRecipeMessage>(OnSetRecipe);
        SubscribeLocalEvent<SmelterComponent, SmelterUiStartSmeltingMessage>(OnStartSmelting);
        SubscribeLocalEvent<SmelterComponent, MaterialAmountChangedEvent>(OnMaterialChanged);
        SubscribeLocalEvent<SmelterComponent, SmelterDoAfterEvent>(OnSmelterDoAfter);
    }

    private void OnMapInit(Entity<SmelterComponent> ent, ref MapInitEvent args)
    {
        foreach (var recipe in _proto.EnumeratePrototypes<SmelterRecipePrototype>())
        {
            if (ent.Comp.Recipes.Contains(recipe.ID))
                continue;

            ent.Comp.Recipes.Add(recipe.ID);
        }
    }

    private void OnInteractUsing(EntityUid uid, SmelterComponent comp, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (TryFillFuelWith(args.User, args.Used, comp))
        {
            args.Handled = true;
            UpdateUIRecipes((uid, comp));

            _popup.PopupEntity(Loc.GetString("Smelter-insert-fuel"), args.User);
        }
    }

    private void OnBeforeUIOpen(Entity<SmelterComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateUIRecipes((ent, ent.Comp));
    }

    private void OnSetRecipe(Entity<SmelterComponent> ent, ref SmelterUiClickRecipeMessage args)
    {
        if(!_proto.HasIndex<SmelterRecipePrototype>(args.Recipe))
            return;

        ent.Comp.SelectedRecipe = args.Recipe;
        Dirty(ent);
        UpdateUIRecipes((ent, ent.Comp));
    }

    private void OnStartSmelting(Entity<SmelterComponent> ent, ref SmelterUiStartSmeltingMessage args)
    {
        var recipeId = ent.Comp.SelectedRecipe;

        if (string.IsNullOrEmpty(recipeId))
            return;

        if (!TryStartSmelting(ent, recipeId))
            return;
    }

    private void UpdateUIRecipes(Entity<SmelterComponent> entity)
    {
        if (!TryComp<MaterialStorageComponent>(entity, out var storage))
            return;

        var netEnt = GetNetEntity(entity.Owner);
        var recipes = new List<SmelterUiRecipeSelected>();

        foreach (var recipeId in entity.Comp.Recipes)
        {
            if (!TryGetRecipe(recipeId, out var recipe))
                continue;

            var canMelt = HasMaterials(entity, recipe);

            recipes.Add(new SmelterUiRecipeSelected(recipeId, canMelt));
        }

        _userInterface.SetUiState(entity.Owner, SmelterUiKey.Key, new SmelterUiRecipesState(recipes, entity.Comp.SelectedRecipe, entity.Comp.CurrentFuelAmount, entity.Comp.MaxFuelAmount, netEnt));
    }

    private bool TryStartSmelting(Entity<SmelterComponent> ent, string recipeId)
    {
        if (ent.Comp.IsSmelting)
            return false;

        if (!HasFuel(ent, recipeId))
            return false;

        if (!TryGetRecipe(recipeId, out var recipe))
            return false;

        if (!HasMaterials(ent, recipe))
            return false;

        ent.Comp.IsSmelting = true;
        Dirty(ent);

        ent.Comp.CurrentFuelAmount -= recipe.FuelNeeded;

        if (TryComp<MaterialStorageComponent>(ent, out var storage))
        {
            foreach (var (material, requiredAmount) in recipe.Materials)
            {
                if (!_materialStorage.TryChangeMaterialAmount(ent, material, -requiredAmount))
                    return false;
            }
        }

        var args = new DoAfterArgs(
            EntityManager,
            ent.Owner,
            TimeSpan.FromSeconds(recipe.SmelterTime),
            new SmelterDoAfterEvent(recipeId),
            ent.Owner
        )
        {
            BreakOnDamage = false,
            CancelDuplicate = false
        };

        ent.Comp.AudioStream = _audio.PlayPvs(
            ent.Comp.MeltSound,
            ent.Owner
        )?.Entity;

        if (_pointLight.TryGetLight(ent.Owner, out var light))
            _pointLight.SetEnabled(ent.Owner, true, light);

        _appearanceSystem.SetData(ent.Owner, SmelterVisuals.State, true);
        _doAfter.TryStartDoAfter(args);

        return true;
    }

    private void OnSmelterDoAfter(Entity<SmelterComponent> ent, ref SmelterDoAfterEvent args)
    {
        if (_pointLight.TryGetLight(ent.Owner, out var light))
            _pointLight.SetEnabled(ent.Owner, false, light);

        ent.Comp.IsSmelting = false;
        ent.Comp.AudioStream = _audio.Stop(ent.Comp.AudioStream);
        _appearanceSystem.SetData(ent.Owner, SmelterVisuals.State, false);

        if (args.Cancelled)
            return;

        if (!TryGetRecipe(args.RecipeId, out var recipe))
            return;

        var coords = _transform.GetMapCoordinates(ent.Owner);
        Spawn(recipe.Result, coords);

        UpdateUIRecipes(ent);
        Dirty(ent);
    }

    private bool TryFillFuelWith(EntityUid user, EntityUid toInsert, SmelterComponent? smelter = null)
    {
        if (smelter == null)
            return false;

        if (!TryComp<FuelComponent>(toInsert, out var fuel))
            return false;

        if (!_tagSystem.HasTag(toInsert, FuelTag))
            return false;

        if (smelter.CurrentFuelAmount == smelter.MaxFuelAmount)
        {
            _popup.PopupEntity(Loc.GetString("Smelter-fuel-full"), user);
            return false;
        }

        smelter.CurrentFuelAmount =
            Math.Min(smelter.MaxFuelAmount,
                    smelter.CurrentFuelAmount + fuel.FuelAmount);

        QueueDel(toInsert);
        return true;
    }
    private bool HasFuel(Entity<SmelterComponent> ent, string recipeId)
    {
        if (!TryGetRecipe(recipeId, out var recipe))
            return false;

        if (ent.Comp.CurrentFuelAmount < recipe.FuelNeeded)
            return false;

        return true;
    }
    private bool HasMaterials(Entity<SmelterComponent> ent, SmelterRecipePrototype recipe)
    {
        if (!TryComp<MaterialStorageComponent>(ent, out var storage))
            return false;

        foreach (var (material, requiredAmount) in recipe.Materials)
        {
            var stored = _materialStorage.GetMaterialAmount(ent, material, storage);
            if (stored < requiredAmount)
                return false;
        }

        return true;
    }
    private bool TryGetRecipe(string id, [NotNullWhen(true)] out SmelterRecipePrototype? recipe)
    {
        return _proto.TryIndex(id, out recipe);
    }
    private void OnMaterialChanged(Entity<SmelterComponent> ent, ref MaterialAmountChangedEvent args)
    {
        if (!_userInterface.IsUiOpen(ent.Owner, SmelterUiKey.Key))
            return;

        UpdateUIRecipes((ent, ent.Comp));
    }
}
