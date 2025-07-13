using System;
using System.Numerics;
using SDL2;

#nullable enable
namespace WorldCustomizer;

internal abstract class GenericUIElement {
    internal Vector2 _position;
    internal Vector2 Position { get { return _position + (parent != null ? parent._position : Vector2.Zero); } set { _position = value; } }
    internal Vector2 size;
    internal GenericUIElement? parent;
    internal GenericUIElement(Vector2 position, Vector2 size, GenericUIElement? parent) {
        this.Position = position;
        this.size = size;
        this.parent = parent;
    }
    GenericUIElement GetTopLevelParent() {
        if (parent == null) {
            return this;
        }
        else {
            return parent.GetTopLevelParent();
        }
    }
    internal abstract Window GetParentWindow();
}
internal abstract class Draggable : GenericUIElement, IRenderable, IAmInteractable {
    internal Vector2? mouseOffset;
    const int Handle_Height = 15;
    internal Draggable(Vector2 position, Vector2 size, GenericUIElement parent) : base (position, size, parent) {
        mouseOffset = null;
    }
    internal override Window GetParentWindow() {
        return parent.GetParentWindow();
    }
    public virtual void Render(IntPtr window, IntPtr renderer) {
        var r = new SDL.SDL_FRect(){x=Position.X, y=Position.Y, w=size.X, h=Handle_Height};
        SDL.SDL_SetRenderDrawColor(renderer, 82, 82, 82, 255);
        SDL.SDL_RenderFillRectF(renderer, ref r);
    }
    public virtual void Signal(string text) {
    }
    public virtual void Update() {
        SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
        if (mouseOffset == null && GetParentWindow().parentProgram.clicked && mouseX > Position.X && mouseX < Position.X+size.X && mouseY > Position.Y && mouseY < Position.Y+Handle_Height) {
            mouseOffset = new Vector2(mouseX, mouseY) - Position;
        }
        if (mouseOffset != null && !GetParentWindow().parentProgram.mouseDown) {
            mouseOffset = null;
        }
        if (mouseOffset != null) {
            Position = new Vector2(mouseX, mouseY) - (Vector2)mouseOffset;
        }
    }
}