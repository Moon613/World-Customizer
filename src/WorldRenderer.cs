using System;
using System.Collections.Generic;
using System.Numerics;
using SDL2;

#nullable enable
namespace WorldCustomizer;

class WorldRenderer : GenericUIElement, IRenderable, IAmInteractable {
    static List<SDL.SDL_Vertex> circle = [
        new(){position=new SDL.SDL_FPoint(){x=0, y=0}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=0, y=3}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=3, y=0}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        
        new(){position=new SDL.SDL_FPoint(){x=0, y=0}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=3, y=0}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=0, y=-3}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        
        new(){position=new SDL.SDL_FPoint(){x=0, y=0}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=0, y=-3}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=-3, y=0}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        
        new(){position=new SDL.SDL_FPoint(){x=0, y=0}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=-3, y=0}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=0, y=3}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}}
    ];
    public int zoom;
    public bool dragged;
    /// <summary>
    /// This is a bitmask for which layers to draw with transparency.<br></br>If a layer's bit is set to 0 then it will be transparent.
    /// </summary>
    public byte currentlyFocusedLayers;
    /// <summary>
    /// Used when dragging the world around, so that it stays relative to the mouse when moving.
    /// </summary>
    public Vector2 relativeToMouse;
    /// <summary>
    /// The texture that all rooms on layer 1 are rendered to.
    /// </summary>
    public IntPtr layer1Texture;
    /// <summary>
    /// The texture that all rooms on layer 2 are rendered to.
    /// </summary>
    public IntPtr layer2Texture;
    /// <summary>
    /// The texture that all rooms on layer 3 are rendered to.
    /// </summary>
    public IntPtr layer3Texture;
    /// <summary>
    /// The render target for all the layer textures once their rooms are drawn.<br></br>Layer 3 is drawn first and layer 1 last,
    /// so rooms on layer 1 cover up rooms on layer 3.
    /// </summary>
    public IntPtr finalTexture;
    /// <summary>
    /// The original size the renderer was created with.<br></br>
    /// This is so that the layer textures can be drawn to the final texture at the same size they were all created with.
    /// </summary>
    public Vector2 originalSize;
    /// <summary>
    /// Keeps track of how to view has been moved around,
    /// so that the rooms can move without effecting the position of the world renderer.
    /// </summary>
    public Vector2 dragPosition;
    private WorldData? WorldData { get { return GetParentWindow().parentProgram.currentWorld; }}
    public WorldRenderer(Vector2 position, Vector2 size, GenericUIElement parent, IntPtr renderer) : base(position, size, parent) {
        zoom = 1;
        dragged = false;
        currentlyFocusedLayers = 1;
        originalSize = size;
        dragPosition = Vector2.Zero;
        layer1Texture = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_RGBA8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, (int)size.X, (int)size.Y);
        layer2Texture = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_RGBA8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, (int)size.X, (int)size.Y);
        layer3Texture = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_RGBA8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, (int)size.X, (int)size.Y);
        finalTexture = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_RGBA8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, (int)size.X, (int)size.Y);
        SDL.SDL_SetTextureBlendMode(layer1Texture, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
        SDL.SDL_SetTextureBlendMode(layer2Texture, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
        SDL.SDL_SetTextureBlendMode(layer3Texture, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
        SDL.SDL_SetTextureBlendMode(finalTexture, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
    }
    public void Destroy() {
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

        if ((currentlyFocusedLayers & 4) == 0) {
            SDL.SDL_SetTextureAlphaMod(layer3Texture, 128);
        }
        else {
            SDL.SDL_SetTextureAlphaMod(layer3Texture, 255);
        }
        if ((currentlyFocusedLayers & 2) == 0) {
            SDL.SDL_SetTextureAlphaMod(layer2Texture, 128);
        }
        else {
            SDL.SDL_SetTextureAlphaMod(layer2Texture, 255);
        }
        if ((currentlyFocusedLayers & 1) == 0) {
            SDL.SDL_SetTextureAlphaMod(layer1Texture, 128);
        }
        else {
            SDL.SDL_SetTextureAlphaMod(layer1Texture, 255);
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
        
        // Utils.DebugLog($"{zoom} + {scrollY} = {scrollY+zoom}");
        Utils.DebugLog($"{mousePos} - {relativeToMouse} = {mousePos-relativeToMouse} OR {dragPosition}");
        // Checks that a zoom is actually happening and that it will not make the world dissappear.
        if (!dragged && scrollY != 0 && scrollY+zoom >= 1 && scrollY+zoom <= 20) {
            zoom += (int)scrollY;
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
                relativeToMouse = mousePos - dragPosition;
            }
            if (!GetParentWindow().parentProgram.mouseDown) {
                dragged = false;
            }
            if (dragged) {
                dragPosition = mousePos - relativeToMouse;
            }
        }
    }
    internal override WindowRenderCombo GetParentWindow() {
        return parent.GetParentWindow();
    }
    void RenderRoom(IntPtr renderer, RoomData room) {
        if (room.roomTexture == null) {
            room.roomTexture = SDL.SDL_CreateTextureFromSurface(renderer, room.roomSurface);
            SDL.SDL_SetTextureBlendMode((IntPtr)room.roomTexture, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
        }
        var r = new SDL.SDL_FRect(){x=dragPosition.X + room.devPosition.X*0.5f, y=dragPosition.Y + room.devPosition.Y*0.5f, w=room.size.X, h=room.size.Y};
        var outline = new SDL.SDL_FRect(){x=dragPosition.X + room.devPosition.X*0.5f - 2.5f, y=dragPosition.Y + room.devPosition.Y*0.5f - 12, w=room.size.X+5, h=room.size.Y+16.5f};;
        if (room.layer == 0) {
            SDL.SDL_SetRenderTarget(renderer, layer1Texture);
            SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
            SDL.SDL_RenderFillRectF(renderer, ref outline);
            SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
            SDL.SDL_RenderDrawRectF(renderer, ref outline);
            SDL.SDL_RenderCopyF(renderer, (IntPtr)room.roomTexture, (IntPtr)null, ref r);
        }
        else if (room.layer == 1) {
            SDL.SDL_SetRenderTarget(renderer, layer2Texture);
            SDL.SDL_SetRenderDrawColor(renderer, 0, 255, 0, 255);
            SDL.SDL_RenderFillRectF(renderer, ref outline);
            SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
            SDL.SDL_RenderDrawRectF(renderer, ref outline);
            SDL.SDL_RenderCopyF(renderer, (IntPtr)room.roomTexture, (IntPtr)null, ref r);
        }
        else if (room.layer == 2) {
            SDL.SDL_SetRenderTarget(renderer, layer3Texture);
            SDL.SDL_SetRenderDrawColor(renderer, 255, 0, 0, 255);
            SDL.SDL_RenderFillRectF(renderer, ref outline);
            SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
            SDL.SDL_RenderDrawRectF(renderer, ref outline);
            SDL.SDL_RenderCopyF(renderer, (IntPtr)room.roomTexture, (IntPtr)null, ref r);
        }
        Utils.WriteText(renderer, IntPtr.Zero, room.name, Utils.currentFont, dragPosition.X+room.devPosition.X*0.5f, dragPosition.Y+room.devPosition.Y*0.5f-11.5f, 11);
        foreach (var con in room.roomConnections) {
            Utils.DrawGeometryWithVertices(renderer, con+dragPosition+room.devPosition*0.5f, circle.ToArray());
        }
        SDL.SDL_SetRenderTarget(renderer, (IntPtr)null);
    }
}