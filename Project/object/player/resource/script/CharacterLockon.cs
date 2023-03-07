using Godot;
using Godot.Collections;
using Project.Core;

namespace Project.Gameplay
{
	/// <summary>
	/// Responsible for figuring out which target to lock onto.
	/// Also contains the code for bouncing off stuff when using the homing attack.
	/// </summary>
	public partial class CharacterLockon : Node3D
	{
		private CharacterController Character => CharacterController.instance;

		/// <summary> Active lockon target shown on the HUD. </summary>
		public Node3D Target
		{
			get => target;
			private set
			{
				target = value;
				wasTargetChanged = true;
			}
		}
		private Node3D target;
		/// <summary> Was lockonTarget changed this frame? </summary>
		private bool wasTargetChanged;
		private readonly Array<Node3D> activeTargets = new Array<Node3D>(); //List of targetable objects

		/// <summary> Enables detection of new lockonTargets. </summary>
		public bool IsMonitoring { get; set; }

		public bool IsHomingAttacking { get; set; }
		public bool IsPerfectHomingAttack { get; private set; }
		private bool monitoringPerfectHomingAttack;
		public void EnablePerfectHomingAttack() => monitoringPerfectHomingAttack = true;
		public void DisablePerfectHomingAttack() => monitoringPerfectHomingAttack = false;
		public Vector3 HomingAttackDirection => Target != null ? (Target.GlobalPosition - GlobalPosition).Normalized() : this.Forward();

		public void StartHomingAttack()
		{
			IsHomingAttacking = true;
			IsPerfectHomingAttack = monitoringPerfectHomingAttack;
			if (IsPerfectHomingAttack)
				LevelSettings.instance.AddBonus(LevelSettings.BonusType.PerfectHomingAttack);
		}

		public void StopHomingAttack()
		{
			IsHomingAttacking = false;
			Character.ResetActionState();
			ResetLockonTarget();
		}

		public void UpdateLockonTargets()
		{
			wasTargetChanged = false;
			GlobalRotation = Vector3.Up * Character.PathFollower.ForwardAngle;

			if (IsMonitoring)
			{
				int currentTarget = -1; //Index of the current target
				float closestDistance = Mathf.Inf; //Current closest target
				if (Target != null && Target.IsInsideTree()) //Current lockon target starts as the closest target
					closestDistance = Target.GlobalPosition.Flatten().DistanceSquaredTo(Character.GlobalPosition.Flatten());

				//Check whether to pick a new target
				for (int i = 0; i < activeTargets.Count; i++)
				{
					if (IsTargetValid(activeTargets[i]) != TargetState.Valid)
						continue;

					float dst = activeTargets[i].GlobalPosition.Flatten().DistanceSquaredTo(Character.GlobalPosition.Flatten());
					if (dst > closestDistance)
						continue;

					//Update data
					closestDistance = dst;
					currentTarget = i;
				}

				if (currentTarget != -1 && activeTargets[currentTarget] != Target) //Target has changed
					Target = activeTargets[currentTarget];
				else if (Target != null && IsTargetValid(Target) != TargetState.Valid) //Validate current lockon target
					Target = null;
			}
			else if (IsHomingAttacking) //Validate homing attack target
			{
				TargetState state = IsTargetValid(Target);
				if (state == TargetState.NotInList)
					Target = null;
			}

			if (Target != null)
			{
				Vector2 screenPos = Character.Camera.ConvertToScreenSpace(Target.GlobalPosition);
				UpdateLockonReticle(screenPos, wasTargetChanged);
			}
			else if (wasTargetChanged) //Disable UI
				DisableLockonReticle();
		}

		private enum TargetState
		{
			Valid,
			NotInList,
			PlayerBusy,
			Invisible,
			HitObstacle,
		}

