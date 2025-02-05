using Godot;
using System;
using System.Collections.Generic;

public struct SliderProperties
{
	public double Value { get; set; }
	public double MaxValue { get; set; }
	public double Step { get; set; }

	public SliderProperties(double value, double maxValue, double step)
	{
		Value = value;
		MaxValue = maxValue;
		Step = step;
	}
}

public partial class UiReplicatorSettingsSlider : HSlider
{
	private Dictionary<
		ArtifactGrowCondition,
		SliderProperties
	> _sliderValues { get; set; } = new();

	private Label _amount;
	private UiReplicator _sceneRoot;
	private ReplicatorStorage _replicatorStorage = new();
	private Replicator _replicator;

	public override void _Ready()
	{
		_amount = GetParent().FindChild("Amount", true) as Label;
		_sceneRoot = Owner as UiReplicator;
		_replicatorStorage = GD.Load<ReplicatorStorage>(Resources.ReplicatorStorage);

		_setInitialValue();
		ValueChanged += _onSliderValueChanged;
	}

	private void _setInitialValue()
	{
		ArtifactGrowCondition condition = _currentGrowCondition();
		SliderProperties sliderProperties = new(Value, MaxValue, Step);

		var replicator = _sceneRoot.Replicator;
		if (_replicatorStorage.Replicators.ContainsKey(replicator)) {
			_replicatorStorage.Replicator = _replicator;
			var replicatorSettings = _replicatorStorage.Content(replicator).Settings;
			foreach (var (key, setting) in replicatorSettings) {
				if (key == condition) {
					Value = setting.Value;
					sliderProperties = setting;
					break;
				}
			}
		}

		_amount.Text = Value.ToString();
		_sceneRoot.Replicator.UpdateSettings(condition, sliderProperties);
	}

	private void _onSliderValueChanged(double value)
	{
		_amount.Text = value.ToString();
		SliderProperties sliderProperties = new(value, MaxValue, Step);
		ArtifactGrowCondition condition = _currentGrowCondition();

		if (!_sliderValues.ContainsKey(condition)) {
			_sliderValues.Add(condition, sliderProperties);
		}
		else {
			_sliderValues[condition] = sliderProperties;
		}

		// let replicator know whats going on
		_sceneRoot.Replicator.UpdateSettings(condition, sliderProperties);
		_sceneRoot.SliderValues = _sliderValues;
	}

	private ArtifactGrowCondition _currentGrowCondition()
	{
		foreach (ArtifactGrowCondition condition in _enumValues()) {
			if (condition.ToString() != GetParent().Name) {
				continue;
			}
			return condition;
		}
		return new();
	}

	private Array _enumValues()
	{
		return Enum.GetValues(typeof(ArtifactGrowCondition));
	}
}
