using Robust.Shared.IoC;
using Content.Shared._Andromeda.Food;
using Robust.Shared.Timing;

namespace Content.Server._Andromeda.Food;
public sealed class PerishableFoodSystem : SharedPerishableFood
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PerishableFoodComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<PerishableFoodComponent> ent, ref MapInitEvent args)
    {
        var now = _timing.CurTime;

        ent.Comp.WiltedAt = now + TimeSpan.FromMinutes(ent.Comp.FreshDuration);
        ent.Comp.RottenAt = ent.Comp.WiltedAt + TimeSpan.FromMinutes(ent.Comp.WiltedDuration);

        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;

        var query = EntityQueryEnumerator<PerishableFoodComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.State == PerishableState.Rotten)
                continue;

            if (comp.State == PerishableState.Fresh && now >= comp.WiltedAt)
                TransitionTo(uid, comp, PerishableState.Wilted);
            else if (comp.State == PerishableState.Wilted && now >= comp.RottenAt)
                TransitionTo(uid, comp, PerishableState.Rotten);
        }
    }
}
