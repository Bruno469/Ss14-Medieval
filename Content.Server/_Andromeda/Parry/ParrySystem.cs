using Content.Shared.Blocking;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared._Andromeda.Input;
using Content.Shared._Andromeda.Parry;
using Content.Shared.Damage.Components;

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Strip.Components;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

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

        args.Cancelled = true;

        if (!TryComp<HandsComponent>(ent.Owner, out var hands) ||
            hands.ActiveHandEntity is not { } held ||
            !TryComp<ParryComponent>(held, out var parry))
            return;

        _popup.PopupEntity(Loc.GetString("sucess-parry"), ent.Owner, ent.Owner, PopupType.Large);
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
        if (!TryComp<HandsComponent>(user, out var hands))
            return;

        if (hands.ActiveHandEntity is not { } held)
            return;

        if (!TryComp<ParryComponent>(held, out var parry))
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
        Logger.Info("Parry Ativo");
    }

    private bool IsMeleeAttack(EntityUid attacker)
    {
        // jogador segurando arma melee
        if (TryComp<HandsComponent>(attacker, out var hands) &&
            hands.ActiveHandEntity is { } held &&
            HasComp<MeleeWeaponComponent>(held))
            return true;

        return false;
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
                Logger.Info("Parry acabou");
            }
        }
    }
}
