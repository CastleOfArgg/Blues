using Godot;
using System;

public class Weapon : Spatial
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
            Anim.Play("Fire");
            //TODO add fire (particals)
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

    public void Copy(Weapon other)
    {
        IsMelee = other.IsMelee;
        Ammo = other.Ammo;
        IsLoaded = other.IsLoaded;
        Damage = other.Damage;
}
}
