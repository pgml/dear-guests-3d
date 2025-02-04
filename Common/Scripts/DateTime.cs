using Godot;

public partial class DateTime : Resource
{
	[Signal]
	public delegate void DateTimeUpdatedEventHandler(
		double dayTimeHours,
		int dayOfYear,
		int year
	);

	[Export(PropertyHint.Enum, "dd. MMM,MMM dd")]
	public string DateFormat { get; set; } = "dd. MMM";

	public double DateTimeHours = 0;
	public int DayOfYear = 1;
	public int Year = 1;

	public void UpdateDateTime(double dateTimeHours, int dayOfYear, int year)
	{
		DateTimeHours = dateTimeHours;
		DayOfYear = dayOfYear;
		Year = year;
		EmitSignal(SignalName.DateTimeUpdated, DateTimeHours, DayOfYear, Year);
	}

	public int Hours()
	{
		return (int)DateTimeHours;
	}

	public int Minutes()
	{
		return (int)((DateTimeHours - Hours()) * 60);
	}

	public int TwentyFour()
	{
		return System.Convert.ToInt32(Formatted().Replace(":", ""));
	}

	public string Formatted()
	{
		return $"{Hours():D2}:{Minutes():D2}";
	}

	public System.DateTime CurrentDate()
	{
		System.DateTime startOfYear = new(Year, 1, 1);
		System.DateTime date = startOfYear.AddDays(DayOfYear - 1);
		return date;
	}

	public string CurrentDateString()
	{
		return CurrentDate().ToString(DateFormat);
	}

	public int Month()
	{
		System.DateTime startOfYear = new(Year, 1, 1);
		System.DateTime date = startOfYear.AddDays(DayOfYear - 1);
		return date.Month;
	}

	public double TimeStamp()
	{
		var baseDate = new System.DateTime(
			1969 + Year, 1, 1,
			Hours(), Minutes(), 0
		);

		var toDate = new System.DateTime(
			1969 + Year, Month(), DayOfYear,
			Hours(), Minutes(), 0
		);

		return toDate.Subtract(baseDate).TotalSeconds;
	}
}
