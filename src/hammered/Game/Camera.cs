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

    public Camera(Vector3 pos, Vector3 target, float aspectRatio)
    {
        float fovAngle = MathHelper.ToRadians(45);
        float near = 0.01f;
        float far = 30f;

        _worldMatrix = Matrix.Identity;
        _viewMatrix = Matrix.CreateLookAt(pos, target, Vector3.Up);
        _projectionMatrix = Matrix.CreatePerspectiveFieldOfView(fovAngle, aspectRatio, near, far);
    }
}