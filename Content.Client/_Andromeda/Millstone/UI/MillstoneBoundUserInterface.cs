using Content.Shared.Containers.ItemSlots;
using Content.Shared._Andromeda.Millstone;
using Content.Client._Andromeda.Millstone.UI;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Millstone.UI
{
    public sealed class MillstoneBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private MillstoneMenu? _menu;

        public MillstoneBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = this.CreateWindow<MillstoneMenu>();
            _menu.OnGrind += StartGrinding;
            _menu.OnEjectAll += EjectAll;
            _menu.OnEjectChamber += EjectChamberContent;
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (state is not MillstoneInterfaceState cState)
                return;

            _menu?.UpdateState(cState);
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            base.ReceiveMessage(message);
            _menu?.HandleMessage(message);
        }

        public void StartGrinding()
        {
            var playerMan = IoCManager.Resolve<IPlayerManager>();
            var user = playerMan.LocalSession?.AttachedEntity;

            if (user == null)
                return;

            SendMessage(new MillstoneStartMessage(
                EntMan.GetNetEntity(user.Value)
            ));
        }

        public void EjectAll()
        {
            SendMessage(new MillstoneEjectChamberAllMessage());
        }

        public void EjectChamberContent(EntityUid uid)
        {
            SendMessage(new MillstoneEjectChamberContentMessage(EntMan.GetNetEntity(uid)));
        }
    }
}
