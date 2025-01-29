using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Agent : NavigationAgent3D
{
	[Export(PropertyHint.File, "*.toml")]
	public string Schedule { get; set; }

	public List<Task> Tasks { get; private set; } = new();
	public Task ActiveTask { get; set; } = new();
	public Goal CurrentGoal { get; set; } = null;
	public dynamic TargetLocation { get; set; }

	private readonly object _lock = new();
	private List<AiAction> _actions = new();
	private List<Goal> _currentGoals = new();
	private CreatureData _creatureData;

	public override void _Ready()
	{
		var aiData = GD.Load<AiData>(Resources.AiData);
		_creatureData = aiData.CreatureData[GetParent().Name];

		VelocityComputed += _onVelocityComputed;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_creatureData is not null) {
			_creatureData.Direction = Velocity;
		}

		EvaluateGoals();
	}

	public void Initialise()
	{
		Tasks = (new TaskList()).Fetch(Schedule);

		SetGoals();
	}

	public void SetGoals()
	{
		lock (_lock) {
			foreach (var task in Tasks) {
				_currentGoals.Add(new Goal(task));
			}
		}
	}

	public void RemoveGoal(Goal goal)
	{
		lock (_lock) {
			_currentGoals.Remove(goal);
		}
	}

	public void AddAction(AiAction action)
	{
		_actions.Add(action);
	}

	public void EvaluateGoals()
	{
		//GD.Print(Goals().Count());
		if (Goals().Count() == 0) {
			return;
		}

		Goal goal = Goals().First();

		foreach (AiAction action in _actions) {
			var availableActions = _creatureData.Node.Actions;
			if (availableActions.ContainsKey(goal.Task.Action)) {
				action.Execute(ref goal);
				//GD.Print(goal.Name, " ", goal.Task.Location.Name, " ", goal.IsActive);
			}
		}
	}

	public IOrderedEnumerable<Goal> Goals()
	{
		lock (_lock) {
			List<Goal> goalsList = new(_currentGoals);
			IOrderedEnumerable<Goal> goals = goalsList
				.OrderByDescending(goal => goal.Priority);
			return goals;
		}
	}


	public void SetTargetPosition()
	{
		if (TargetLocation.Type == "area" && TargetPosition == Vector3.Inf) {
			// since determine the closest outer edge of the area can be unreliable
			// we target the centre point of the area and stop moving as soon as
			// we enter the area (determined in the AreaEntered signal)
			//TargetLocation = Locations.LocationAreas[TargetLocation.Name] as LocationArea;
			//TargetPosition = TargetLocation.FindCentrePoint();
		}
		else if (TargetLocation.Type == "marker") {
			var markers = Locations.LocationMarkers;

			if (TargetLocation.Name == "random") {
				var random = new Random();
				var keys = new List<string>(markers.Keys);
				string randomLocationName = keys[random.Next(keys.Count)];
				TargetLocation = Locations.LocationMarkers[randomLocationName];
				TargetPosition = TargetLocation.GlobalPosition;
			}
			else {
				if (markers.ContainsKey(TargetLocation.Name)) {
					TargetLocation = markers[TargetLocation.Name] as LocationMarker;
					TargetPosition = TargetLocation.GlobalPosition;
				}
			}
		}
	}

	public void FollowPath()
	{
		Vector3 globalPosition = _creatureData.Controller.GlobalPosition;
		Vector3 nextPathPosition = GetNextPathPosition();
		Vector3 newVelocity = globalPosition.DirectionTo(nextPathPosition);

		if (AvoidanceEnabled) {
			SetVelocityForced(newVelocity);
		}
		else {
			_onVelocityComputed(newVelocity);
		}
	}

	public void EndNavigation()
	{
		Velocity = Vector3.Zero;
		//CurrentPointPath = new();
		TargetLocation = null;
		TargetPosition = Vector3.Inf;

		//RemoveGoal(CurrentGoal);
		CurrentGoal = null;

		_creatureData.Parent.GlobalPosition = _creatureData.Controller.GlobalPosition;
		_creatureData.Controller.Position = Vector3.Zero;
	}

	private void _onVelocityComputed(Vector3 safeVelocity)
	{
		Velocity = safeVelocity;
	}
}
