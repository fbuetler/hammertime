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
        float depth = GetIntersectionDepth(rect);
        return 0 <= depth && depth <= Radius;
    }

    // calculates the signed depth of intersection between a circle and a rectangle
    // 
    // the amount of overlap between two intersecting shapes (at max the diameter of the circle)
    // if the shapes are not intersecting, a negative number is returned
    public float GetIntersectionDepth(Rectangle rect)
    {
        Vector2 v = new Vector2(MathHelper.Clamp(Center.X, rect.Left, rect.Right),
                                MathHelper.Clamp(Center.Y, rect.Top, rect.Bottom));

        Vector2 direction = Center - v;
        float distance = direction.Length();

        if (0 <= distance && distance <= Radius)
        {
            return 2 * distance;
        }

        return -1;
    }
}