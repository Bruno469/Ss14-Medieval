using Content.Shared._Andromeda.Millstone;
using Content.Server._Andromeda.Millstone.EntitySystems;
using Robust.Shared.Audio;

namespace Content.Server._Andromeda.Millstone.Components
{
    [Access(typeof(MillstoneSystem)), RegisterComponent]
    public sealed partial class MillstoneComponent : Component
    {
        [DataField]
        public int StorageMaxEntities = 5;

        [DataField]
        public TimeSpan WorkTime = TimeSpan.FromSeconds(3.5);

        [DataField]
        public float WorkTimeMultiplier = 1;

        [DataField]
        public SoundSpecifier GrindSound { get; set; } = new SoundPathSpecifier("/Audio/_Andromeda/Machines/Millstone.ogg");
        public EntityUid? AudioStream;
    }

    [Access(typeof(MillstoneSystem)), RegisterComponent]
    public sealed partial class ActiveMillstoneComponent : Component
    {
        [ViewVariables]
        public TimeSpan EndTime;
    }
}
