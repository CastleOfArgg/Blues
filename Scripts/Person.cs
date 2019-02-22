using Godot;
using System;
using System.Collections.Generic;

public class Person : KinematicBody
{
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
    public NodePath WaypointPath = "";
    public Spatial WayPointGroup = null;
    public Spatial WayPoint = null;
    
    public Spatial DynamicWaypoint = null;
    public Spatial EnemyBody = null;

    public delegate void AIDelegate();
    private AIDelegate AI = null;
    
    public AnimationTree AnimTree;
    public AnimationNodeStateMachinePlayback StateMachine;
    //public RayCast LeftFootRayCast;
    //public RayCast RightFootRayCast;

    //Attacking
    public bool CoolDown = false;

    //Weapon nodes - change through ChangeWeapon()
    private Spatial Bow;
    private Spatial Arrow;
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
        Bow = (Spatial)GetNode("Skeleton/LeftHandAttachment/Bow");
        Arrow = (Spatial)GetNode("Skeleton/RightHandAttachment/Arrow");
        Pistol = (Spatial)GetNode("Skeleton/RightHandAttachment/Pistol");

        PunchArea = (Area)GetNode("Skeleton/RightHandAttachment/Area");
        BodyCollisionShape = (CollisionShape)GetNode("CollisionShape");
        BulletRay = (RayCast)GetNode("BulletRayCast");
        
        AnimTree = (AnimationTree)GetNode("AnimationTree");
        StateMachine = (AnimationNodeStateMachinePlayback)AnimTree.Get("parameters/playback");
        ChangeState(State);

        //LeftFootRayCast = (RayCast)GetNode("Skeleton/LeftFootAttachment/RayCast");
        //RightFootRayCast = (RayCast)GetNode("Skeleton/RightFootAttachment/RayCast");
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

