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
    //Animations
    public static Dictionary<string, Animation> AttackAnimations = new Dictionary<string, Animation>();
    public static string[] Animations = { "Punching", "Shooting_Pistol" };

    public static Weapon Punch = new Weapon()
    {
        IsMelee = true,
        Damage = 10
    };
    public List<Godot.Object> BodiesInPunchArea = new List<Godot.Object>();
    public Area PunchArea;
    public CollisionShape BodyCollisionShape;

    public float ACCEL = Resources.ACCEL;
    public float DEACCEL = Resources.DEACCEL;
    public Vector3 vel = new Vector3();
    public float WalkSpeed = Resources.WalkSpeed;
    public float RunSpeed = Resources.RunSpeed;
    public bool IsWalking = false;
    public float Pitch = 0;
    public float Yaw = 0;
    public float RotateSpeed = Resources.RotateSpeed;

    //Pickups
    public List<PickupArea> PickupsInArea = new List<PickupArea>();
    public PickupArea ClosestPickup = null;
    public PickupArea.REQ PickupRequirement = PickupArea.REQ.NON_PLAYER_ONLY;

    [Export]
    public STATE State = STATE.IDLE;
    [Export]
    public CIV Civ = CIV.French;
    [Export]
    public int Health = 100;

    public Weapon Weapon = null;
    public RayCast BulletRay;

    [Export]
    public NodePath WaypointPath = null;
    public Spatial WayPointGroup = null;
    public Spatial WayPoint = null;
    
    public Spatial DynamicWaypoint = null;
    public Spatial EnemyBody = null;

    public delegate void AIDelegate();
    private AIDelegate AI = null;
    
    //public AnimationPlayer Anim;
    public AnimationTreePlayer AnimTree;

    //Attacking
    public bool CoolDown = false;

    //Weapon nodes - change through ChangeWeapon()
    private Spatial Pistol;

    public override void _Ready()
    {
        Init();

        if (WaypointPath != "")
            WayPointGroup = (Spatial)GetNode(WaypointPath);
        FindWayPoint();
    }

    public void Init()
    {
        //load animations - do once
        if (AttackAnimations.Count == 0)
            foreach (var anim in Animations)
                AttackAnimations.Add(anim, (Animation)GD.Load(Resources.PersonAnimationPath + anim + ".tres"));

        Pistol = (Spatial)GetNode("Skeleton/HandAttachment/Pistol");

        PunchArea = (Area)GetNode("Skeleton/HandAttachment/Area");
        BodyCollisionShape = (CollisionShape)GetNode("CollisionShape");
        BulletRay = (RayCast)GetNode("BulletRayCast");

        //Anim = (AnimationPlayer)GetNode("AnimationPlayer");
        AnimTree = (AnimationTreePlayer)GetNode("AnimationTreePlayer");
        ChangeState(State);
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

        //makes sure everything is unchanged
        Orthonormalize();
    }

    public virtual void MoveCharacter(float delta, Vector3 Movement)
    {
        Movement = Movement.Normalized();
        vel = Movement;
        vel = MoveAndSlide(vel, new Vector3(0, 1, 0), 0.05f, 4, Mathf.Deg2Rad(Resources.MAX_SLOPE_ANGLE));

        Animate(vel);
    }

    public void Animate(Vector3 vel)
    {
        AnimTree.Blend2NodeSetAmount("Idle_Run", vel.Length() / WalkSpeed);
    }

    public void ChangeWeapon(Weapon newWeapon)
    {
        //hide weapon on person
        if(Weapon != null)
        {
            switch (Weapon.Name)
            {
                case "Pistol":
                    Pistol.Visible = false;
                    break;
            }
        }

        //show weapon on person and set correct animation
        if (newWeapon == null)
        {
            AnimTree.AnimationNodeSetAnimation("Attack", AttackAnimations["Punching"]);
        }
        else
        {
            switch (newWeapon.Name)
            {
                case "Pistol":
                    Pistol.Visible = true;
                    AnimTree.AnimationNodeSetAnimation("Attack", AttackAnimations["Shooting_Pistol"]);
                    break;
            }
        }

        Weapon = newWeapon;
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
                AnimTree.TimeseekNodeSeek("Death_Seek", 0);
                AnimTree.Blend2NodeSetAmount("Alive_Death", 1);
                BodyCollisionShape.SetDisabled(true);
                AI = null;
                break;
            case STATE.PLAYER:
                AI = null;
                break;
        }
    }

    private void AI_ForcedAnimation()
    {
        //TODO
    }

    private void AI_Idle()
    {
        //TODO
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
            //TODO get to a shooting position
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
        if (CoolDown == true)
            return;

        AnimTree.TimeseekNodeSeek("Attack_Seek", 0);
        CoolDown = true;
    }

    public void SecondaryAttack()
    {
        //TODO
    }

    public void Fire()
    {
        Weapon.Attack();
        var other = BulletRay.GetCollider();
        if(other is Person person)
        {
            person.TakeDamage(Weapon.Damage);
        }
        //TODO Add other colliders

        var pos = BulletRay.GetCollisionPoint();
        //TODO blood or something (Particals)
    }

    public void TakeDamage(int damage)
    {
        Health -= damage;
        if (Health <= 0)
            ChangeState(STATE.DEATH);
    }

    public void CoolDownOver()
    {
        CoolDown = false;
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

    public void PickupItem()
    {
        if(ClosestPickup.Requirement == PickupArea.REQ.ALL || ClosestPickup.Requirement == PickupRequirement)
        {
            var obj = ClosestPickup.Obj;
            if (obj is Weapon weapon)
            {
                if(Weapon != null)
                    Weapon.SetVisible(false);

                var temp = (Weapon)GetNode("Skeleton/HandAttachment/" + weapon.Name);
                temp.SetVisible(true);
                temp.Copy(weapon);

                if (Weapon == null)
                {
                    ClosestPickup.QueueFree();
                }
                else
                {
                    weapon.QueueFree();
                    var scene2 = (PackedScene)GD.Load(Resources.ScenePath + Weapon.Name + ".tscn");
                    var sceneInstance2 = scene2.Instance();
                    sceneInstance2.SetName(Weapon.Name);
                    ClosestPickup.AddChild(sceneInstance2);
                }
                
                AnimTree.AnimationNodeSetAnimation("Attack", AttackAnimations["Shooting_" + weapon.Name]);
                Weapon = temp;
            }
        }
    }

    public void CanPickup(PickupArea pickup)
    {
        PickupsInArea.Add(pickup);
    }

    public void CanNoLongerPickup(PickupArea pickup)
    {
        PickupsInArea.Remove(pickup);
    }

    private void _on_Sight_body_entered(Godot.Object body)
    {
        if (State == STATE.PLAYER || State == STATE.DEATH || State == STATE.FORCEDANIMATION)
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





