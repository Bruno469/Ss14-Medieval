using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization;

namespace Content.Shared._Andromeda.Millstone
{
    public sealed class SharedMillstone
    {
        public static string InputContainerId = "inputContainer";
    }

    [Serializable, NetSerializable]
    public sealed class MillstoneStartMessage : BoundUserInterfaceMessage
    {
        public NetEntity User;
        public MillstoneStartMessage(NetEntity user)
        {
            User = user;
        }
    }

    [Serializable, NetSerializable]
    public sealed class MillstoneEjectChamberAllMessage : BoundUserInterfaceMessage
    {
        public MillstoneEjectChamberAllMessage()
        {
        }
    }

    [Serializable, NetSerializable]
    public sealed class MillstoneEjectChamberContentMessage : BoundUserInterfaceMessage
    {
        public NetEntity EntityId;
        public MillstoneEjectChamberContentMessage(NetEntity entityId)
        {
            EntityId = entityId;
        }
    }

    [Serializable, NetSerializable]
    public sealed class MillstoneWorkStartedMessage : BoundUserInterfaceMessage
    {
        public MillstoneWorkStartedMessage()
        {
        }
    }

    [Serializable, NetSerializable]
    public sealed class MillstoneWorkCompleteMessage : BoundUserInterfaceMessage
    {
        public MillstoneWorkCompleteMessage()
        {
        }
    }

    [NetSerializable, Serializable]
    public enum MillstoneUiKey : byte
    {
        Key
    }

    [NetSerializable, Serializable]
    public sealed class MillstoneInterfaceState : BoundUserInterfaceState
    {
        public bool IsBusy;
        public bool CanGrind;
        public NetEntity[] ChamberContents;
        public ReagentQuantity[]? ReagentQuantities;

        public MillstoneInterfaceState(bool isBusy, bool canGrind, NetEntity[] chamberContents, ReagentQuantity[]? Contents)
        {
            IsBusy = isBusy;
            CanGrind = canGrind;
            ChamberContents = chamberContents;
            ReagentQuantities = Contents;
        }
    }
}
