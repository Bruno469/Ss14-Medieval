using System.Linq;
using JetBrains.Annotations;
using Content.Server._Andromeda.Millstone.Components;
using Content.Server.Stack;
using Content.Server.Jittering;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Kitchen.Components;
using Content.Shared._Andromeda.Millstone;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.DoAfter;
using Content.Shared.Random;
using Content.Shared.Stacks;
using Content.Shared.Jittering;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Server._Andromeda.Millstone.EntitySystems
{
    [UsedImplicitly]
    internal sealed class MillstoneSystem : EntitySystem
    {
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainersSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly StackSystem _stackSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly SharedDestructibleSystem _destructible = default!;
        [Dependency] private readonly RandomHelperSystem _randomHelper = default!;
        [Dependency] private readonly JitteringSystem _jitter = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MillstoneComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<MillstoneComponent, ComponentStartup>((uid, _, _) => UpdateUiState(uid));
            SubscribeLocalEvent<MillstoneComponent, MillstoneDoAfterEvent>(OnDoAfterComplete);

            SubscribeLocalEvent<MillstoneComponent, MillstoneStartMessage>(OnStartMessage);
            SubscribeLocalEvent<MillstoneComponent, MillstoneEjectChamberAllMessage>(OnEjectChamberAllMessage);
            SubscribeLocalEvent<MillstoneComponent, MillstoneEjectChamberContentMessage>(OnEjectChamberContentMessage);
            SubscribeLocalEvent<MillstoneComponent, SolutionContainerChangedEvent>(OnSolutionChange);
        }
        private void OnInteractUsing(Entity<MillstoneComponent> entity, ref InteractUsingEvent args)
        {
            var heldEnt = args.Used;
            var inputContainer = _containerSystem.EnsureContainer<Container>(entity.Owner, SharedMillstone.InputContainerId);

            if (!HasComp<ExtractableComponent>(heldEnt))
                return;

            if (args.Handled)
                return;

            if (inputContainer.ContainedEntities.Count >= entity.Comp.StorageMaxEntities)
                return;

            if (!_containerSystem.Insert(heldEnt, inputContainer))
                return;

            args.Handled = true;
            UpdateUiState(entity);
        }

        private void UpdateUiState(EntityUid uid)
        {
            var millstone = default(MillstoneComponent);
            if (!Resolve(uid, ref millstone))
                return;

            var inputContainer = _containerSystem.EnsureContainer<Container>(
                uid,
                SharedMillstone.InputContainerId
            );

            var isBusy = HasComp<ActiveMillstoneComponent>(uid);
            var canGrind = false;

            _solutionContainersSystem.TryGetSolution(
                uid,
                "default",
                out _,
                out var containerSolution
            );

            if (inputContainer.ContainedEntities.Count > 0)
                canGrind = inputContainer.ContainedEntities.All(CanGrind);

            var state = new MillstoneInterfaceState(
                isBusy,
                canGrind,
                GetNetEntityArray(inputContainer.ContainedEntities.ToArray()),
                containerSolution?.Contents.ToArray()
            );

            _userInterfaceSystem.SetUiState(uid, MillstoneUiKey.Key, state);
        }
        private void OnStartMessage(Entity<MillstoneComponent> entity, ref MillstoneStartMessage message)
        {
            if (HasComp<ActiveMillstoneComponent>(entity))
                return;

            var user = GetEntity(message.User);
            if (!Exists(user))
                return;

            var doAfterArgs = new DoAfterArgs(
                EntityManager,
                user,
                entity.Comp.WorkTime,
                new MillstoneDoAfterEvent{},
                entity.Owner,
                entity.Owner
            )
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                BreakOnHandChange = true,
                NeedHand = true,
                DistanceThreshold = 1.5f
            };

            if (!_doAfterSystem.TryStartDoAfter(doAfterArgs))
                return;

            StartMillstone(entity);
        }

        private void StartMillstone(Entity<MillstoneComponent> entity)
        {
            AddComp<ActiveMillstoneComponent>(entity);

            _jitter.AddJitter(entity, -7, 10);

            entity.Comp.AudioStream = _audioSystem.PlayPvs(
                entity.Comp.GrindSound,
                entity.Owner
            )?.Entity;

            UpdateUiState(entity);
        }

        private void FinishWork(Entity<MillstoneComponent> entity)
        {
            var uid = entity.Owner;

            var inputContainer = _containerSystem.EnsureContainer<Container>(
                uid,
                SharedMillstone.InputContainerId
            );

            if (!_solutionContainersSystem.TryGetSolution(
                    uid,
                    "default",
                    out var containerSoln,
                    out var containerSolution))
            {
                StopMillstone(entity);
                return;
            }

            List<(EntityUid item, int newCount)> toSet = new();

            foreach (var item in inputContainer.ContainedEntities.ToList())
            {
                Solution? solution = TryGrindSolution(item, entity, inputContainer.ContainedEntities);

                if (solution is null)
                    continue;

                if (TryComp<StackComponent>(item, out var stack))
                {
                    var totalVolume = solution.Volume * stack.Count;
                    if (totalVolume <= 0)
                        continue;

                    var fitsCount = (int)(
                        stack.Count *
                        FixedPoint2.Min(
                            containerSolution.AvailableVolume / totalVolume + 0.01,
                            1
                        )
                    );

                    if (fitsCount <= 0)
                        continue;

                    var scaledSolution = new Solution(solution);
                    scaledSolution.ScaleSolution(fitsCount);
                    solution = scaledSolution;

                    toSet.Add((item, stack.Count - fitsCount));
                }
                else
                {
                    if (solution.Volume > containerSolution.AvailableVolume)
                        continue;

                    _containerSystem.Remove(item, inputContainer);
                    _destructible.DestroyEntity(item);
                }

                _solutionContainersSystem.TryAddSolution(containerSoln.Value, solution);
            }

            foreach (var (item, amount) in toSet)
            {
                if (amount <= 0)
                {
                    _containerSystem.Remove(item, inputContainer);
                    _destructible.DestroyEntity(item);
                }
                else
                {
                    _stackSystem.SetCount(item, amount);
                }
            }

            _userInterfaceSystem.ServerSendUiMessage(
                uid,
                MillstoneUiKey.Key,
                new MillstoneWorkCompleteMessage()
            );

            StopMillstone(entity);
        }

        private void StopMillstone(Entity<MillstoneComponent> entity)
        {
            entity.Comp.AudioStream = _audioSystem.Stop(entity.Comp.AudioStream);
            RemComp<ActiveMillstoneComponent>(entity);
            RemComp<JitteringComponent>(entity);

            UpdateUiState(entity);
        }

        private void OnDoAfterComplete(Entity<MillstoneComponent> entity, ref MillstoneDoAfterEvent ev)
        {
            if (ev.Cancelled)
            {
                StopMillstone(entity);
                return;
            }

            FinishWork(entity);
        }

        private void OnEjectChamberAllMessage(Entity<MillstoneComponent> entity, ref MillstoneEjectChamberAllMessage message)
        {
            var inputContainer = _containerSystem.EnsureContainer<Container>(entity.Owner, SharedMillstone.InputContainerId);

            if (HasComp<ActiveMillstoneComponent>(entity) || inputContainer.ContainedEntities.Count <= 0)
                return;

            foreach (var toEject in inputContainer.ContainedEntities.ToList())
            {
                _containerSystem.Remove(toEject, inputContainer);
                _randomHelper.RandomOffset(toEject, 0.4f);
            }
            UpdateUiState(entity);
        }

        private void OnEjectChamberContentMessage(Entity<MillstoneComponent> entity, ref MillstoneEjectChamberContentMessage message)
        {
            if (HasComp<ActiveMillstoneComponent>(entity))
                return;

            var inputContainer = _containerSystem.EnsureContainer<Container>(entity.Owner, SharedMillstone.InputContainerId);
            var ent = GetEntity(message.EntityId);

            if (_containerSystem.Remove(ent, inputContainer))
            {
                _randomHelper.RandomOffset(ent, 0.4f);
                UpdateUiState(entity);
            }
        }

        private void OnSolutionChange(Entity<MillstoneComponent> entity, ref SolutionContainerChangedEvent args)
        {
            UpdateUiState(entity.Owner);
        }

        private Solution? TryGrindSolution(EntityUid uid, Entity<MillstoneComponent> grinder, IReadOnlyList<EntityUid> contents)
        {
            if (TryComp<ExtractableComponent>(uid, out var extractable)
                && extractable.GrindableSolution is not null
                && _solutionContainersSystem.TryGetSolution(uid, extractable.GrindableSolution, out _, out var solution))
            {
                var ev = new GrindAttemptEvent(grinder, contents);
                RaiseLocalEvent(uid, ev);

                if (ev.Cancelled)
                    return null;

                return solution;
            }
            else
                return null;
        }

        private bool CanGrind(EntityUid uid)
        {
            var solutionName = CompOrNull<ExtractableComponent>(uid)?.GrindableSolution;

            return solutionName is not null && _solutionContainersSystem.TryGetSolution(uid, solutionName, out _, out _);
        }

        public sealed partial class GrindAttemptEvent : CancellableEntityEventArgs
        {
            public Entity<MillstoneComponent> Grinder;
            public IReadOnlyList<EntityUid> Reagents;

            public GrindAttemptEvent(Entity<MillstoneComponent> grinder, IReadOnlyList<EntityUid> reagents)
            {
                Grinder = grinder;
                Reagents = reagents;
            }
        }
    }
}
