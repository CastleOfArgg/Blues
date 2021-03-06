using Godot;
using System;

public class Player : Person
{
    public Camera Camera;

    public override void _Ready()
    {
        PickupRequirement = PickupArea.REQ.PLAYER_ONLY;

        ACCEL = Resources.PLAYERACCEL;
        DEACCEL = Resources.PLAYERDEACCEL;
        WalkSpeed = Resources.PlayerWalkSpeed;
        RunSpeed = Resources.PlayerRunSpeed;
        Camera = (Camera)GetNode("./Camera");

        State = STATE.PLAYER;

        Input.SetMouseMode(Input.MouseMode.Captured);

        //add if statement if networking
        Camera.SetCurrent(true);

        Init();
    }

    public override void _PhysicsProcess(float delta)
    {
        var Movement = new Vector3(0, 0, 0);

        if (Input.IsActionPressed("Walk_Forward"))
            Movement.x++;
        if (Input.IsActionPressed("Walk_Backward"))
            Movement.x--;
        if (Input.IsActionPressed("Walk_Right"))
            Movement.z--;
        if (Input.IsActionPressed("Walk_Left"))
            Movement.z++;
        MoveCharacter(delta, Movement);

        //set ClosestPickup if any from Pickups (pickups in range)
        var itemDist = -1f;
        ClosestPickup = null;
        foreach (var item in PickupsInArea)
        {
            if (ClosestPickup == null)
            {
                ClosestPickup = item;
                itemDist = item.GlobalTransform.origin.DistanceTo(Transform.origin);
            }
            else
            {
                var temp = item.GlobalTransform.origin.DistanceTo(Transform.origin);
                if (temp < itemDist)
                {
                    ClosestPickup = item;
                    itemDist = temp;
                }
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (Resources.Debugging)
        {
            if (@event.IsActionPressed("Quit"))
                GetTree().Quit();

            if (@event.IsActionPressed("Reload"))
                Globals.Instance.ReloadLevel();

            if (@event.IsActionPressed("change_camera"))
            {
                if (((Camera)GetNode("Camera")).Current)
                {
                    ((Camera)GetTree().GetRoot().GetNode("Root/Camera")).Current = true;
                }
                else
                {
                    ((Camera)GetNode("Camera")).Current = true;
                }
            }

            if (@event.IsActionPressed("ToggleMouse"))
            {
                if (Input.GetMouseMode() == Input.MouseMode.Captured)
                    Input.SetMouseMode(Input.MouseMode.Visible);
                else
                    Input.SetMouseMode(Input.MouseMode.Captured);
            }
        }

        if (@event.IsActionPressed("Interact"))
        {
            if (ClosestPickup != null)
            {
                PickupItem();
            }
        }

        if (@event.IsActionPressed("Attack"))
            DrawWeapon();
        if (@event.IsActionReleased("Attack"))
            MainAttack();
        if (@event.IsActionPressed("Bash"))
            SecondaryAttack();
        
        if (@event.IsActionPressed("Toggle_Run"))
            IsWalking = true;
        if (@event.IsActionReleased("Toggle_Run"))
            IsWalking = false;

        if (@event is InputEventMouseMotion motion)
        {
            Pitch = Mathf.Clamp(Pitch - motion.Relative.y * Resources.CameraSpeed.y, -89, 89);
            Yaw = (Yaw - motion.Relative.x * Resources.CameraSpeed.x) % 360;
            Vector3 rot = new Vector3
            {
                x = Mathf.Deg2Rad(-Pitch),
                y = Mathf.Deg2Rad(Yaw)
            };
            Rotation = rot;
        }
    }

    public override void MoveCharacter(float delta, Vector3 Movement)
    {
        Movement = Movement.Normalized();

        var pos = Transform.basis.z.Normalized() * Movement.x;
        pos += Transform.basis.x.Normalized() * Movement.z;

        pos.y = 0;
        pos = pos.Normalized();

        var hvel = vel;
        hvel += Resources.CharacterGravity;

        var speed = RunSpeed;
        if (IsWalking)
            speed = WalkSpeed;

        var accel = DEACCEL;
        if (pos.Dot(hvel) > 0)
            accel = ACCEL;

        hvel = hvel.LinearInterpolate(pos * speed, accel * delta);
        vel.x = hvel.x;
        vel.y = hvel.y;
        vel.z = hvel.z;
        
        vel = MoveAndSlide(vel, new Vector3(0, 1, 0), false, 4, Mathf.Deg2Rad(Resources.MAX_SLOPE_ANGLE));

        Animate(vel);
    }
}
