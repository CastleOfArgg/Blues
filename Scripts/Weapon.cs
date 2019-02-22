using Godot;
using System;

public class Weapon : Spatial
{
    [Export]
    public bool IsMelee = true;
    [Export]
    public int ClipAmount = 6;
    [Export]
    public int Ammo = 0;
    [Export]
    public bool IsLoaded = true;
    [Export]
    public int Damage = 100;
    [Export]
    public bool MustDraw = false;
    public bool IsDrawn = true;

    [Export]
    public Projectile.Type ProjectileType = Projectile.Type.None;

    AnimationPlayer Anim;

    public override void _Ready()
    {
        Anim = (AnimationPlayer)GetNode("AnimationPlayer");
        if (MustDraw)
            IsDrawn = false;
    }

    public void Attack()
    {
        if (MustDraw && !IsDrawn)
            return;
        if (IsMelee)
        {
            Swing();
        }
        else
        {
            Anim.Play("Fire");
            //TODO add particals
        }
    }

    public void Bash()
    {

    }

    public void Shoot()
    {
        Ammo--;
        if(Ammo == 0)
            IsLoaded = false;
    }

    public void Reload()
    {
        Ammo = ClipAmount;
        IsLoaded = true;
    }

    public void SetAmmo()
    {
        Ammo = ClipAmount;
    }

    public void SetAmmo(int amount)
    {
        Ammo = amount;
    }

    public void Draw()
    {
        if (IsDrawn)
            return;
        Anim.Play("Draw");
    }

    public void Swing()
    {

    }

    public void SetDrawn(bool isDrawn)
    {
        IsDrawn = isDrawn;
    }

    public void Copy(Weapon other)
    {
        IsMelee = other.IsMelee;
        Ammo = other.Ammo;
        IsLoaded = other.IsLoaded;
        Damage = other.Damage;
}
}
