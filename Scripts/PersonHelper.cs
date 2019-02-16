using System;
using Godot;

/*
 * THIS CLASS IS IMPORTANT - DO NOT DELETE
 * Used as a workaround for an engine bug
 * Bug: When an animation calls another node's function that node is locked at (0,0,0) (at least true for the root node)
 */

public class PersonHelper : Spatial
{
    public Person Parent;

    public override void _Ready()
    {
        Parent = (Person)GetNode("..");
    }

    public void CheckPunchHit()
    {
        Parent.CheckPunchHit();
    }

    public void Fire()
    {
        Parent.Fire();
    }

    public void CoolDownOver()
    {
        Parent.CoolDownOver();
    }
}