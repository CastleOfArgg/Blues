using System;
using Godot;

public class Globals : Node
{
    public static Globals Instance;

    public Level CurrentLevel = null;

    public override void _Ready()
    {
        Instance = this;
    }

    public void SetLevel(Level level)
    {
        CurrentLevel = level;
    }

    public void ReloadLevel()
    {
        GetTree().ChangeScene(Resources.ScenePath + "Root" + ".tscn");
    }
}
