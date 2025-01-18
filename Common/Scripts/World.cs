using Godot;

//[Tool]
public partial class World : Node
{
	[Export]
	public DirectionalLight3D Sun { get; set; }

	//[Export]
	//public DirectionalLight3D Moon { get; set; }

	//[Export]
	//public WorldEnvironment Environment { get; set; }

	//private double _dayTime;
	//[Export(PropertyHint.Range, "0.0, 24, 0.0001")]
	//public double DayTime
	//{
	//	get => _dayTime;
	//	set {
	//		_dayTime = value;
	//		if (DayTime < 0.0) {
	//			_dayTime += HoursInDay;
	//			_dayOfYear -= 1;
	//		}
	//		else if (DayTime > HoursInDay) {
	//			_dayTime -= HoursInDay;
	//			_dayOfYear += 1;
	//		}
	//		_update();
	//	}
	//}

	//private int _dayOfYear;
	//[Export(PropertyHint.Range, "1, 365, 1")]
	//public int DayOfYear
	//{
	//	get => _dayOfYear;
	//	set {
	//		_dayOfYear = value;
	//		_update();
	//	}
	//}

	//private float _latitude;
	//[Export(PropertyHint.Range, "-90, 90, 0.01")]
	//public float Latitude
	//{
	//	get => _latitude;
	//	set {
	//		_latitude = value;
	//		_update();
	//	}
	//}

	//private double _planetAxialTilt;
	//[Export(PropertyHint.Range, "-180, 180, 0.01")]
	//public double PlanetAxialTilt
	//{
	//	get => _planetAxialTilt == 0 ? 23.44 : _planetAxialTilt;
	//	set {
	//		_planetAxialTilt= value;
	//		_update();
	//	}
	//}

	//private double _moonOrbitalInclination;
	//[Export(PropertyHint.Range, "-180, 180, 0.01")]
	//public double MoonOrbitalInclination
	//{
	//	get => _moonOrbitalInclination == 0 ? 5.14 : _moonOrbitalInclination;
	//	set {
	//		_moonOrbitalInclination = value;
	//	 	_updateMoon();
	//	}
	//}

	//private double _moonOrbitalPeriod;
	//[Export(PropertyHint.Range, "0.1, 365, 0.01")]
	//public double MoonOrbitalPeriod
	//{
	//	get => _moonOrbitalPeriod == 0 ? 29.5 : _moonOrbitalPeriod;
	//	set {
	//		_moonOrbitalPeriod = value;
	//	 	_updateMoon();
	//	}
	//}
	//
	//private double _timeScale;
	//[Export(PropertyHint.Range, "0.0, 1.0, 0.01")]
	//public double TimeScale { get; set; } = 0.01f;
	//
	//private double _sunBaseEnergy;
	//[Export(PropertyHint.Range, "0.0, 1.0, 0.01")]
	//public double SunBaseEnergy
	//{
	//	get => _sunBaseEnergy == 0 ? 1 : _sunBaseEnergy;
	//	set {
	//		_sunBaseEnergy = value;
	//		_updateShader();
	//	}
	//}
	//
	//private double _moonBaseEnergy;
	//[Export(PropertyHint.Range, "0.0, 1.0, 0.01")]
	//public double MoonBaseEnergy
	//{
	//	get => _moonBaseEnergy == 0 ? 0.2 : _moonBaseEnergy;
	//	set {
	//		_moonBaseEnergy = value;
	//		_updateShader();
	//	}
	//}

	//public static double HoursInDay { get; } = 24;
	//public static double DaysInYear { get; } = 365;

	//public override void _Ready()
	//{
	//	return;
	//	if (IsInstanceValid(Sun)) {
	//		Sun.Position = Vector3.Zero;
	//		Sun.Rotation = Vector3.Zero;
	//		Sun.RotationOrder = EulerOrder.Zxy;

	//		if (_sunBaseEnergy == 0) {
	//			_sunBaseEnergy = Sun.LightEnergy;
	//		}
	//	}

	//	if (IsInstanceValid(Moon)) {
	//		Moon.Position = Vector3.Zero;
	//		Moon.Rotation = Vector3.Zero;
	//		Moon.RotationOrder = EulerOrder.Zxy;

	//		if (_moonBaseEnergy == 0.0f) {
	//			_moonBaseEnergy = Moon.LightEnergy;
	//		}
	//	}
	//	
	//	_update();
	//}

	//public override void _Process(double delta)
	//{
	//	if (Engine.IsEditorHint()) {
	//		return;
	//	}
	//	_dayTime += delta * _timeScale;
	//	GD.Print("Day time: " + _dayTime);
	//}

	//private void _update()
	//{
	//	_updateSun();
	//	_updateMoon();
	//	//_updateShader();
	//}
	//
	//private void _updateSun()
 	//{
	//	if (IsInstanceValid(Sun)) {
	//		double dayProgress = _dayTime / HoursInDay;
	//		double earthOrbitProgress = _dayOfYear + 193 + dayProgress;
	//		Sun.Rotation = new Vector3(
	//			(float)(dayProgress * 2.0 - 0.5) * -Mathf.Pi,
	//			(float)Mathf.DegToRad(Mathf.Cos(earthOrbitProgress * Mathf.Pi * 2.0) * _planetAxialTilt),
	//			Mathf.DegToRad(_latitude)
	//		);
	//		if (IsInsideTree()) {
	//			Vector3 sunDirection = Sun.ToGlobal(new Vector3(0, 0, 1).Normalized());
	//			Sun.LightEnergy = (float)Mathf.SmoothStep(
	//			-0.0,
	//			0.1,
	//			sunDirection.Y
	//		) * (float)_sunBaseEnergy;
	//		}
	//	}
	//}

	//private void _updateMoon()
 	//{
	//	double dayProgress = _dayTime / HoursInDay;

	//	if (IsInstanceValid(Moon)) {
	//		double moonOrbitProgress = ((double)_dayOfYear % _moonOrbitalPeriod) + dayProgress;
	//		double axialTilt = _moonOrbitalInclination;
	//		axialTilt += _planetAxialTilt * Mathf.Sin((dayProgress * 2 - 1) * Mathf.Pi);
	//		Moon.Rotation = new Vector3(
	//			(float)(dayProgress - moonOrbitProgress * 2.0 - 0.5) * -Mathf.Pi,
	//			Mathf.DegToRad((float)axialTilt),
	//			Mathf.DegToRad(_latitude)
	//		);
	//		if (IsInsideTree()) {
	//			Vector3 moonDirection = Moon.ToGlobal(new Vector3(0, 0, 1).Normalized());
	//			Moon.LightEnergy = (float)Mathf.SmoothStep(-0.0f, 0.1f, moonDirection.Y) * (float)_sunBaseEnergy;
	//		}
	//	}
	//}

	//private void _updateShader()
	//{
	//	if (!IsInstanceValid(Environment)) {
	//		return;
	//	}

	//	if (Environment.Environment.Sky is null) {
	//		return;
	//	}
	//	
	//	//Environment.Environment.Sky.SkyMaterial.
	//}
}