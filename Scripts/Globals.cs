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

    public static Vector3 GetRotationFrom(Vector3 vec, Vector3 source)
    {
        return vec
                .Rotated(Vector3.Right, source.x)
                .Rotated(Vector3.Up, source.y)
                .Rotated(Vector3.Forward, source.z);
    }
}
