using Godot;
using System;

public partial class FlyCamera : Camera3D
{
	[Export]
	public float MoveSpeed = 10.0f;
	[Export]
	public float LookSensitivity = 0.01f;
	[Export]
	public float MaxLookAngle = 85.0f;

	private Vector3 _velocity = new Vector3();
	private Vector3 _rotation = new Vector3();

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _PhysicsProcess(double delta)
	{
		HandleInput(delta);
		UpdateRotation(delta);
		MoveAndSlide();
		SwitchMouse();
	}

	private void SwitchMouse()
	{
		if (Input.IsActionJustPressed("switch_mouse"))
		{
			if (Input.MouseMode == Input.MouseModeEnum.Captured)
			{
				Input.MouseMode = Input.MouseModeEnum.Visible;
			} else
			{
				Input.MouseMode = Input.MouseModeEnum.Captured;
			}
		}
	}

	private void HandleInput(double delta)
	{
		_velocity = Vector3.Zero;

		Vector3 forwardDirection = Transform.Basis.Z;
		forwardDirection.Y = 0;  // Ignore the vertical component
		forwardDirection = forwardDirection.Normalized();

		if (Input.IsActionPressed("move_forward"))
			_velocity -= forwardDirection;
		if (Input.IsActionPressed("move_backward"))
			_velocity += forwardDirection;
		if (Input.IsActionPressed("move_left"))
			_velocity -= Transform.Basis.X;
		if (Input.IsActionPressed("move_right"))
			_velocity += Transform.Basis.X;
		if (Input.IsActionPressed("move_up"))
			_velocity += Vector3.Up;
		if (Input.IsActionPressed("move_down"))
			_velocity -= Vector3.Up;

		_velocity = _velocity.Normalized() * MoveSpeed * (float)delta;
	}

	private void UpdateRotation(double delta)
	{
		if (Input.MouseMode != Input.MouseModeEnum.Captured) return;
			var mouseMovement = Input.GetLastMouseVelocity();
		_rotation.X -= mouseMovement.Y * LookSensitivity * (float)delta;
		_rotation.Y -= mouseMovement.X * LookSensitivity * (float)delta;

		// Clamp the pitch to prevent flipping over
		_rotation.X = Mathf.Clamp(_rotation.X, -Mathf.DegToRad(MaxLookAngle), Mathf.DegToRad(MaxLookAngle));

		Rotation = new Vector3(_rotation.X, _rotation.Y, 0);
	}

	private void MoveAndSlide()
	{
		GlobalTransform = GlobalTransform.Translated(_velocity);
	}
}