using Content.Shared.Blocking;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;


namespace Content.Shared._Andromeda.Parry;

/// <summary>
/// This handles...
/// </summary>
public sealed class ParrySystem : EntitySystem
{

    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ParryComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ParryUserComponent, DamageModifyEvent>(OnUserDamageModified);
    }

    private void OnUseInHand(Entity<ParryComponent> ent, ref UseInHandEvent args)
    {
        //_audio.PlayPvs(ent.Comp.Sound, ent.Owner);
        if (args.Handled || HasComp<ParryUserComponent>(args.User))
        {
            Logger.Info("Handled");
            return;
        }

        ActiveParry(args.User);
        Logger.Info("USADO");
        args.Handled = true;
    }

    private void OnUserDamageModified(Entity<ParryUserComponent> ent, ref DamageModifyEvent args)
    {

        if (args.Damage.GetTotal() <= 0)
            return;

        if (!TryComp<DamageableComponent>(ent.Owner, out var dmgComp))
            return;

        _popup.PopupPredicted("Toma Parry", ent.Owner, ent.Owner, PopupType.Large);
        var modify = new DamageModifierSet();
        foreach (var key in dmgComp.Damage.DamageDict.Keys)
        {
            modify.Coefficients.TryAdd(key, 0);
        }

        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, modify);

        if (!args.Damage.Equals(args.OriginalDamage))
        {
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/sword_crash.ogg"), ent.Owner);
        }

    }

    private void ActiveParry(EntityUid owner)
    {
        if (HasComp<ParryComponent>(owner))
            return;
        AddComp<ParryUserComponent>(owner);
        Logger.Info("Parry Ativo");
    }
}
