using Godot;
using System;

public class Utils : Node
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        
    }

    public static bool LessX(Vector2 v1, Vector2 v2)
    {
        return v1.x < v2.x;
    }

    public static bool LessY(Vector2 v1, Vector2 v2)
    {
        return v1.y < v2.y;
    }

    public static Vector2 IndexToXY(int width, int index)
    {
        return new Vector2(index % width, index / width);
    }
}
