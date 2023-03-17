using System;
using Microsoft.Xna.Framework;

namespace hammered;

public class Camera
{
    public Matrix WorldMatrix
    {
        get { return _worldMatrix; }
    }
    public Matrix ViewMatrix
    {
        get { return _viewMatrix; }
    }
    public Matrix ProjectionMatrix
    {
        get { return _projectionMatrix; }
    }
    private Matrix _worldMatrix, _viewMatrix, _projectionMatrix;

    public Camera(Vector3 target, float aspectRatio, float mapWidth)
    {
        float fovAngle = 45;
        float near = 0.01f;
        float far = 30f;

        // assume only width matters as maps are usually designed to be wider than deeper
        float height = mapWidth / (float)Math.Sin(fovAngle) * 0.8f;
        float setBackDistance = 10f;
        Vector3 pos = new Vector3(
            target.X,
            target.Y + height,
            target.Z + setBackDistance
        );

        // setup our graphics scene matrices 
        _worldMatrix = Matrix.Identity;
        _viewMatrix = Matrix.CreateLookAt(pos, target, Vector3.Up);
        _projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45), aspectRatio, near, far);
    }
}