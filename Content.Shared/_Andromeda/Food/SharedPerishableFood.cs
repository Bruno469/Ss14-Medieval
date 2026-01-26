using Content.Shared.Chemistry.EntitySystems;
using Content.Shared._Andromeda.Food;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.FixedPoint;
using Content.Shared.Chemistry.Components;
using Content.Shared.Examine;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.GameObjects;

namespace Content.Shared._Andromeda.Food;

public abstract class SharedPerishableFood : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PerishableFoodComponent, ExaminedEvent>(OnFoodExamine);
    }

    private void OnFoodExamine(Entity<PerishableFoodComponent> entity, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(PerishableFoodComponent)))
        {
            if (!args.IsInDetailsRange)
                return;

            string color;
            string text;

            switch (entity.Comp.State)
            {
                case PerishableState.Fresh:
                    color = "green";
                    text = Loc.GetString("perishable-food-fresh");
                    break;

                case PerishableState.Wilted:
                    color = "yellow";
                    text = Loc.GetString("perishable-food-wilted");
                    break;

                case PerishableState.Rotten:
                    color = "purple";
                    text = Loc.GetString("perishable-food-rotten");
                    break;

                default:
                    return;
            }

            args.PushMarkup(
                Loc.GetString(
                    "perishable-food-status",
                    ("color", color),
                    ("status", text)
                )
            );
        }
    }

    protected void TransitionTo(EntityUid uid, PerishableFoodComponent comp, PerishableState newState)
    {
        comp.State = newState;
        Dirty(uid, comp);

        if (!_solutions.TryGetSolution(uid, "food", out var solnEntity, out var solution))
            return;

        if (newState == PerishableState.Wilted)
        {
            _solutions.RemoveEachReagent(solnEntity.Value, FixedPoint2.New(1));
        }
        else if (newState == PerishableState.Rotten)
        {
            _solutions.RemoveReagent(solnEntity.Value, "Vitamin", FixedPoint2.MaxValue);
            _solutions.RemoveReagent(solnEntity.Value, "Nutriment", FixedPoint2.MaxValue);

            _solutions.TryAddReagent(solnEntity.Value, "Toxin", FixedPoint2.New(2), out _);
        }
    }
}
