using Godot;
using System;

public class PickupArea : Area
{
    public enum REQ
    {
        ALL,
        NONE,
        PLAYER_ONLY,
        NON_PLAYER_ONLY
    };
    [Export]
    public REQ Requirement = REQ.ALL;

    public Spatial Obj = null;

    public override void _Ready()
    {
        Obj = (Spatial)GetChild(GetChildCount() - 1);
    }

    private void _on_PickupArea_body_entered(Godot.Object body)
    {
        if(body is Person person)
        {
            person.CanPickup(this);
        }
    }


    private void _on_PickupArea_body_exited(Godot.Object body)
    {
        if (body is Person person)
        {
            person.CanNoLongerPickup(this);
        }
    }
}