        AnimTree.Advance(delta);
    }

    public virtual void MoveCharacter(float delta, Vector3 Movement)
    {
        Movement = Movement.Normalized();
        vel = Movement;
        vel = MoveAndSlide(vel, Resources.CharacterGravity, false, 4, Mathf.Deg2Rad(Resources.MAX_SLOPE_ANGLE));

        Animate(vel);
    }

    public void Animate(Vector3 vel)
    {
        AnimTree.Set("parameters/Idle_Walk/Idle_Walk/blend_position", new Vector2(vel.z, vel.x));
    }

    public void ChangeWeapon(Weapon newWeapon)
    {
        //hide weapon on person
        if (Weapon != null)
        {
            AnimTree.Set("parameters/Idle_Walk/Move_Draw/blend_amount", 0);
            AnimTree.Set("parameters/Idle_Walk/Draw_Seek/seek_position", 100);
            Weapon.IsDrawn = true;

            if (Weapon.ProjectileType != Projectile.Type.None)
                ShowProjectile(false);

            switch (Weapon.Name)
            {
                case "Bow":
                    Bow.Visible = false;
                    break;
                case "Pistol":
                    Pistol.Visible = false;
                    break;
            }
        }

        //show weapon on person and set correct animation
        if (newWeapon == null)
        {
            AnimTree.Set("parameters/Idle_Walk/Punch_Shoot/blend_amount", 0);
        }
        else
        {
            AnimTree.Set("parameters/Idle_Walk/Punch_Shoot/blend_amount", 1);
            AnimTree.Set("parameters/Idle_Walk/Attack_Seek/seek_position", 100);
            switch (newWeapon.Name)
            {
                case "Bow":
                    AnimTree.Set("parameters/Idle_Walk/Pistol_Bow/blend_amount", 1);
                    Bow.Visible = true;
                    break;
                case "Pistol":
                    AnimTree.Set("parameters/Idle_Walk/Pistol_Bow/blend_amount", 0);
                    Pistol.Visible = true;
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
                StateMachine.Travel("Death");
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

    public void DrawWeapon()
    {
        if (CoolDown)
            return;


        AnimTree.Set("parameters/Idle_Walk/Move_Attack/blend_amount", 1);
        if (Weapon != null && Weapon.MustDraw)
        {
            AnimTree.Set("parameters/Idle_Walk/Move_Draw/blend_amount", 1);
            AnimTree.Set("parameters/Idle_Walk/Draw_Seek/seek_position", 0);
        }
    }

    public void ShowProjectile(bool vis)
    {
        switch (Weapon.ProjectileType)
        {
            case Projectile.Type.Arrow:
                Arrow.Visible = vis;
                break;
        }
    }

    public void MainAttack()
    {
        if (CoolDown || (Weapon != null && !Weapon.IsDrawn))
            return;


        AnimTree.Set("parameters/Idle_Walk/Move_Draw/blend_amount", 0);
        AnimTree.Set("parameters/Idle_Walk/Attack_Seek/seek_position", 0);
        CoolDown = true;
    }

    public void SecondaryAttack()
    {
        //TODO
    }

    public void Fire()
    {
        Weapon.Attack();
        if (Weapon.ProjectileType != Projectile.Type.None)
        {
            //put projectile in game
            var scene2 = (PackedScene)GD.Load(Resources.ScenePath + Weapon.ProjectileType.ToString() + ".tscn");
            var sceneInstance2 = (Projectile)scene2.Instance();
            var scale = sceneInstance2.Scale;
            Globals.Instance.CurrentLevel.GetNode("Projectiles").AddChild(sceneInstance2);
            var offset = Globals.GetRotationFrom(Resources.ProjectileOffset, BulletRay.GlobalTransform.basis.GetEuler());
            sceneInstance2.Transform = new Transform(BulletRay.GlobalTransform.basis, BulletRay.GlobalTransform.origin + offset);

            //
            Vector3 forceSource = BulletRay.GlobalTransform.basis.GetEuler();
            var bowPosition = new Vector3(sceneInstance2.GlobalTransform.origin) { y = 0 };
            var hitPosition = Globals.GetRotationFrom(BulletRay.CastTo, BulletRay.GlobalTransform.basis.GetEuler());
            if (BulletRay.IsColliding())
                hitPosition = new Vector3(BulletRay.GetCollisionPoint()) { y = 0 };
            forceSource.y = Mathf.Atan2(-(bowPosition.x - hitPosition.x), -(bowPosition.z - hitPosition.z));

            //apply force to projectile
            var force = Globals.GetRotationFrom(Resources.ProjectileImpulse, forceSource);
            sceneInstance2.ApplyCentralImpulse(force);
            sceneInstance2.Scale = scale * 10;
            sceneInstance2.Damage = Weapon.Damage;
            sceneInstance2.Firer = this;
            GD.Print(BulletRay.GetCollisionPoint().DistanceTo(GlobalTransform.origin));
            if (BulletRay.GetCollisionPoint().DistanceTo(GlobalTransform.origin) <= Resources.TooCloseDistance)
                sceneInstance2.IsTooClose = true;
            return;
        }

        var other = (Spatial)BulletRay.GetCollider();
        if (other == null)
            return;

        if (other.GetParent().GetParent().GetParent() is Person person)
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
        List<System.Object> alreadyHit = new List<object>();
        foreach (var body in BodiesInPunchArea)
        {
            if(((Spatial)body).GetParent().GetParent().GetParent() is Person person && !alreadyHit.Contains(person))
            {
                person.TakeDamage(Punch.Damage);
                alreadyHit.Add(person);
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

                Weapon temp;
                if(weapon.MustDraw)
                    temp = (Weapon)GetNode("Skeleton/LeftHandAttachment/" + weapon.Name);
                else
                    temp = (Weapon)GetNode("Skeleton/RightHandAttachment/" + weapon.Name);
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
                    ClosestPickup.Reset();
                    ClosestPickup.Obj.Scale = Weapon.Scale * 10;
                }

                ChangeWeapon(temp);
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
	
	private void _on_HandArea_area_entered(Godot.Object body)
    {
        BodiesInPunchArea.Add(body);
    }

    private void _on_HandArea_area_exited(Godot.Object body)
    {
        BodiesInPunchArea.Remove(body);
    }

    private void _on_Area_area_entered(object bodyObj, string bone)
    {
        var body = (Spatial)bodyObj;
        
        if (body.GetParent() is Projectile proj && proj.HasCollision)
        {
            if (proj.IsTooClose && proj.Firer == this)
                return;
            GD.Print(proj.IsTooClose);
            GD.Print(proj.Firer);
            GD.Print(this);
            proj.PutInObject((Spatial)GetNode("Skeleton").GetNode(bone + "Attachment").GetNode("Area"));
            TakeDamage(proj.Damage);
            proj.QueueFree();
        }
    }
}

