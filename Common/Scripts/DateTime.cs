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

	public System.DateTimeKind TimeZone { get; private set; } = System.DateTimeKind.Local;

	public void UpdateDateTime(double dateTimeHours, int dayOfYear, int year)
	{
		DateTimeHours = dateTimeHours;
		DayOfYear = dayOfYear;
		Year = year;
		EmitSignal(SignalName.DateTimeUpdated, DateTimeHours, DayOfYear, Year);
	}

	public int DayOfMonth()
	{
		return CurrentDate().Day;
	}

	public int Hours()
	{
		return (int)DateTimeHours;
	}

	public int Minutes()
	{
		return (int)((DateTimeHours - Hours()) * 60);
	}

	public int Month()
	{
		System.DateTime startOfYear = new(Year, 1, 1);
		System.DateTime date = startOfYear.AddDays(DayOfYear - 1);
		return date.Month;
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

	public System.DateTime Now()
	{
		return new System.DateTime(
			1969 + Year, Month(), DayOfMonth(),
			Hours(), Minutes(), 0
		);
	}

	public string NowString()
	{
		return $"{CurrentDateString()} - {Formatted()}";
	}

	public string CurrentDateString()
	{
		return CurrentDate().ToString(DateFormat);
	}

	public double TimeStamp(System.DateTime toDate = new System.DateTime())
	{
		var baseDate = new System.DateTime(1970, 1, 1, 0, 0, 0, TimeZone);
		if (toDate == new System.DateTime()) {
			toDate = Now();
		}
		return toDate.Subtract(baseDate).TotalSeconds;
	}

	public System.DateTime TimeStampToDateTime(double timestamp)
	{
		System.DateTime dateTime = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, TimeZone);
		return dateTime.AddSeconds(timestamp).ToLocalTime();
	}

	public string TimeStampToDateTimeString(double timestamp)
	{
		return TimeStampToDateTime(timestamp).ToString($"{DateFormat} - HH:mm");
	}
}
