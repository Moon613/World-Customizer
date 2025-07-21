using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using SDL2;

#nullable enable
namespace WorldCustomizer;

class WorldRenderer : GenericUIElement, IRenderable, IAmInteractable {
    float zoom;
    bool dragged;
    int currentlyFocusedLayer;
    Vector2 relativeToMouse;
    IntPtr layer1Surface;
    IntPtr layer2Surface;
    IntPtr layer3Surface;
    IntPtr finalSurface;
    Vector2 originalSize;
    Vector2 dragPosition;
    private WorldData? WorldData { get { return GetParentWindow().parentProgram.currentWorld; }}
    public WorldRenderer(Vector2 position, Vector2 size, GenericUIElement parent) : base(position, size, parent) {
        zoom = 1f;
        dragged = false;
        currentlyFocusedLayer = 0;
        originalSize = size;
        dragPosition = position;
        layer1Surface = SDL.SDL_CreateRGBSurface(0, (int)size.X, (int)size.Y, 32, 0x000000FF, 0x0000FF00, 0x00FF0000, 0xFF000000);
        layer2Surface = SDL.SDL_CreateRGBSurface(0, (int)size.X, (int)size.Y, 32, 0x000000FF, 0x0000FF00, 0x00FF0000, 0xFF000000);
        layer3Surface = SDL.SDL_CreateRGBSurface(0, (int)size.X, (int)size.Y, 32, 0x000000FF, 0x0000FF00, 0x00FF0000, 0xFF000000);
        finalSurface = SDL.SDL_CreateRGBSurface(0, (int)size.X, (int)size.Y, 32, 0x000000FF, 0x0000FF00, 0x00FF0000, 0xFF000000);
    }
    ~WorldRenderer() {
        SDL.SDL_FreeSurface(layer1Surface);
        SDL.SDL_FreeSurface(layer2Surface);
        SDL.SDL_FreeSurface(layer3Surface);
        SDL.SDL_FreeSurface(finalSurface);
    }
    public void Render(IntPtr window, IntPtr renderer) {
        SDL.SDL_FillRect(layer1Surface, (IntPtr)null, 0x00000000);
        SDL.SDL_FillRect(layer2Surface, (IntPtr)null, 0x00000000);
        SDL.SDL_FillRect(layer3Surface, (IntPtr)null, 0x00000000);
        SDL.SDL_FillRect(finalSurface, (IntPtr)null, 0x00000000);

        if (WorldData != null) {
            // Parallel.ForEach(WorldData.roomData, (room) => {RenderRoom(renderer, room);});
            foreach (RoomData room in WorldData.roomData) {
                RenderRoom(renderer, room);
            }
        }
        var rect = new SDL.SDL_Rect(){x=0, y=0, w=(int)originalSize.X, h=(int)originalSize.Y};
        SDL.SDL_BlitScaled(layer3Surface, (IntPtr)null, finalSurface, ref rect);
        SDL.SDL_BlitScaled(layer2Surface, (IntPtr)null, finalSurface, ref rect);
        SDL.SDL_BlitScaled(layer1Surface, (IntPtr)null, finalSurface, ref rect);

        var finalRect = new SDL.SDL_Rect(){x=(int)_position.X, y=(int)_position.Y, w=(int)size.X, h=(int)size.Y};
        IntPtr texture = SDL.SDL_CreateTextureFromSurface(renderer, finalSurface);
        SDL.SDL_RenderCopy(renderer, texture, ref finalRect, (IntPtr)null);
        SDL.SDL_DestroyTexture(texture);
    }
    public void Signal(string text) {
        throw new NotImplementedException();
    }
    public void Update() {
        SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
        Vector2 mousePos = new Vector2(mouseX, mouseY);
        float scrollY = GetParentWindow().parentProgram.scrollY;
        
        // Checks that a zoom is actually happening and that it will not make the world dissappear.
        if (!dragged && scrollY != 0 && scrollY*0.1f+zoom >= 1 && scrollY*0.1f+zoom <= 3) {
            zoom += scrollY * 0.1f;
            if (scrollY > 0) {
                Position += new Vector2(32, 18);
                size -= new Vector2(64, 36);
            }
            else if (scrollY < 0) {
                Position -= new Vector2(32, 18);
                size += new Vector2(64, 36);
            }
        }
        
        if (GetParentWindow().IsFocused) {
            if (GetParentWindow().parentProgram.clicked) {
                dragged = true;
                relativeToMouse = mousePos*(1/zoom) - dragPosition;
            }
            if (!GetParentWindow().parentProgram.mouseDown) {
                dragged = false;
            }
            if (dragged) {
                dragPosition = mousePos*(1/zoom) - relativeToMouse;
            }
        }
    }
    internal override WindowRenderCombo GetParentWindow() {
        return parent.GetParentWindow();
    }
    void RenderRoom(IntPtr renderer, RoomData room) {
        var r = new SDL.SDL_Rect(){x=(int)(dragPosition.X + room.devPosition.X*0.5f), y=(int)(dragPosition.Y + room.devPosition.Y*0.5f), w=(int)room.size.X, h=(int)room.size.Y};
        if (room.layer == 0) {
            SDL.SDL_BlitScaled(room.roomSurface, (IntPtr)null, layer1Surface, ref r);
        }
        else if (room.layer == 1) {
            SDL.SDL_BlitScaled(room.roomSurface, (IntPtr)null, layer2Surface, ref r);
        }
        else if (room.layer == 2) {
            SDL.SDL_BlitScaled(room.roomSurface, (IntPtr)null, layer3Surface, ref r);
        }
        // var rect = new SDL.SDL_FRect(){x=Position.X + zoom*(room.devPosition.X*0.3f), y=Position.Y + zoom*(room.devPosition.Y*0.3f), w=zoom*room.size.X, h=zoom*room.size.Y};
        // IntPtr texture = SDL.SDL_CreateTextureFromSurface(renderer, room.roomSurface);
        // SDL.SDL_RenderCopyF(renderer, texture, (IntPtr)null, ref rect);
        // SDL.SDL_DestroyTexture(texture);
    }
}