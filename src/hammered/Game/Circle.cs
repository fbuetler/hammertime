using Microsoft.Xna.Framework;

namespace hammered;

public class Circle
{
    public Vector2 Center;

    public float Radius;

    public Circle(Vector2 position, float radius)
    {
        Center = position;
        Radius = radius;
    }

    public bool Intersects(Rectangle rect)
    {
        return GetIntersectionDepth(rect).Item2;
    }

    // calculates the depth of intersection between a circle and a rectangle
    // 
    // returns the amount of overlap between two intersecting shapes 
    // these depth values can be negative depending on which wides the shapes 
    // intersect. This allows callers to determine the correct direction
    // to push objects in order to resolve collisions.
    // If the shapes are not intersecting, Vector2.Zero is returned.
    public (Vector2, bool) GetIntersectionDepth(Rectangle rect)
    {
        Vector2 v = new Vector2(MathHelper.Clamp(Center.X, rect.Left, rect.Right),
                                MathHelper.Clamp(Center.Y, rect.Top, rect.Bottom));

        Vector2 direction = Center - v;
        float distanceSquared = direction.LengthSquared();

        if (distanceSquared <= Radius * Radius)
        {
            return (direction, true);
        }

        return (Vector2.Zero, false);
    }
}