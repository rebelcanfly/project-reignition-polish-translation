using Godot;
using System.Collections.Generic;

namespace Project.Core
{
	public partial class DebugManager : Node2D
	{
		public static DebugManager Instance;

		[Export]
		private Control debugMenuRoot;

		private bool isAdvancingFrame;
		private bool IsPaused => GetTree().Paused;

		private enum Properties
		{
			HSpeed,
			VSpeed,
			Grounded,
			Charge,
			PropertyCount
		}

		public override void _EnterTree()
		{
			Instance = this;
			ProcessMode = ProcessModeEnum.Always;

			IsStageCullingEnabled = true;


			if (OS.IsDebugBuild()) // Editor Debug
			{
				UseEditorSkills = true;
				UseDebugSave = true;
				SkipCountdown = true;
			}
		}

		public override void _PhysicsProcess(double _)
		{
			if (!OS.IsDebugBuild()) //Don't do anything in real build
				return;

			if (isAdvancingFrame)
			{
				GetTree().Paused = true;
				isAdvancingFrame = false;
			}

			if (Input.IsActionJustPressed("debug_menu"))
				debugMenuRoot.Visible = !debugMenuRoot.Visible;

			if (Input.IsActionJustPressed("debug_turbo"))
				Engine.TimeScale = 2.5f;
			else if (Input.IsActionJustReleased("debug_turbo"))
				Engine.TimeScale = 1f;

			if (Input.IsActionJustPressed("debug_pause"))
				GetTree().Paused = !IsPaused;

			if (Input.IsActionJustPressed("debug_window_small"))
			{
				SaveManager.Config.screenResolution = 0;
				SaveManager.Config.isFullscreen = false;
				SaveManager.Instance.ApplyConfig();
			}

			if (Input.IsActionJustPressed("debug_window_medium"))
			{
				SaveManager.Config.screenResolution = 3;
				SaveManager.Config.isFullscreen = false;
				SaveManager.Instance.ApplyConfig();
			}

			if (Input.IsActionJustPressed("debug_window_large"))
			{
				SaveManager.Config.screenResolution = 4;
				SaveManager.Config.isFullscreen = true;
				SaveManager.Instance.ApplyConfig();
			}

			if (Input.IsActionJustPressed("debug_step"))
			{
				GetTree().Paused = false;
				isAdvancingFrame = true;
			}

			if (Input.IsActionJustPressed("debug_restart"))
			{
				if (!Input.IsKeyPressed(Key.Shift) && IsInstanceValid(Gameplay.CharacterController.instance))
					Gameplay.CharacterController.instance.StartRespawn();
				else
				{
					TransitionManager.QueueSceneChange(string.Empty);
					TransitionManager.StartTransition(new TransitionData());
				}
			}

			if (line3d.Count + line2d.Count != 0 && !IsPaused) // Queue Raycast Redraw
				QueueRedraw();
		}

		#region Raycast Debug Code
		public override void _Draw()
		{
			if (!IsDebugRaysEnabled)
			{
				line2d.Clear();
				line3d.Clear();
				return;
			}

			for (int i = line2d.Count - 1; i >= 0; i--)
			{
				DrawLine(line2d[i].start, line2d[i].end, line3d[i].color, 1.0f, true);
				line2d.RemoveAt(i);
			}

			Camera3D cam = GetViewport().GetCamera3D();
			if (cam == null)
			{
				line3d.Clear();
				return; //NO CAMERA
			}

			for (int i = line3d.Count - 1; i >= 0; i--)
			{
				if (cam.IsPositionBehind(line3d[i].start) || cam.IsPositionBehind(line3d[i].end))
					continue;

				Vector2 startPos = cam.UnprojectPosition(line3d[i].start);
				Vector2 endPos = cam.UnprojectPosition(line3d[i].end);

				DrawLine(startPos, endPos, line3d[i].color, 1.0f, true);
				line3d.RemoveAt(i);
			}
		}

		public struct Line3D
		{
			public Vector3 start;
			public Vector3 end;
			public Color color;

			public Line3D(Vector3 s, Vector3 e, Color c)
			{
				start = s;
				end = e;
				color = c;
			}
		}

		public struct Line2D
		{
			public Vector2 start;
			public Vector2 end;
			public Color color;

			public Line2D(Vector2 s, Vector2 e, Color c)
			{
				start = s;
				end = e;
				color = c;
			}
		}

		private static readonly List<Line3D> line3d = new List<Line3D>();
		private static readonly List<Line2D> line2d = new List<Line2D>();

		public static void DrawLn(Vector3 s, Vector3 e, Color c) => line3d.Add(new Line3D(s, e, c));
		public static void DrawRay(Vector3 s, Vector3 r, Color c) => line3d.Add(new Line3D(s, s + r, c));

		public static void DrawLn(Vector2 s, Vector2 e, Color c) => line2d.Add(new Line2D(s, e, c));
		public static void DrawRay(Vector2 s, Vector2 r, Color c) => line2d.Add(new Line2D(s, s + r, c));
		#endregion

		#region Debug Cheats
		/// <summary> Draw debug rays? </summary>
		private bool IsDebugRaysEnabled { get; set; }
		public void OnRayToggled(bool enabled) => IsDebugRaysEnabled = enabled;

		[Signal]
		public delegate void StageCullingToggledEventHandler();
		public static bool IsStageCullingEnabled { get; private set; }
		private void OnStageCullingToggled(bool enabled)
		{
			IsStageCullingEnabled = enabled;
			EmitSignal(SignalName.StageCullingToggled);
		}

		/// <summary> Have all worlds/stages unlocked. </summary>
		public bool UnlockAllStages { get; private set; }
		[Signal]
		public delegate void UnlockStagesToggledEventHandler();
		private void OnUnlockStagesToggled(bool enabled)
		{
			UnlockAllStages = enabled;
			EmitSignal(SignalName.UnlockStagesToggled);
		}

		/// <summary> Don't load skills from save data, use inspector values instead. </summary>
		public bool UseEditorSkills { get; private set; }
		/// <summary> Use a custom save. </summary>
		public bool UseDebugSave { get; private set; }
		#endregion

		#region Gameplay Cheats
		/// <summary> Infinite soul gauge. </summary>
		public bool InfiniteSoulGauge { get; private set; }
		private void OnInfiniteSoulToggled(bool enabled) => InfiniteSoulGauge = enabled;
		/// <summary> Infinite rings. </summary>
		public bool InfiniteRings { get; private set; }
		private void OnInfiniteRingsToggled(bool enabled) => InfiniteRings = enabled;
		/// <summary> Skip countdowns for faster debugging. </summary>
		public bool SkipCountdown { get; private set; }
		private void OnSkipCountdownToggled(bool enabled) => SkipCountdown = enabled;
		#endregion
	}
}