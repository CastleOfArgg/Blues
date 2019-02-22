using Godot;

public static class Resources
{
    public static bool Debugging = true;

    //Paths
    public static string ScenePath = "res://Scenes/";
    public static string MeshPath = "res://Meshes/";
    public static string PersonAnimationPath = MeshPath + "Man_001/Animation/";

    //movement
    public static Vector3 CharacterGravity = new Vector3(0, 0, 0);
    public static float MAX_SLOPE_ANGLE = 40f;
    public static float WaypointDistance = 1f;

    public static float PLAYERACCEL = 4.5f;
    public static float PLAYERDEACCEL = 16;
    public static float PlayerWalkSpeed = 3;
    public static float PlayerRunSpeed = 5;

    public static float ACCEL = 0.1f;
    public static float DEACCEL = 20f;
    public static float WalkSpeed = 2;
    public static float RunSpeed = 4.5f;
    public static float RotateSpeed = 1f;

    public static Vector2 CameraSpeed = new Vector2(2, 1);

    //attacks
    public static float MeleeDistance = .4f;
    public static float TooCloseDistance = 5f;
    public static Vector3 ProjectileOffset = new Vector3(-0.5f, 0, 0);
    public static Vector3 ProjectileImpulse = new Vector3(0, 0, 20);
}