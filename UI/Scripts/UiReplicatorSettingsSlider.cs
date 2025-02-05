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

	public override void _Ready()
	{
		_amount = GetParent().FindChild("Amount", true) as Label;
		_sceneRoot = Owner as UiReplicator;
		_setInitialValue();

		ValueChanged += _onSliderValueChanged;
	}

	private void _setInitialValue()
	{
		ArtifactGrowCondition condition = _currentGrowCondition();
		Replicator replicator = _sceneRoot.Replicator;
		var replicatorSettings = replicator.CurrentSettings;
		if (replicatorSettings.ContainsKey(condition)) {
			Value = replicatorSettings[condition].Value;
		}
		_amount.Text = Value.ToString();
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
