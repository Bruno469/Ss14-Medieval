using Content.Shared._Andromeda.Input;
using Content.Shared._Andromeda.Parry;
using Robust.Shared.Input.Binding;

public sealed class ClientParrySystem : EntitySystem
{
    public override void Initialize()
    {
        CommandBinds.Builder
            .Bind(AndromedaKeyFunctions.AndromedaActivateParry,
                InputCmdHandler.FromDelegate(session =>
                {
                    if (session?.AttachedEntity is not { } entity)
                        return;

                    RaiseNetworkEvent(new ParryActionEvent());
                }, handle: true))
            .Register<ClientParrySystem>();
    }
}
