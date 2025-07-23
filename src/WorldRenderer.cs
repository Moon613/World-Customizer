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
    IntPtr layer1Texture;
    IntPtr layer2Texture;
    IntPtr layer3Texture;
    IntPtr finalTexture;
    Vector2 originalSize;
    Vector2 dragPosition;
    private WorldData? WorldData { get { return GetParentWindow().parentProgram.currentWorld; }}
    public WorldRenderer(Vector2 position, Vector2 size, GenericUIElement parent, IntPtr renderer) : base(position, size, parent) {
        zoom = 1f;
        dragged = false;
        currentlyFocusedLayer = 0;
        originalSize = size;
        dragPosition = position;
        layer1Texture = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_RGBA8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, (int)size.X, (int)size.Y);
        layer2Texture = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_RGBA8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, (int)size.X, (int)size.Y);
        layer3Texture = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_RGBA8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, (int)size.X, (int)size.Y);
        finalTexture = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_RGBA8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, (int)size.X, (int)size.Y);
        SDL.SDL_SetTextureBlendMode(layer1Texture, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
        SDL.SDL_SetTextureBlendMode(layer2Texture, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
        SDL.SDL_SetTextureBlendMode(layer3Texture, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
        SDL.SDL_SetTextureBlendMode(finalTexture, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
    }
    ~WorldRenderer() {
        SDL.SDL_DestroyTexture(layer1Texture);
        SDL.SDL_DestroyTexture(layer2Texture);
        SDL.SDL_DestroyTexture(layer3Texture);
        SDL.SDL_DestroyTexture(finalTexture);
    }
    public void Render(IntPtr window, IntPtr renderer) {
        SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 0);
        SDL.SDL_SetRenderTarget(renderer, layer1Texture);
        SDL.SDL_RenderClear(renderer);
        SDL.SDL_SetRenderTarget(renderer, layer2Texture);
        SDL.SDL_RenderClear(renderer);
        SDL.SDL_SetRenderTarget(renderer, layer3Texture);
        SDL.SDL_RenderClear(renderer);
        SDL.SDL_SetRenderTarget(renderer, finalTexture);
        SDL.SDL_RenderClear(renderer);
        SDL.SDL_SetRenderTarget(renderer, (IntPtr)null);

        if (WorldData != null) {
            // Parallel.ForEach(WorldData.roomData, (room) => {RenderRoom(renderer, room);});
            foreach (RoomData room in WorldData.roomData) {
                RenderRoom(renderer, room);
            }
        }
        SDL.SDL_SetRenderTarget(renderer, finalTexture);
        var rect = new SDL.SDL_Rect(){x=0, y=0, w=(int)originalSize.X, h=(int)originalSize.Y};
        SDL.SDL_RenderCopy(renderer, layer3Texture, (IntPtr)null, ref rect);
        SDL.SDL_RenderCopy(renderer, layer2Texture, (IntPtr)null, ref rect);
        SDL.SDL_RenderCopy(renderer, layer1Texture, (IntPtr)null, ref rect);
        SDL.SDL_SetRenderTarget(renderer, (IntPtr)null);

        var finalRect = new SDL.SDL_Rect(){x=(int)_position.X, y=(int)_position.Y, w=(int)size.X, h=(int)size.Y};
        SDL.SDL_RenderCopy(renderer, finalTexture, ref finalRect, (IntPtr)null);
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
        if (room.roomTexture == null) {
            room.roomTexture = SDL.SDL_CreateTextureFromSurface(renderer, room.roomSurface);
        }
        var r = new SDL.SDL_Rect(){x=(int)(dragPosition.X + room.devPosition.X*0.5f), y=(int)(dragPosition.Y + room.devPosition.Y*0.5f), w=(int)room.size.X, h=(int)room.size.Y};
        if (room.layer == 0) {
            SDL.SDL_SetRenderTarget(renderer, layer1Texture);
            SDL.SDL_RenderCopy(renderer, (IntPtr)room.roomTexture, (IntPtr)null, ref r);
        }
        else if (room.layer == 1) {
            SDL.SDL_SetRenderTarget(renderer, layer2Texture);
            SDL.SDL_RenderCopy(renderer, (IntPtr)room.roomTexture, (IntPtr)null, ref r);
        }
        else if (room.layer == 2) {
            SDL.SDL_SetRenderTarget(renderer, layer3Texture);
            SDL.SDL_RenderCopy(renderer, (IntPtr)room.roomTexture, (IntPtr)null, ref r);
        }
        SDL.SDL_SetRenderTarget(renderer, (IntPtr)null);
    }
}