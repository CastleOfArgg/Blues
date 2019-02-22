using Godot;
using System;

public class Projectile : RigidBody
{
    public enum Type
    {
        None,
        Arrow
    }
    public Type Obj = Type.Arrow;
    [Export]
    public bool HasCollision = true;
    [Export]
    public int Damage = 50;

    public Spatial Firer = null;

    //used to make sure not to hit the fireing actor
    public bool IsTooClose = false;

    private void _on_PointArea_body_entered(object body)
    {
        if (!HasCollision || body == this)
            return;

        var b = (Spatial)body;
        
        PutInObject(b);
        QueueFree();
    }

    public void PutInObject(Spatial body)
    {
        //put projectile in object
        var scene2 = (PackedScene)GD.Load(Resources.ScenePath + Obj.ToString() + ".tscn");
        var sceneInstance2 = (Projectile)scene2.Instance();
        var scale = sceneInstance2.Scale;
        body.AddChild(sceneInstance2);
        sceneInstance2.GlobalTransform = GlobalTransform;
        sceneInstance2.HasCollision = false;
        sceneInstance2.GravityScale = 0;
    }
}






