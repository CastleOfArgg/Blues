using Godot;
using System;

public class Level : Spatial
{
    [Export]
    public string LevelName = "";

    public override void _Ready()
    {
        Globals.Instance.CurrentLevel = this;
        if (LevelName == "")
            GD.Print("LEVEL NAME MISSING!");
    }
}
