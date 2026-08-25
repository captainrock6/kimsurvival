using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KimSurvival;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ParallelQA
{
    /// <summary>
    /// Editor-only adapter for legacy visual gates. It enters a region through
    /// the production map controls, walks with mapped movement input, interacts
    /// through mapped raw input, and resolves the visible loot tray through its
    /// production buttons/actions. It never mutates the ledger or inventory
    /// directly; every state change is produced by those player-facing actions.
    /// </summary>
    internal static class ProductionSearchNodeQaDriver
    {
        internal sealed class Target
        {
            internal object View;
            internal PrototypeSearchNodeDefinition Definition;
            internal float X;
            internal PrototypeSearchNodeState State;
        }

        internal static void BeginExpedition(KimSurvivalPrototype prototype, PrototypeExpeditionRegionId region, string context)
        {
            Require(prototype != null, context + " prototype exists");
            Invoke(prototype, "BeginExpeditionThroughProductionMap", region);
            Require(prototype.Session.Phase == GamePhase.Exploring && prototype.Session.SelectedRegionId == region,
                context + " production map confirmation selected " + region);
        }

        internal static Target MoveToNext(KimSurvivalPrototype prototype, bool requiresSwimming, string context)
        {
            Require(prototype != null, context + " prototype exists");
            PrototypeSearchNodeRuntime runtime = GetField<PrototypeSearchNodeRuntime>(prototype, "searchNodeRuntime");
            PrototypePlayerTraversal traversal = GetField<PrototypePlayerTraversal>(prototype, "playerTraversal");
            IEnumerable source = GetFieldValue(prototype, "nodes") as IEnumerable;
            Require(source != null, context + " active search-node views exist");

            var candidates = new List<Target>();
            foreach (object view in source)
            {
                if (view == null) continue;
                PrototypeSearchNodeDefinition definition = GetPublicField<PrototypeSearchNodeDefinition>(view, "Definition");
                float x = GetPublicField<float>(view, "X");
                if (definition.RequiresSwimming != requiresSwimming) continue;
                PrototypeSearchNodeState state = runtime.Ledger.GetOrCreate(definition).State;
                if (state == PrototypeSearchNodeState.Depleted) continue;
                candidates.Add(new Target { View = view, Definition = definition, X = x, State = state });
            }

            Target target = candidates
                .OrderBy(value => value.State == PrototypeSearchNodeState.Hidden ? 0 : 1)
                .ThenBy(value => Math.Abs(value.X - traversal.X))
                .ThenBy(value => value.Definition.NodeId, StringComparer.Ordinal)
                .FirstOrDefault();
            Require(target != null, context + " has an undepleted " + (requiresSwimming ? "water" : "land") + " search node");
            Invoke(prototype, "MoveNaturallyToSearchNode", target.View);
            Require(Math.Abs(traversal.X - target.X) <= 0.08f,
                context + " reached " + target.Definition.NodeId + " through production movement");
            Require(prototype.Session.IsSwimming == requiresSwimming,
                context + " traversal state matches node water requirement");
            return target;
        }

        internal static void Open(KimSurvivalPrototype prototype, Target target, string context)
        {
            Require(target != null, context + " target exists");
            PrototypeSearchNodeRuntime runtime = GetField<PrototypeSearchNodeRuntime>(prototype, "searchNodeRuntime");
            PrototypeSearchNodeSnapshot before = runtime.Ledger.GetOrCreate(target.Definition).Clone();
            Invoke(prototype, "InteractWithNearestSearchNodeThroughRawInput", false);
            Require(runtime.IsTrayOpen && runtime.ActiveNode != null &&
                    string.Equals(runtime.ActiveNodeId, target.Definition.NodeId, StringComparison.Ordinal),
                context + " opened the production loot tray for " + target.Definition.NodeId);
            Require(before.State != PrototypeSearchNodeState.Hidden ||
                    runtime.ActiveNode.State == PrototypeSearchNodeState.RevealedPartial,
                context + " hidden node was revealed by the production interaction");
        }

        internal static int TakeAllAndClose(KimSurvivalPrototype prototype, string context)
        {
            PrototypeSearchNodeRuntime runtime = GetField<PrototypeSearchNodeRuntime>(prototype, "searchNodeRuntime");
            Require(runtime.IsTrayOpen && runtime.ActiveNode != null, context + " production loot tray is open");
            string nodeId = runtime.ActiveNodeId;
            int remainingBefore = runtime.ActiveNode.RemainingAmount;
            int replacementCount = 0;
            Invoke(prototype, "ActuateSearchTrayThroughRawInput",
                new PrototypeRawSearchLootInput { KeyboardTakeAll = true });
            if (runtime.HasPendingBagSwap)
            {
                IList buttons = GetFieldValue(prototype, "bagButtons") as IList;
                Require(buttons != null, context + " bag replacement buttons exist");
                GameSession session = prototype.Session;
                Button replacement = Enumerable.Range(0, Math.Min(buttons.Count, session.ActiveBagSlotCount))
                    .Where(index => buttons[index] is Button button && button != null &&
                                    button.gameObject.activeInHierarchy && button.interactable)
                    .OrderBy(index => ReplacementPriority(session.GetBagSlot(index), session.PendingKind))
                    .ThenBy(index => session.GetBagSlot(index).Amount)
                    .Select(index => buttons[index] as Button)
                    .FirstOrDefault();
                Require(replacement != null, context + " production bag replacement button is available");
                Require(EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null &&
                        EventSystem.current.currentSelectedGameObject.GetComponent<Button>() != null,
                    context + " production bag replacement receives UI focus");
                EventSystem.current.SetSelectedGameObject(replacement.gameObject);
                ExecuteEvents.Execute(
                    replacement.gameObject,
                    new BaseEventData(EventSystem.current),
                    ExecuteEvents.submitHandler);
                replacementCount += 1;
            }

            Require(!runtime.HasPendingBagSwap && runtime.ActiveNode != null &&
                    (runtime.ActiveNode.RemainingAmount < remainingBefore || replacementCount > 0) &&
                    (runtime.ActiveNode.State == PrototypeSearchNodeState.RevealedPartial ||
                     runtime.ActiveNode.State == PrototypeSearchNodeState.Depleted),
                context + " committed finite loot from " + nodeId + " through the production loot tray");
            if (runtime.IsTrayOpen)
            {
                Invoke(prototype, "ActuateSearchTrayThroughRawInput",
                    new PrototypeRawSearchLootInput { KeyboardCancel = true });
            }
            Require(!runtime.IsTrayOpen, context + " closed the production loot tray through raw input");
            return replacementCount;
        }

        private static int ReplacementPriority(BagStack stack, ResourceKind? pendingKind)
        {
            if (stack.IsEmpty) return 0;
            if (stack.Kind == ResourceKind.Food) return 1;
            if (pendingKind.HasValue && stack.Kind == pendingKind.Value) return 2;
            return 3;
        }

        internal static void ReturnToCamp(KimSurvivalPrototype prototype, string context)
        {
            object result = Invoke(prototype, "ReturnToCampThroughRawInput");
            Require(result is bool && (bool)result && prototype.Session.Phase == GamePhase.Camp,
                context + " returned through the production mapped input");
        }

        internal static int SearchAndTakeAllNext(
            KimSurvivalPrototype prototype,
            bool requiresSwimming,
            string context)
        {
            Target target = MoveToNext(prototype, requiresSwimming, context);
            Open(prototype, target, context);
            return TakeAllAndClose(prototype, context);
        }

        private static object Invoke(object target, string methodName, params object[] arguments)
        {
            Require(target != null, methodName + " target exists");
            MethodInfo method = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(value => string.Equals(value.Name, methodName, StringComparison.Ordinal))
                .FirstOrDefault(value => ParametersMatch(value.GetParameters(), arguments));
            Require(method != null, "production method " + methodName + " with the required signature exists");
            try
            {
                return method.Invoke(target, arguments);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }

        private static bool ParametersMatch(ParameterInfo[] parameters, object[] arguments)
        {
            if (parameters.Length != arguments.Length) return false;
            for (int index = 0; index < parameters.Length; index += 1)
            {
                object argument = arguments[index];
                if (argument == null)
                {
                    if (parameters[index].ParameterType.IsValueType) return false;
                    continue;
                }
                if (!parameters[index].ParameterType.IsInstanceOfType(argument)) return false;
            }
            return true;
        }

        private static object GetFieldValue(object target, string fieldName)
        {
            Require(target != null, fieldName + " owner exists");
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Require(field != null, "private field " + fieldName + " exists");
            return field.GetValue(target);
        }

        private static T GetField<T>(object target, string fieldName)
        {
            object value = GetFieldValue(target, fieldName);
            Require(value is T, "private field " + fieldName + " is " + typeof(T).Name);
            return (T)value;
        }

        private static T GetPublicField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
            Require(field != null, "node-view field " + fieldName + " exists");
            object value = field.GetValue(target);
            Require(value is T, "node-view field " + fieldName + " is " + typeof(T).Name);
            return (T)value;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
