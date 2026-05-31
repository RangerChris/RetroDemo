namespace RetroDemo.Scenes;

/// <summary>Represents a demo scene that can be updated and drawn.</summary>
public interface IScene : IDisposable
{
    /// <summary>
    /// Update scene logic. Returns true when the scene has finished and the next
    /// scene should start.
    /// </summary>
    bool Update(float deltaTime);

    /// <summary>Draw the scene. Must be called between BeginDrawing/EndDrawing.</summary>
    void Draw();
}
