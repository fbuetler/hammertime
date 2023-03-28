using System;
using Microsoft.Xna.Framework;

namespace hammered;

public class Camera
{
    public Matrix View
    {
        get { return _view; }
    }
    public Matrix Projection
    {
        get { return _projection; }
    }
    private Matrix _view, _projection;

    public Camera(Vector3 target, float aspectRatio, float mapWidth)
    {
        float fovAngle = 45;
        float near = 0.01f;
        float far = 100f;

        // assume only width matters as maps are usually designed to be wider than deeper
        float height = mapWidth / MathF.Sin(fovAngle);
        float setBackDistance = 10f;
        Vector3 pos = new Vector3(
            target.X,
            target.Y + height,
            target.Z + setBackDistance
        );

        // setup our graphics scene matrices 
        _view = Matrix.CreateLookAt(pos, target, Vector3.Up);
        _projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45), aspectRatio, near, far);
    }
}