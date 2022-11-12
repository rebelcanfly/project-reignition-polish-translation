using Godot;
using Project.Core;

namespace Project.Gameplay.Objects
{
	/// <summary>
	/// Launches the player. Use <see cref="CreateLaunchData(Vector3, Vector3, float, bool)"/> to bypass needing a Launcher node.
	/// </summary>
	[Tool]
	public partial class Launcher : Area3D //Similar to Character.JumpTo(), but jumps between static points w/ custom sfx support
	{
		[Signal]
		public delegate void ActivatedEventHandler();

		[Export]
		private float startingHeight; //Height at the beginning of the arc
		[Export]
		private float middleHeight; //Height at the highest point of the arc
		[Export]
		private float finalHeight; //Height at the end of the arc
		[Export]
		private float distance; //How far to travel

		private Vector3 travelDirection; //Direction the player should face when being launched

		public LaunchData GetLaunchData()
		{
			LaunchData data = new LaunchData
			{
				launchDirection = GetLaunchDirection(),

				startPosition = GlobalPosition + Vector3.Up * startingHeight,
				startingHeight = startingHeight,
				middleHeight = middleHeight,
				finalHeight = finalHeight,

				distance = distance,
			};

			data.Calculate();
			return data;
		}

		[Export]
		public bool allowJumpDashing;

		[Export]
		public LaunchDirection launchDirection;
		public enum LaunchDirection
		{
			Forward,
			Up,
		}
		public Vector3 GetLaunchDirection()
		{
			if (launchDirection == LaunchDirection.Forward)
				return this.Forward();

			return this.Up();
		}

		public Vector3 StartingPoint => GlobalPosition + Vector3.Up * startingHeight;

		public virtual void Activate(Area3D a)
		{
			if (sfxPlayer != null)
				sfxPlayer.Play();

			IsCharacterCentered = recenterSpeed == 0;
			LaunchData launchData = GetLaunchData();
			Character.StartLauncher(launchData, this, true);

			EmitSignal(SignalName.Activated);
		}

		[Export]
		private int recenterSpeed; //How fast to recenter the character

		public bool IsCharacterCentered { get; private set; }
		private CharacterController Character => CharacterController.instance;

		public Vector3 RecenterCharacter()
		{
			Vector3 pos = Character.GlobalPosition.MoveToward(StartingPoint, recenterSpeed * PhysicsManager.physicsDelta);
			IsCharacterCentered = pos.IsEqualApprox(StartingPoint);
			return pos;
		}

		[Export]
		private AudioStreamPlayer sfxPlayer;
	}

	public struct LaunchData
	{
		public Vector3 launchDirection;
		public Vector3 startPosition;

		public float distance;
		public float startingHeight;
		public float middleHeight;
		public float finalHeight;

		public Vector3 InitialVelocity { get; private set; }
		public float HorizontalVelocity { get; private set; } //Horizontal velocity remains constant throughout the entire launch
		public float InitialVerticalVelocity { get; private set; }
		public float FinalVerticalVelocity { get; private set; }

		public float FirstHalfTime { get; private set; }
		public float SecondHalfTime { get; private set; }
		public float TotalTravelTime { get; private set; }

		public bool IsLauncherFinished(float t) => t + PhysicsManager.physicsDelta >= TotalTravelTime;
		private float GRAVITY => -RuntimeConstants.GRAVITY; //Use the same gravity as the character controller

		//Get the current position, t -> [0 <-> 1]
		public Vector3 InterpolatePositionRatio(float t) => InterpolatePositionTime(t * TotalTravelTime);
		//Get the current position. t -> current time, in seconds.
		public Vector3 InterpolatePositionTime(float t)
		{
			Vector3 displacement = InitialVelocity * t + Vector3.Up * GRAVITY * t * t / 2f;
			return startPosition + displacement;
		}


		public void Calculate()
		{
			if (middleHeight <= finalHeight || middleHeight < startingHeight) //Ignore middle
				middleHeight = Mathf.Max(startingHeight, finalHeight);

			FirstHalfTime = Mathf.Sqrt((-2 * middleHeight) / GRAVITY);
			SecondHalfTime = Mathf.Sqrt((-2 * (middleHeight - finalHeight)) / GRAVITY);
			TotalTravelTime = FirstHalfTime + SecondHalfTime;

			HorizontalVelocity = distance / TotalTravelTime;
			InitialVerticalVelocity = Mathf.Sqrt(-2 * GRAVITY * (middleHeight - startingHeight));
			FinalVerticalVelocity = GRAVITY * SecondHalfTime;

			InitialVelocity = launchDirection.RemoveVertical().Normalized() * HorizontalVelocity + Vector3.Up * InitialVerticalVelocity;
		}

		/// <summary>
		/// Creates new launch data.
		/// s -> starting position, e -> ending position, h -> height, relativeToEnd -> Is the height relative to the end, or start?
		/// </summary>
		public static LaunchData Create(Vector3 s, Vector3 e, float h, bool relativeToEnd = false)
		{
			Vector3 delta = e - s;
			LaunchData data = new LaunchData()
			{
				startPosition = s,
				launchDirection = delta.Normalized(),

				distance = delta.Flatten().Length(),
				startingHeight = 0f,
				middleHeight = h,
				finalHeight = delta.y,
			};

			if (relativeToEnd)
				data.middleHeight += delta.y;

			data.Calculate();
			return data;
		}
	}
}
