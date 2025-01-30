using Godot;

/**
 * modified C# port of https://github.com/krzmig/godot-simple-sky-project
 * with clouds and shader support removed since you wont see it here anyway
 */

[Tool]
public partial class World : Node
{
	[Export]
	public DirectionalLight3D Sun { get; set; }

	[Export]
	public DirectionalLight3D AnomalySun { get; set; }

	[Export]
	public DirectionalLight3D Moon { get; set; }

	[Export]
	public WorldEnvironment Environment { get; set; }

	[Export]
	public bool PauseTime { get; set; } = false;

	/// <summary>
	/// If set to true directional light transform won't be affected
	/// </summary>
	[Export]
	public bool TimeOnly { get; set; } = false;

	// For simplify, a local time, I skip totally a longitude
	private double _dayTimeHours;
	[Export(PropertyHint.Range, "0.0, 24, 0.0001")]
	public double DayTimeHours {
		get => _dayTimeHours;
		set {
			_dayTimeHours = value;
			if (DayTimeHours < 0.0) {
				_dayTimeHours += HoursInDay;
				_dayOfYear -= 1;
			}
			else if (DayTimeHours > HoursInDay) {
				_dayTimeHours -= HoursInDay;
				_dayOfYear += 1;
			}
			_update();
		}
	}

	public (int Hour, int Minute, string Formatted) DayTime { get {
		int h = (int)DayTimeHours;
		int m = (int)((DayTimeHours - h) * 60);
		int[] dt = {h, m};
		return (h, m, $"{h:D2}:{m:D2}");
	}}

	/// <summary>
	/// Day of year. In game, if you reach DAYS_IN_YEAR, don't set 0
	/// to keep correct position of the moon
	/// </summary>
	private int _dayOfYear;
	[Export(PropertyHint.Range, "1, 365, 1")]
	public int DayOfYear {
		get => _dayOfYear == 0 ? 1 : _dayOfYear;
		set {
			_dayOfYear = value;
			_update();
		}
	}

	// For simplify, a local time, I skip totally a longitude
	private float _latitude;
	[Export(PropertyHint.Range, "-90, 90, 0.000001")]
	public float Latitude {
		get => _latitude == 0 ? 51.050407f : _latitude;
		set {
			_latitude = value;
			_update();
		}
	}

	/// <summary>
	/// The tilt of the rotational axis resulting in the occurrence of seasons
	/// <summary/>
	private double _planetAxialTilt;
	[Export(PropertyHint.Range, "-180, 180, 0.01")]
	public double PlanetAxialTilt {
		get => _planetAxialTilt == 0 ? 23.5 : _planetAxialTilt;
		set {
			_planetAxialTilt = value;
			_update();
		}
	}

	/// <summary>
	/// The deviation of the moon's orbit from the earth's orbit
	/// </summary>
	private double _moonOrbitalInclination;
	[Export(PropertyHint.Range, "-180, 180, 0.01")]
	public double MoonOrbitalInclination {
		get => _moonOrbitalInclination == 0 ? 5.14 : _moonOrbitalInclination;
		set {
			_moonOrbitalInclination = value;
		 	_updateMoon();
		}
	}

	/// <summary>
	/// Time required for the moon to orbit around the earth
	/// (in days = one rotation of the earth around its own axis)
	/// </summary>
	private double _moonOrbitalPeriod;
	[Export(PropertyHint.Range, "0.1, 365, 0.01")]
	public double MoonOrbitalPeriod {
		get => _moonOrbitalPeriod == 0 ? 29.5 : _moonOrbitalPeriod;
		set {
			_moonOrbitalPeriod = value;
		 	_updateMoon();
		}
	}

	/// <summary>
	/// Multiplier of the elapsed time in the running game.<br />
	/// If 0.0 - day time will not be changed automatically.<br />
	/// If 1.0 - one hour will take one second.
	/// </summary>
	[Export(PropertyHint.Range, "0.0, 1.0, 0.0001")]
	public double TimeScale { get; set; } = 0.00666f;

	// If 0, the value will set from the sun object, but as the script runs
	// in the editor, it may set the wrong value, so it is best to set it manually.
	private double _sunBaseEnergy;
	[Export(PropertyHint.Range, "0.0, 1.0, 0.01")]
	public double SunBaseEnergy {
		get => _sunBaseEnergy == 0 ? 1 : _sunBaseEnergy;
		set { _sunBaseEnergy = value; }
	}

	private double _anomalySunBaseEnergy;
	[Export(PropertyHint.Range, "0.0, 1.0, 0.01")]
	public double AnomalySunBaseEnergy {
		get => _anomalySunBaseEnergy;
		set { _anomalySunBaseEnergy = value; }
	}