		private TargetState IsTargetValid(Node3D t)
		{
			if (!activeTargets.Contains(t)) //Not in target list anymore (target hitbox may have been disabled)
				return TargetState.NotInList;

			if (Character.ActionState == CharacterController.ActionStates.Damaged || IsBouncing) //Character is busy
				return TargetState.PlayerBusy;

			if (!t.IsVisibleInTree() || !Character.Camera.IsOnScreen(t.GlobalPosition)) //Not visible
				return TargetState.Invisible;

			//Raycast for obstacles
			Vector3 castPosition = Character.GlobalPosition;
			if (Character.VerticalSpeed < 0)
				castPosition += Character.UpDirection * Character.VerticalSpeed * PhysicsManager.physicsDelta;
			Vector3 castVector = t.GlobalPosition - castPosition;
			RaycastHit h = this.CastRay(castPosition, castVector, Runtime.Instance.environmentMask);
			Debug.DrawRay(castPosition, castVector, Colors.Magenta);

			if (h && h.collidedObject != t)
				return TargetState.HitObstacle;

			return TargetState.Valid;
		}

		public void ResetLockonTarget()
		{
			Character.Camera.LockonTarget = null;

			IsHomingAttacking = false;
			IsPerfectHomingAttack = false;

			if (Target != null) //Reset Active Target
			{
				Target = null;
				DisableLockonReticle();
			}
		}

		#region Bouncing
		[Export]
		public LockoutResource bounceLockoutSettings;
		[Export]
		public float bounceSpeed;
		[Export]
		public float bouncePower;

		private float bounceTimer;
		public bool IsBouncing => bounceTimer != 0;
		private const float BOUNCE_LOCKOUT_TIME = .15f;

		public void UpdateBounce()
		{
			bounceTimer = Mathf.MoveToward(bounceTimer, 0, PhysicsManager.physicsDelta);

			Character.MoveSpeed = Mathf.MoveToward(Character.MoveSpeed, 0f, Character.GroundSettings.friction * PhysicsManager.physicsDelta);
			Character.VerticalSpeed -= Runtime.GRAVITY * PhysicsManager.physicsDelta;
		}

		public void StartBounce() //Bounce the character up and back (So they can target an enemy again)
		{
			IsHomingAttacking = false;
			bounceTimer = BOUNCE_LOCKOUT_TIME;

			/*
			if (Target != null)
				Character.GlobalPosition = Target.GlobalPosition;
			*/
			ResetLockonTarget();

			Character.CanJumpDash = true;
			Character.MoveSpeed = bounceSpeed;
			Character.VerticalSpeed = bouncePower;
			Character.AddLockoutData(bounceLockoutSettings);
			Character.ResetActionState();
		}
		#endregion

		#region Homing Attack Reticle
		[Export]
		private Node2D lockonReticle;
		[Export]
		private AnimationPlayer lockonAnimator;

		public void DisableLockonReticle() => lockonAnimator.Play("disable");
		public void UpdateLockonReticle(Vector2 screenPosition, bool newTarget)
		{
			lockonReticle.SetDeferred("position", screenPosition);
			if (newTarget)
			{
				lockonAnimator.Play("RESET");
				lockonAnimator.Advance(0);
				lockonAnimator.Play("enable");
			}
		}

		public void PerfectHomingAttack()
		{
			//TODO Play animation
		}
		#endregion


		//Targeting areas on the lockon layer
		public void OnTargetTriggerEnter(Area3D area)
		{
			if (!activeTargets.Contains(area))
				activeTargets.Add(area);
		}

		public void OnTargetTriggerExit(Area3D area)
		{
			if (activeTargets.Contains(area))
				activeTargets.Remove(area);
		}

		//Allow targeting physics bodies as well...
		public void OnTargetBodyEnter(PhysicsBody3D body)
		{
			if (!activeTargets.Contains(body))
				activeTargets.Add(body);
		}

		public void OnTargetBodyExit(PhysicsBody3D body)
		{
			if (activeTargets.Contains(body))
				activeTargets.Remove(body);
		}
	}
}
