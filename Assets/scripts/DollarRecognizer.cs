using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DollarRecognizer
{
	public class Unistroke
	{
		public int ExampleIndex;
		public string Name { get; private set; }
		public Vector2[] Points { get; private set; }
		public float Angle { get; private set; }
		public List<float> Vector { get; private set; }

		public Unistroke(string name, IEnumerable<Vector2> points)
		{
			Name = string.Intern(name);
			Points = DollarRecognizer.resample(points, _kNormalizedPoints);
			Angle = DollarRecognizer.indicativeAngle(Points);
			DollarRecognizer.rotateBy(Points, -Angle);
			DollarRecognizer.scaleTo(Points, _kNormalizedSize);
			DollarRecognizer.translateTo(Points, Vector2.zero);
			Vector = DollarRecognizer.vectorize(Points);
		}
		
		public override string ToString()
		{
			return string.Format("{0} #{1}", Name, ExampleIndex);
		}
	}

	public struct Result
	{
		public Unistroke Match;
		public float Score;
		public float Angle;

		public Result(Unistroke match, float score, float angle)
		{
			Match = match;
			Score = score;
			Angle = angle;
		}

		public static Result None
		{
			get
			{
				return new Result(null, 0, 0);
			}
		}

		public override string ToString()
		{
			return string.Format("{0} @{2} ({1})", Match, Score, Angle);
		}
	}

	public string[] EnumerateGestures()
	{
		List<string> result = new List<string>();

		for (int i = 0; i < _library.Count; i++)
		{
			if (!result.Contains(_library[i].Name))
				result.Add(_library[i].Name);
		}

		return result.ToArray();
	}


	protected const int _kNormalizedPoints = 64;
	protected const float _kNormalizedSize = 256.0f;
	protected const float _kAngleRange = 45.0f * Mathf.Deg2Rad;
	protected const float _kAnglePrecision = 2.0f * Mathf.Deg2Rad;
	protected static readonly float _kDiagonal = (Vector2.one * _kNormalizedSize).magnitude;
	protected static readonly float _kHalfDiagonal = _kDiagonal * 0.5f;

	protected List<Unistroke> _library;
	protected Dictionary<string, List<int>> _libraryIndex;

	public DollarRecognizer()
	{
		_library = new List<Unistroke>();
		_libraryIndex = new Dictionary<string, List<int>>();
	}

	public Unistroke SavePattern(string name, IEnumerable<Vector2> points)
	{
		Unistroke stroke = new Unistroke(name, points);

		int index = _library.Count;
		_library.Add(stroke);

		List<int> examples = null;
		if (_libraryIndex.ContainsKey(stroke.Name))
			examples = _libraryIndex[stroke.Name];
		if (examples == null)
		{
			examples = new List<int>();
			_libraryIndex[stroke.Name] = examples;
		}
		stroke.ExampleIndex = examples.Count;
		examples.Add(index);

		return stroke;
	}

	public Result Recognize(IEnumerable<Vector2> points)
	{
		Vector2[] working = resample(points, _kNormalizedPoints);
		float angle = indicativeAngle(working);
		rotateBy(working, -angle);
		scaleTo(working, _kNormalizedSize);
		translateTo(working, Vector2.zero);

		List<float> v = vectorize(working); // positions of given new drawing

		float bestDist = float.PositiveInfinity;
		int bestIndex = -1;

		for (int i = 0; i < _library.Count; i++)
		{
			float dist = optimalCosineDistance(_library[i].Vector, v);
			if (dist < bestDist)
			{
				bestDist = dist;
				bestIndex = i;
			}
		}

		if (bestIndex < 0) 
			return Result.None;
		else
			return new Result(_library[bestIndex], 1.0f - bestDist, (_library[bestIndex].Angle - angle) * Mathf.Rad2Deg);
	}

	protected static Vector2[] resample(IEnumerable<Vector2> points, int targetCount)
	{
		List<Vector2> result = new List<Vector2>();

		float interval = pathLength(points) / (targetCount - 1);
		float accumulator = 0;

		Vector2 previous = Vector2.zero;

		IEnumerator<Vector2> stepper = points.GetEnumerator();
		bool more = stepper.MoveNext();
		Vector2 point = stepper.Current;
		result.Add(point);
		previous = point;

		while (more)
		{
			Vector2 delta = point - previous;
			float dist = delta.magnitude;
			if ((accumulator + dist) >= interval)
			{
				float span = ((interval - accumulator) / dist);
				Vector2 q = previous + (span * delta);
				result.Add(q);
				previous = q;
				accumulator = 0;
			}
			else
			{
				accumulator += dist;
				previous = point;
				more = stepper.MoveNext();
                if(!more)break;
				point = stepper.Current;
			}
		}
		
		if (result.Count < targetCount)
		{
			// sometimes we fall a rounding-error short of adding the last point, so add it if so
			result.Add(previous);
		}

		return result.ToArray();
	}

	protected static Vector2 centroid(Vector2[] points)
	{
		Vector2 result = Vector2.zero;

		for (int i = 0; i < points.Length; i++)
		{
			result += points[i];
		}

		result = result / (float)points.Length;
		return result;
	}

	protected static float indicativeAngle(Vector2[] points)
	{
		Vector2 delta = centroid(points) - points[0];
		return Mathf.Atan2(delta.y, delta.x);
	}

	protected static void rotateBy(Vector2[] points, float angle)
	{
		Vector2 c = centroid(points);
		float cos = Mathf.Cos(angle);
		float sin = Mathf.Sin(angle);

		for (int i = 0; i < points.Length; i++)
		{
			Vector2 delta = points[i] - c;
			points[i].x = (delta.x * cos) - (delta.y * sin);
			points[i].y = (delta.x * sin) + (delta.y * cos);
			points[i] += c;
		}
	}

	protected static Rect boundingBox(Vector2[] points)
	{
		Rect result = new Rect();
		result.xMin = float.PositiveInfinity;
		result.xMax = float.NegativeInfinity;
		result.yMin = float.PositiveInfinity;
		result.yMax = float.NegativeInfinity;

		for (int i = 0; i < points.Length; i++)
		{
			result.xMin = Mathf.Min(result.xMin, points[i].x);
			result.xMax = Mathf.Max(result.xMax, points[i].x);
			result.yMin = Mathf.Min(result.yMin, points[i].y);
			result.yMax = Mathf.Max(result.yMax, points[i].y);
		}

		return result;
	}

	protected static void scaleTo(Vector2[] points, float normalizedSize)
	{
		Rect bounds = boundingBox(points);
		Vector2 scale = new Vector2(bounds.width, bounds.height) * (1.0f / normalizedSize);
		for (int i = 0; i < points.Length; i++)
		{
			points[i].x = points[i].x * scale.x;
			points[i].y = points[i].y * scale.y;
		}
	}

	protected static void translateTo(Vector2[] points, Vector2 newCentroid)
	{
		Vector2 c = centroid(points);
		Vector2 delta = newCentroid - c;

		for (int i = 0; i < points.Length; i++)
		{
			points[i] = points[i] + delta;
		}
	}

	protected static List<float> vectorize(Vector2[] points)
	{
		float sum = 0;
		List<float> result = new List<float>();

		for (int i = 0; i < points.Length; i++)
		{
			result.Add(points[i].x);
			result.Add(points[i].y);
			sum += points[i].sqrMagnitude;
		}

		float mag = Mathf.Sqrt(sum);
		for (int i = 0; i < result.Count; i++)
		{
			result[i] /= mag;
		}

		return result;
	}

	protected static float optimalCosineDistance(List<float> v1, List<float> v2)
	{
		if (v1.Count != v2.Count)
		{
			return float.NaN;
		}
		
		float a = 0;
		float b = 0;

		for (int i = 0; i < v1.Count; i += 2)
		{
			a += (v1[i] * v2[i]) + (v1[i+1] * v2[i+1]);
			b += (v1[i] * v2[i+1]) - (v1[i+1] * v2[i]);
		}

		float angle = Mathf.Atan(b / a);
		float result = Mathf.Acos((a * Mathf.Cos(angle)) + (b * Mathf.Sin(angle)));
		return result;
	}

	protected static float distanceAtAngle(Vector2[] points, Unistroke test, float angle)
	{
		Vector2[] rotated = new Vector2[points.Length];
		rotateBy(rotated, angle);
		return pathDistance(rotated, test.Points);
	}

	protected static float pathDistance(Vector2[] pts1, Vector2[] pts2)
	{
		if (pts1.Length != pts2.Length)
			return float.NaN;

		float result = 0;
		for (int i = 0; i < pts1.Length; i++)
		{
			result += (pts2[i] - pts1[i]).magnitude; 
		}

		return result / (float)pts1.Length;
	}

	protected static float pathLength(IEnumerable<Vector2> points)
	{
		float result = 0;
		Vector2 previous = Vector2.zero;

		bool first = true;
		foreach (Vector2 point in points)
		{
			if (first)
				first = false;
			else
			{
				result += (point - previous).magnitude;
			}

			previous = point;
		}

		return result;
	}
}