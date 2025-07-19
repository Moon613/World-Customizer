using System;
using System.Numerics;
using SDL2;

#nullable enable
namespace WorldCustomizer;

class WorldRenderer : GenericUIElement, IRenderable, IAmInteractable {
    float zoom;
    bool dragged;
    Vector2 relativeToMouse;
    private WorldData? WorldData { get { return GetParentWindow().parentProgram.currentWorld; }}
    public WorldRenderer(Vector2 position, Vector2 size, GenericUIElement parent) : base(position, size, parent) {
        zoom = 2;
        dragged = false;
    }
    public void Render(IntPtr window, IntPtr renderer) {
        if (WorldData != null) {
            foreach (RoomData room in WorldData.roomData) {
                RenderRoom(renderer, room);
            }
        }
    }
    public void Signal(string text) {
        throw new NotImplementedException();
    }
    public void Update() {
        SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
        Vector2 mousePos = new Vector2(mouseX, mouseY);
        float scrollY = GetParentWindow().parentProgram.scrollY;
        
        // Checks that a zoom is actually happening and that it will not make the world dissappear.
        if (!dragged && scrollY != 0 && scrollY*0.1f+zoom > 0.1f) {
            zoom += scrollY * 0.1f;
            Utils.DebugLog(Position.Magnitude());
            Position += scrollY > 0? (mousePos-(size*0.5f)) * -0.1f : (Position-(size*0.5f)) * -0.1f;
            if (scrollY > 0) {
                Position += (mousePos-(size*0.5f)) * (1f/(4*zoom));
            }
        }
        
        if (GetParentWindow().IsFocused) {
            if (GetParentWindow().parentProgram.clicked) {
                dragged = true;
                relativeToMouse = mousePos - Position;
            }
            if (!GetParentWindow().parentProgram.mouseDown) {
                dragged = false;
            }
            if (dragged) {
                Position = mousePos - relativeToMouse;
            }
        }
    }
    internal override WindowRenderCombo GetParentWindow() {
        return parent.GetParentWindow();
    }
    void RenderRoom(IntPtr renderer, RoomData room) {
        var rect = new SDL.SDL_FRect(){x=Position.X + zoom*(room.devPosition.X*0.3f), y=Position.Y + zoom*(room.devPosition.Y*0.3f), w=zoom*room.size.X, h=zoom*room.size.Y};
        IntPtr texture = SDL.SDL_CreateTextureFromSurface(renderer, room.roomSurface);
        SDL.SDL_RenderCopyF(renderer, texture, (IntPtr)null, ref rect);
        SDL.SDL_DestroyTexture(texture);
    }
}