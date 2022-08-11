using Godot;
using Godot.Collections;
using Project.Core;
using Project.Gameplay;
using Project.Gameplay.Triggers;
using Project.Gameplay.Objects;
using Project.Gameplay.Hazards;

namespace Project.Editor
{
	[Tool]
	public class Plugin : EditorPlugin
	{
		public Plugin plugin;

		private Node target;
		private Camera editorCam;

		public override bool Handles(Object obj)
		{
			return obj is DriftTrigger || obj is JumpTrigger || obj is Launcher || obj is FlyingPot || obj is Catapult || obj is Majin || obj is SpikeBall;
		}
		public override void Edit(Object obj) => target = obj as Node;

		public override bool ForwardSpatialGuiInput(Camera cam, InputEvent e)
		{
			editorCam = cam;
			UpdateOverlays();
			return base.ForwardSpatialGuiInput(cam, e);
		}

		private const int PREVIEW_RESOLUTION = 32;

		public override void ForwardSpatialDrawOverViewport(Control overlay)
		{
			if (editorCam == null) return;

			if (target is Launcher)
				DrawLaunchData(overlay, (target as Launcher).GetLaunchData(), DEFAULT_DRAW_COLOR);
			else if (target is JumpTrigger)
				DrawLaunchData(overlay, (target as JumpTrigger).GetLaunchData(), DEFAULT_DRAW_COLOR);
			else if (target is Catapult)
				DrawLaunchData(overlay, (target as Catapult).GetLaunchData(), DEFAULT_DRAW_COLOR.LinearInterpolate(Colors.Red, (target as Catapult).launchPower));
			else if (target is FlyingPot)
				UpdatePot(overlay);
			else if (target is DriftTrigger)
				UpdateDriftCorner(overlay);
			else if (target is SpikeBall)
				UpdateSpikeBall(overlay);
			else if (target is Majin)
				UpdateMajinPath(overlay);
		}

		private readonly Color DEFAULT_DRAW_COLOR = Colors.Blue;
		private void DrawLaunchData(Control overlay, LaunchData launchData, Color overrideColor)
		{
			Array<Vector2> points = new Array<Vector2>();

			for (int i = 0; i < PREVIEW_RESOLUTION; i++)
			{
				float simulationTime = (i / (float)PREVIEW_RESOLUTION) * launchData.TotalTravelTime;
				Vector3 position = launchData.InterpolatePosition(simulationTime);
				if (!editorCam.IsPositionBehind(position))
					points.Add(editorCam.UnprojectPosition(position));
			}

			Vector2[] pointsList = new Vector2[points.Count];
			points.CopyTo(pointsList, 0);
			overlay.DrawPolyline(pointsList, overrideColor, 1, true);
		}

		private void UpdatePot(Control overlay)
		{
			Array<Vector3> points = new Array<Vector3>();
			FlyingPot pot = (target as FlyingPot);

			Vector3 bottomRight = pot.GlobalTranslation + pot.Right() * pot.travelBounds.x;
			Vector3 bottomLeft = pot.GlobalTranslation + pot.Left() * pot.travelBounds.x;
			points.Add(bottomRight);
			points.Add(bottomRight + Vector3.Up * pot.travelBounds.y);
			points.Add(bottomLeft + Vector3.Up * pot.travelBounds.y);
			points.Add(bottomLeft);

			Vector2[] pointsList = new Vector2[points.Count];
			for (int i = 0; i < points.Count; i++)
			{
				if (!editorCam.IsPositionBehind(points[i]))
					pointsList[i] = editorCam.UnprojectPosition(points[i]);
			}
			overlay.DrawPolyline(pointsList, Colors.Blue, 1, true);
		}

		private void UpdateDriftCorner(Control overlay)
		{
			if (editorCam.IsPositionBehind((target as DriftTrigger).GlobalTranslation) ||
			editorCam.IsPositionBehind((target as DriftTrigger).MiddlePosition))
				return;

			Vector2 start = editorCam.UnprojectPosition((target as DriftTrigger).GlobalTranslation);
			Vector2 middle = editorCam.UnprojectPosition((target as DriftTrigger).MiddlePosition);
			overlay.DrawLine(start, middle, Colors.Blue, 1, true);

			if (editorCam.IsPositionBehind((target as DriftTrigger).EndPosition)) return;

			Vector2 end = editorCam.UnprojectPosition((target as DriftTrigger).EndPosition);
			overlay.DrawLine(middle, end, Colors.Blue, 1, true);
		}

		private void UpdateSpikeBall(Control overlay)
		{
			SpikeBall ball = target as SpikeBall;

			if (ball.movementType == SpikeBall.MovementType.Static) return; //Don't draw

			if(ball.movementType == SpikeBall.MovementType.Linear)
			{
				Vector2 start = editorCam.UnprojectPosition(ball.GlobalTranslation);
				Vector2 end = editorCam.UnprojectPosition(ball.GlobalTranslation + ball.Right() * ball.distance);
				overlay.DrawLine(start, end, Colors.Red);
			}
			else
			{
				Array<Vector3> points = new Array<Vector3>();
				for (int i = 0; i < PREVIEW_RESOLUTION; i++)
				{
					float angle = Mathf.Tau * ((float)i / PREVIEW_RESOLUTION);
					points.Add(ball.GlobalTranslation + ball.Forward().Rotated(ball.Up(), angle).Normalized() * ball.distance);
				}

				Vector2 start = editorCam.UnprojectPosition(points[0]);
				Vector2 end = editorCam.UnprojectPosition(points[points.Count - 1]);
				overlay.DrawLine(start, end, Colors.Red);
				for (int i = 1; i < points.Count; i++)
				{
					start = editorCam.UnprojectPosition(points[i - 1]);
					end = editorCam.UnprojectPosition(points[i]);
					overlay.DrawLine(start, end, Colors.Red);
				}
			}
		}

		private void UpdateMajinPath(Control overlay)
		{
			Majin t = target as Majin;
			if (t.spawnOffset == Vector3.Zero) return;

			Vector3 s = t.GlobalTranslation;
			Vector3 e = s + t.spawnOffset;
			if (editorCam.IsPositionBehind(s) ||
			editorCam.IsPositionBehind(e))
				return;

			overlay.DrawLine(editorCam.UnprojectPosition(s), editorCam.UnprojectPosition(e), Colors.Blue, 1, true);
		}
	}
}
