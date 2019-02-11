using Godot;
using System;

public class Weapon : MeshInstance
{
    [Export]
    public bool IsMelee = true;
    [Export]
    public int Ammo = 0;
    [Export]
    public bool IsLoaded = true;
    [Export]
    public int Damage = 100;

    AnimationPlayer Anim;

    public override void _Ready()
    {
        Anim = (AnimationPlayer)GetNode("AnimationPlayer");
    }

    public void Attack()
    {
        if (IsMelee)
        {
            Swing();
        }
        else
        {
            Shoot();
        }
    }

    public void Bash()
    {

    }

    public void Shoot()
    {
        IsLoaded = false;
    }

    public void Swing()
    {

    }
}