	// If 0, the value will set from the moon object, but as the script runs
	// in the editor, it may set the wrong value, so it is best to set it manually.
	private double _moonBaseEnergy;
	[Export(PropertyHint.Range, "0.0, 1.0, 0.01")]
	public double MoonBaseEnergy {
		get => _moonBaseEnergy == 0 ? 0.2 : _moonBaseEnergy;
		set { _moonBaseEnergy = value; }
	}

	public static double HoursInDay { get; } = 24;
	public static double DaysInYear { get; } = 365;

	public override void _Ready()
	{
		if (IsInstanceValid(Sun)) {
			Sun.Position = Vector3.Zero;
			Sun.Rotation = Vector3.Zero;
			Sun.RotationOrder = EulerOrder.Zxy;

			if (SunBaseEnergy == 0) {
				SunBaseEnergy = Sun.LightEnergy;
			}
		}

		//if (IsInstanceValid(AnomalySun)) {
		//	AnomalySun.Position = Vector3.Zero;
		//	AnomalySun.Rotation = Vector3.Zero;
		//	AnomalySun.RotationOrder = EulerOrder.Zxy;

		//	if (AnomalySunBaseEnergy == 0) {
		//		AnomalySunBaseEnergy = AnomalySun.LightEnergy;
		//	}
		//}

		if (IsInstanceValid(Moon)) {
			Moon.Position = Vector3.Zero;
			Moon.Rotation = Vector3.Zero;
			Moon.RotationOrder = EulerOrder.Zxy;

			if (MoonBaseEnergy == 0.0f) {
				MoonBaseEnergy = Moon.LightEnergy;
			}
		}

		_update();
	}

	public override void _Process(double delta)
	{
		if (!Engine.IsEditorHint() && !PauseTime) {
			DayTimeHours += delta * TimeScale;
		}
	}

	private void _update()
	{
		_updateSun();
		_updateMoon();
	}

	private void _updateSun()
 	{
		if (!IsInstanceValid(Sun)) {
			return;
		}

		double dayProgress = DayTimeHours / HoursInDay;

		if (TimeOnly) {
			dayProgress = 10 / HoursInDay;
		}

		// 193 is the number of days from the summer solstice to the end of the year.
		// Here we want 0 for the summer solstice and 1 for the winter solstice.
		double earthOrbitProgress = (DayOfYear + 193 + dayProgress) / DaysInYear;
		Sun.Rotation = new Vector3(
			// Sunset and sunrise
			(float)(dayProgress * 2.0 - 0.5) * -Mathf.Pi,
			// Rotation to the deviation of the axis of rotation from the orbit.
			// This gives us shorter days in winter and longer days in summer.
			(float)Mathf.DegToRad(Mathf.Cos(earthOrbitProgress * Mathf.Pi * 2) * PlanetAxialTilt),
			Mathf.DegToRad(Latitude)
		);

		if (IsInsideTree()) {
			// Disabling light under the horizon
			Vector3 sunDirection = Sun.ToGlobal(Vector3.Back).Normalized();
			Sun.LightEnergy = (float)Mathf.SmoothStep(
				-0.05,
				0.1,
				sunDirection.Y
			) * (float)SunBaseEnergy;

			Vector3 AnomalySunDirection = Sun.ToGlobal(Vector3.Back).Normalized();
			AnomalySun.LightEnergy = (float)Mathf.SmoothStep(
				-0.05,
				0.1,
				AnomalySunDirection.Y
			) * (float)AnomalySunBaseEnergy;
		}
	}

	private void _updateMoon()
 	{
		if (TimeOnly) {
			return;
		}

		double dayProgress = DayTimeHours / HoursInDay;

		if (IsInstanceValid(Moon)) {
			// Progress of the moon's orbital rotation in days
			double moonOrbitProgress = (((double)DayOfYear % MoonOrbitalPeriod) + dayProgress) / MoonOrbitalPeriod;
			double axialTilt = MoonOrbitalInclination;
			// Adding a planet axial tilt depending on the time of day
			axialTilt += PlanetAxialTilt * Mathf.Sin((dayProgress * 2 - 1) * Mathf.Pi);
			Moon.Rotation = new Vector3(
				(float)((dayProgress - moonOrbitProgress) * 2 - 1) * Mathf.Pi,
				 Mathf.DegToRad((float)axialTilt),
				Mathf.DegToRad(Latitude)
			);

			if (IsInsideTree()) {
				// Disabling light under the horizon
				Vector3 moonDirection = Moon.ToGlobal(Vector3.Back).Normalized();
				Moon.LightEnergy = (float)Mathf.SmoothStep(
					-0.05f,
					0.1f,
					moonDirection.Y
				) * (float)MoonBaseEnergy;
			}
		}
	}
}
