using AssetRipper.Assets;
using AssetRipper.Assets.Bundles;
using AssetRipper.SourceGenerated.Classes.ClassID_91;
using AssetRipper.SourceGenerated.Enums;
using AssetRipper.SourceGenerated.Extensions;
using AssetRipper.SourceGenerated.Subclasses.ConditionConstant;
using AssetRipper.SourceGenerated.Subclasses.StateConstant;
using AssetRipper.SourceGenerated.Subclasses.StateMachineConstant;
using AssetRipper.SourceGenerated.Subclasses.TransitionConstant;

namespace AssetRipper.Premium;

/// <summary>
/// Reads the Mecanim controller constants which the normal importer has already made available.
/// This reports structure and conditions only; it does not invent states, parameters, scripts, or
/// blend weights for missing controller data.
/// </summary>
public static class PremiumMecanimStateMachineAnalyzer
{
	public static PremiumMecanimReport Create(GameBundle gameBundle)
	{
		ArgumentNullException.ThrowIfNull(gameBundle);
		PremiumMecanimControllerSummary[] controllers = gameBundle.FetchAssets()
			.OfType<IAnimatorController>()
			.OrderBy(static controller => GetNodeId(controller), StringComparer.Ordinal)
			.Select(CreateControllerSummary)
			.ToArray();
		return new PremiumMecanimReport(
			controllers.LongLength,
			controllers.Sum(static controller => controller.StateMachineCount),
			controllers.Sum(static controller => controller.StateCount),
			controllers.Sum(static controller => controller.TransitionCount),
			controllers.Sum(static controller => controller.ConditionCount),
			controllers.Sum(static controller => controller.BlendTreeStateCount),
			controllers.Sum(static controller => controller.UnresolvedParameterBindingCount),
			controllers);
	}

	private static PremiumMecanimControllerSummary CreateControllerSummary(IAnimatorController controller)
	{
		HashSet<uint> parameterIds = controller.Controller.Values.Data.ValueArray
			.Select(static value => value.ID)
			.ToHashSet();
		List<PremiumMecanimStateSummary> states = [];
		List<PremiumMecanimTransitionSummary> transitions = [];
		long conditionCount = 0;
		long unresolvedParameterBindings = 0;

		for (int machineIndex = 0; machineIndex < controller.Controller.StateMachineArray.Count; machineIndex++)
		{
			IStateMachineConstant stateMachine = controller.Controller.StateMachineArray[machineIndex].Data;
			for (int stateIndex = 0; stateIndex < stateMachine.StateConstantArray.Count; stateIndex++)
			{
				IStateConstant state = stateMachine.StateConstantArray[stateIndex].Data;
				uint stateId = state.GetId();
				bool isBlendTree = state.IsBlendTree();
				int blendTreeNodeCount = isBlendTree ? state.GetBlendTree().NodeArray.Count : 0;
				states.Add(new PremiumMecanimStateSummary(machineIndex, stateIndex, stateId, isBlendTree, blendTreeNodeCount, state.GetWriteDefaultValues()));

				for (int transitionIndex = 0; transitionIndex < state.TransitionConstantArray.Count; transitionIndex++)
				{
					ITransitionConstant transition = state.TransitionConstantArray[transitionIndex].Data;
					PremiumMecanimConditionSummary[] conditions = transition.ConditionConstantArray
						.Select(conditionPointer => CreateConditionSummary(conditionPointer.Data, parameterIds, out bool isUnresolved))
						.ToArray();
					conditionCount += conditions.LongLength;
					unresolvedParameterBindings += conditions.LongCount(static condition => condition.IsParameterUnresolved);
					transitions.Add(new PremiumMecanimTransitionSummary(
						machineIndex,
						stateId,
						transition.DestinationState,
						transitionIndex,
						transition.GetHasExitTime(),
						transition.GetExitTime(),
						transition.TransitionDuration,
						transition.GetInterruptionSource().ToString(),
						conditions));
				}
			}
		}

		PremiumMecanimStateSummary[] orderedStates = states
			.OrderBy(static state => state.StateMachineIndex)
			.ThenBy(static state => state.StateId)
			.ThenBy(static state => state.StateIndex)
			.ToArray();
		PremiumMecanimTransitionSummary[] orderedTransitions = transitions
			.OrderBy(static transition => transition.StateMachineIndex)
			.ThenBy(static transition => transition.SourceStateId)
			.ThenBy(static transition => transition.DestinationStateId)
			.ThenBy(static transition => transition.TransitionIndex)
			.ToArray();
		return new PremiumMecanimControllerSummary(
			GetNodeId(controller),
			controller.Controller.StateMachineArray.Count,
			orderedStates.LongLength,
			orderedTransitions.LongLength,
			conditionCount,
			orderedStates.LongCount(static state => state.IsBlendTree),
			unresolvedParameterBindings,
			orderedStates,
			orderedTransitions);
	}

	private static PremiumMecanimConditionSummary CreateConditionSummary(ConditionConstant condition, IReadOnlySet<uint> parameterIds, out bool isUnresolved)
	{
		bool isExitTime = condition.ConditionModeE == AnimatorConditionMode.ExitTime;
		isUnresolved = !isExitTime && !parameterIds.Contains(condition.EventID);
		return new PremiumMecanimConditionSummary(
			condition.EventID,
			condition.ConditionModeE.ToString(),
			condition.EventThreshold,
			condition.ExitTime,
			isExitTime,
			isUnresolved);
	}

	private static string GetNodeId(IUnityObjectBase asset)
	{
		string collectionPath = string.IsNullOrWhiteSpace(asset.Collection.FilePath) ? asset.Collection.Name : asset.Collection.FilePath;
		return $"{collectionPath}:{asset.PathID}";
	}
}

public sealed record PremiumMecanimConditionSummary(uint ParameterId, string Mode, float Threshold, float ExitTime, bool IsExitTime, bool IsParameterUnresolved);
public sealed record PremiumMecanimStateSummary(int StateMachineIndex, int StateIndex, uint StateId, bool IsBlendTree, int BlendTreeNodeCount, bool WriteDefaultValues);
public sealed record PremiumMecanimTransitionSummary(int StateMachineIndex, uint SourceStateId, uint DestinationStateId, int TransitionIndex, bool HasExitTime, float ExitTime, float Duration, string InterruptionSource, IReadOnlyList<PremiumMecanimConditionSummary> Conditions);
public sealed record PremiumMecanimControllerSummary(string Id, long StateMachineCount, long StateCount, long TransitionCount, long ConditionCount, long BlendTreeStateCount, long UnresolvedParameterBindingCount, IReadOnlyList<PremiumMecanimStateSummary> States, IReadOnlyList<PremiumMecanimTransitionSummary> Transitions);
public sealed record PremiumMecanimReport(long ControllerCount, long StateMachineCount, long StateCount, long TransitionCount, long ConditionCount, long BlendTreeStateCount, long UnresolvedParameterBindingCount, IReadOnlyList<PremiumMecanimControllerSummary> Controllers);
