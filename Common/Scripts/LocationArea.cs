using Godot;

public partial class LocationArea : Area2D, Location
{
	[Export]
	public string LocationName { get; set; }

	public CollisionPolygon2D Area { get; set; }

	public override void _Ready()
	{
		if (FindChild("Area") is CollisionPolygon2D) {
			Area = GetNode<CollisionPolygon2D>("Area");
		}
	}

	public (Vector2, float) FindClosestPoint(Vector2 fromPosition)
	{
		if (Area.Polygon.Length == 0) {
			return (Vector2.Inf, 0.0f);
		}

		var closestPoint = Vector2.Inf;
		var closestDistance = float.MaxValue;
		Vector2[] polygon = Area.Polygon;

		for (int i = 0; i < polygon.Length; i++) {
			Vector2 p1 = polygon[i];
			Vector2 p2 = polygon[(i + 1) % polygon.Length];

			Vector2 pointOnSegment = Geometry2D.GetClosestPointToSegment(fromPosition, p1, p2);

			float distance = fromPosition.DistanceTo(pointOnSegment);

			if (distance < closestDistance) {
				closestDistance = distance;
				closestPoint = pointOnSegment;
			}
		}

		return (closestPoint, closestDistance);
	}

	public Vector2 FindCentrePoint()
	{
		var centrePoint = Vector2.Zero;

		if (Area.Polygon.Length > 0) {
			foreach (Vector2 point in Area.Polygon) {
				centrePoint += point;
			}
			centrePoint /= Area.Polygon.Length;
		}

		return centrePoint + GlobalPosition;
	}

	//public Vector2 RandomPoint()
	//{
	//	var shape = new ConvexPolygonShape2D();
	//	shape.SetPointCloud(_area.Polygon);
	//	Rect2 bounds = _area.GetViewportRect();
	//	var random = new Random();

	//	while (true) {
	//		float x = (float)random.NextDouble() * bounds.Size.X + bounds.Position.X;
	//		float y = (float)random.NextDouble() * bounds.Size.Y + bounds.Position.Y;
	//		var point = new Vector2(x, y);
	//	}
	//}
}

