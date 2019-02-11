using Godot;
using System;
using System.Collections.Generic;

public enum STATE
{
    PLAYER,
    FORCEDANIMATION,
    IDLE,
    COMBAT,
    DEATH
}

public enum CIV
{
    French,
    Huran,
}

public class Person : KinematicBody
{
    public static Weapon Punch = new Weapon()
    {
        IsMelee = true,
        Damage = 10
    };
    public Area PunchArea;
    public List<Godot.Object> BodiesInPunchArea = new List<Godot.Object>();

    public float ACCEL = Resources.ACCEL;
    public float DEACCEL = Resources.DEACCEL;
    public Vector3 vel = new Vector3();
    public float WalkSpeed = Resources.WalkSpeed;
    public float RunSpeed = Resources.RunSpeed;
    public bool IsWalking = false;
    public float Pitch = 0;
    public float Yaw = 0;
    public float RotateSpeed = Resources.RotateSpeed;
    public Vector3 InitScale;

    [Export]
    public STATE State = STATE.IDLE;
    [Export]
    public CIV Civ = CIV.French;
    [Export]
    public int Health = 100;

    [Export]
    private NodePath WeaponPath = null;
    public Weapon Weapon = null;

    [Export]
    private NodePath WaypointPath = null;
    public Spatial WayPointGroup = null;
    public Spatial WayPoint = null;
    
    public Spatial DynamicWaypoint = null;
    public Spatial EnemyBody = null;

    public delegate void AIDelegate();
    private AIDelegate AI = null;
    
    public AnimationPlayer Player;

    public override void _Ready()
    {
        InitScale = GetScale();
        PunchArea = (Area)GetNode("Skeleton/HandCollision/Area");

        if(WeaponPath != "")
            Weapon = (Weapon)GetNode(WeaponPath);
        if(WaypointPath != "")
            WayPointGroup = (Spatial)GetNode(WaypointPath);
        FindWayPoint();
        
        Player = (AnimationPlayer)GetNode("AnimationPlayer");
        ChangeState(State);
        Player.SetCurrentAnimation("Idle");
    }

    public override void _PhysicsProcess(float delta)
    {
        if (AI == null)
            return;
        AI();

        //move
        if (WayPoint == null)
        {
            MoveCharacter(delta, new Vector3());
            return;
        }

        //rotate
        RotateObjectLocal(Vector3.Up, Mathf.Pi);
        var rot = GlobalTransform.LookingAt(EnemyBody.GlobalTransform.origin, new Vector3(0, 1, 0)).basis.GetEuler();
        if(Mathf.Abs(Transform.basis.GetEuler().y - rot.y) > Mathf.Pi)
        {
            if (rot.y < 0)
                rot.y += 2 * Mathf.Pi;
            else
                rot.y -= 2 * Mathf.Pi;
        }

        rot = Rotation.LinearInterpolate(rot, delta * RotateSpeed) - Transform.basis.GetEuler();
        rot.y = Mathf.Clamp(rot.y, -1, 1);
        RotateObjectLocal(Vector3.Up, rot.y);
        RotateObjectLocal(Vector3.Up, Mathf.Pi);

        //movement
        var dist = WayPoint.GlobalTransform.origin - GlobalTransform.origin;
        if (Mathf.Sqrt(Mathf.Pow(dist.x, 2) + Mathf.Pow(dist.z, 2)) < Resources.WaypointDistance)
        {
            MoveCharacter(delta, new Vector3());
            return;
        }
        var movement = new Vector3(Mathf.Clamp(dist.x, -1, 1), 0, Mathf.Clamp(dist.z, -1, 1));
        MoveCharacter(delta, movement);

        //make sure everything is unchanged
        Orthonormalize();
        SetScale(InitScale);
    }

    public virtual void MoveCharacter(float delta, Vector3 Movement)
    {
        Movement = Movement.Normalized();
        vel = Movement;
        vel = MoveAndSlide(vel, new Vector3(0, 1, 0), 0.05f, 4, Mathf.Deg2Rad(Resources.MAX_SLOPE_ANGLE));
    }

    public void ChangeState(STATE newState)
    {
        State = newState;
        switch (State)
        {
            case STATE.FORCEDANIMATION:
                AI = AI_ForcedAnimation;
                break;
            case STATE.IDLE:
                AI = AI_Idle;
                break;
            case STATE.COMBAT:
                AI = AI_Combat;
                if (DynamicWaypoint == null)
                {
                    DynamicWaypoint = new Spatial();
                    DynamicWaypoint.Transform = new Transform(DynamicWaypoint.Transform.basis, new Vector3(0, 0, 0));
                    GetTree().GetRoot().AddChild(DynamicWaypoint);
                }
                WayPoint = DynamicWaypoint;
                break;
            case STATE.DEATH:
                Player.Play("Death");
                AI = null;
                break;
            case STATE.PLAYER:
                AI = null;
                break;
        }
    }

    private void AI_ForcedAnimation()
    {
        //no changes
    }

    private void AI_Idle()
    {

    }

    private void AI_Combat()
    {
        //set waypoint
        if(Weapon == null || Weapon.IsMelee)
        {
            //run up to enemy
            Vector3 offset = EnemyBody.GlobalTransform.origin - GlobalTransform.origin;
            if(offset.x != 0)
                offset.x /= Mathf.Abs(offset.x);
            if(offset.y != 0)
                offset.y /= Mathf.Abs(offset.y);
            if(offset.z != 0)
                offset.z /= Mathf.Abs(offset.z);
            WayPoint.Transform = new Transform(EnemyBody.GlobalTransform.basis, EnemyBody.GlobalTransform.origin - offset * Resources.MeleeDistance);
        }
        else
        {
            //get to shooting position
        }

        //if close enough then melee
        if(EnemyBody.Transform.origin.DistanceTo(Transform.origin) < Resources.MeleeDistance)
        {
            MainAttack();
        }
    }

    public void FindWayPoint()
    {
        if (WayPointGroup == null)
            return;
        //TODO make better
        WayPoint = (Spatial)WayPointGroup.GetChildren()[0];
    }

    public void MainAttack()
    {
        if (Weapon == null)
        {
            Punch.Attack();
            Player.Play("Punching");
        }
        else
        {
            Weapon.Attack();
        }
    }

    public void SecondaryAttack()
    {
        Weapon.Bash();
    }

    public void TakeDamage(int damage)
    {
        Health -= damage;
        if (Health <= 0)
            ChangeState(STATE.DEATH);
    }

    public void CheckPunchHit()
    {
        foreach (var body in BodiesInPunchArea)
        {
            if(body is Person person)
            {
                person.TakeDamage(Punch.Damage);
            }
        }
    }

    private void _on_Sight_body_entered(Godot.Object body)
    {
        if (State == STATE.PLAYER)
            return;

        if (body is Person other)
        {
            if (other.Civ != Civ)
            {
                EnemyBody = other;
                ChangeState(STATE.COMBAT);
            }
        }
    }
	
	private void _on_HandArea_body_entered(Godot.Object body)
    {
        BodiesInPunchArea.Add(body);
    }

    private void _on_HandArea_body_exited(Godot.Object body)
    {
        BodiesInPunchArea.Remove(body);
    }
}





