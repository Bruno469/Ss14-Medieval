using System.Diagnostics.CodeAnalysis;
using Content.Shared.Damage.Components;
using Content.Shared._Andromeda.Parry;
using Content.Shared.Hands.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

public sealed class ParrySystem : EntitySystem
{
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    public override void Initialize()
    {
        SubscribeNetworkEvent<ParryActionEvent>(OnParryRequested);
        SubscribeLocalEvent<ParryUserComponent, BeforeDamageChangedEvent>(OnUserDamage);
    }

    private void OnParryRequested(ParryActionEvent ev, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;
        if (user == null)
            return;

        TryParry(user.Value);
    }

    private void OnUserDamage(Entity<ParryUserComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Damage.GetTotal() <= 0)
            return;

        if (!TryGetActiveParryWeapon(ent.Owner, out var parry))
            return;

        args.Cancelled = true;

        _popup.PopupEntity(Loc.GetString("sucess-parry"), ent.Owner, ent.Owner, PopupType.Medium);
        _stamina.TakeStaminaDamage(ent.Owner, -parry.StaminaCost, visual: false, sound: null);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/sword_crash.ogg"), ent.Owner);

        if (args.Origin is { } origin &&
            TryComp<StaminaComponent>(origin, out _) && IsMeleeAttack(origin))
        {
            _stamina.TakeStaminaDamage(origin, parry.StaminaCost);
        }

        RemComp<ParryUserComponent>(ent.Owner);
    }

    private void TryParry(EntityUid user)
    {
        if (!TryGetActiveParryWeapon(user, out var parry))
            return;

        ActiveParry(user, parry);
        _stamina.TakeStaminaDamage(user, parry.StaminaCost);
    }

    private void ActiveParry(EntityUid user, ParryComponent source) {
        if (TryComp<ParryUserComponent>(user, out var parry))
        {
            parry.ParryTimer = source.ParryWindow; return;
        }

        parry = AddComp<ParryUserComponent>(user);
        parry.ParryTimer = source.ParryWindow;
    }

    private bool IsMeleeAttack(EntityUid attacker)
    {
        return TryComp<HandsComponent>(attacker, out var hands) &&
            hands.ActiveHandEntity is { } held &&
            HasComp<MeleeWeaponComponent>(held);
    }

    private bool TryGetActiveParryWeapon(EntityUid user, [NotNullWhen(true)] out ParryComponent? parry)
    {
        parry = null;
        if (!TryComp<HandsComponent>(user, out var hands) || hands.ActiveHandEntity is not { } held)
            return false;

        return TryComp(held, out parry);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ParryUserComponent>();

        while (query.MoveNext(out var uid, out var parry))
        {
            parry.ParryTimer -= frameTime;

            if (parry.ParryTimer <= 0f)
            {
                RemComp<ParryUserComponent>(uid);
            }
        }
    }
}
