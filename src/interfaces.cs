using System;

namespace WorldCustomizer;

/// <summary>
/// This interface signifies that a class has components to be rendered to the screen
/// </summary>
interface IRenderable {
    void Render(IntPtr window, IntPtr renderer);
}
/// <summary>
/// This interface signifies that a class is dynamic and requires an Update function, as well as a way to talk to it's parent
/// </summary>
interface IAmInteractable {
    IAmInteractable Parent {get; set;}
    void Signal(string text);
    void Update();
}