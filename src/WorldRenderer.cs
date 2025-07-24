using System;
using System.Collections.Generic;
using System.Numerics;
using SDL2;

#nullable enable
namespace WorldCustomizer;

internal class WorldRenderer : FocusableUIElement, IRenderable {
    [Flags]
    public enum Layers : byte {
        Layer1 = 1,
        Layer2 = 2,
        Layer3 = 4
    }
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
    public Layers currentlyFocusedLayers;
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
    RoomData? currentlyHoveredRoom;
    private WorldData? WorldData { get { return GetParentWindow().parentProgram.currentWorld; }}
    public WorldRenderer(Vector2 position, Vector2 size, GenericUIElement parent, IntPtr renderer) : base(position, size, parent) {
        zoom = 1;
        dragged = false;
        currentlyFocusedLayers = Layers.Layer1;
        originalSize = size;
        dragPosition = Vector2.Zero;
        currentlyHoveredRoom = null;
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

        if (!IsLayerInteractible(Layers.Layer3)) {
            SDL.SDL_SetTextureAlphaMod(layer3Texture, 128);
        }
        else {
            SDL.SDL_SetTextureAlphaMod(layer3Texture, 255);
        }
        if (!IsLayerInteractible(Layers.Layer2)) {
            SDL.SDL_SetTextureAlphaMod(layer2Texture, 128);
        }
        else {
            SDL.SDL_SetTextureAlphaMod(layer2Texture, 255);
        }
        if (!IsLayerInteractible(Layers.Layer1)) {
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
    public override void Update() {
        base.Update();
        if (GetParentWindow().currentlyFocusedObject != this) {
            return;
        }
        SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
        Vector2 mousePos = new Vector2(mouseX, mouseY);
        SDL.SDL_GetWindowSize(GetParentWindow().window, out int w, out int h);
        Vector2 currentWindowSize = new Vector2(w, h);
        float scrollY = GetParentWindow().parentProgram.scrollY;
        
        // Checks that a zoom is actually happening and that it will not make the world dissappear.
        if (!dragged && scrollY != 0 && (int)scrollY+zoom >= 1 && (int)scrollY+zoom <= 20) {
            zoom += (int)scrollY;
            if (scrollY > 0) {
                Position += (int)scrollY * new Vector2(32, 18);
                size -= (int)scrollY * new Vector2(64, 36);
            }
            else if (scrollY < 0) {
                Position += (int)scrollY * new Vector2(32, 18);
                size -= (int)scrollY * new Vector2(64, 36);
            }
        }

        Vector2 scaledMousePos = (size / currentWindowSize) * mousePos + Position;
        Utils.DebugLog($"{dragged}\n{mousePos} - {relativeToMouse} = {mousePos-relativeToMouse} OR {dragPosition}\n{currentWindowSize}, {size}, {scaledMousePos}");
        
        if (GetParentWindow().IsFocused) {
            // This is used to report if the mouse is currently over a room on the current frame, so that it is not immediantly de-selected when
            // not dragging it around.
            bool mouseOverRoom = false;
            if (WorldData != null) {
                foreach (RoomData room in WorldData.roomData) {
                    Vector2 roomPosition = dragPosition + room.devPosition*0.5f;
                    if (!dragged && IsLayerInteractible(room.layer) && scaledMousePos.X > roomPosition.X && scaledMousePos.X < roomPosition.X+room.size.X && scaledMousePos.Y > roomPosition.Y && scaledMousePos.Y < roomPosition.Y+room.size.Y && (currentlyHoveredRoom == null || (currentlyHoveredRoom != null && currentlyHoveredRoom.layer >= room.layer))) {
                        currentlyHoveredRoom = room;
                        mouseOverRoom = true;
                    }
                }
            }
            // If the mouse is not over a room and one is not currently being moved,
            // then the currently hovered room is set to null. The dragged check is because the room
            // can lag behind the mouse if the mouse moves fast enough.
            if (!mouseOverRoom && !dragged) {
                currentlyHoveredRoom = null;
            }
            if (GetParentWindow().IsFocused && GetParentWindow().parentProgram.clicked) {
                dragged = true;
                if (currentlyHoveredRoom != null) {
                    relativeToMouse = scaledMousePos - currentlyHoveredRoom.devPosition*0.5f;
                }
                else {
                    relativeToMouse = scaledMousePos - dragPosition;
                }
            }
            if (GetParentWindow().IsFocused && !GetParentWindow().parentProgram.mouseDown) {
                dragged = false;
            }
            if (dragged) {
                if (currentlyHoveredRoom != null) {
                    currentlyHoveredRoom.devPosition = 2*(scaledMousePos-relativeToMouse);
                }
                else {
                    dragPosition = scaledMousePos - relativeToMouse;
                }
            }
        }
    }
    void RenderRoom(IntPtr renderer, RoomData room) {
        if (room.roomTexture == null) {
            room.roomTexture = SDL.SDL_CreateTextureFromSurface(renderer, room.roomSurface);
            SDL.SDL_SetTextureBlendMode((IntPtr)room.roomTexture, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
        }
        var r = new SDL.SDL_FRect(){x=dragPosition.X + room.devPosition.X*0.5f, y=dragPosition.Y + room.devPosition.Y*0.5f, w=room.size.X, h=room.size.Y};
        var outline = new SDL.SDL_FRect(){x=dragPosition.X + room.devPosition.X*0.5f - 2.5f, y=dragPosition.Y + room.devPosition.Y*0.5f - 12, w=room.size.X+5, h=room.size.Y+16.5f};
        byte bkgFillModifier = 0;
        if (currentlyHoveredRoom == room) {
            bkgFillModifier = 128;
        }
        if (room.layer == Layers.Layer1) {
            SDL.SDL_SetRenderTarget(renderer, layer1Texture);
            SDL.SDL_SetRenderDrawColor(renderer, bkgFillModifier, bkgFillModifier, bkgFillModifier, 255);
            SDL.SDL_RenderFillRectF(renderer, ref outline);
            SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
            SDL.SDL_RenderDrawRectF(renderer, ref outline);
            SDL.SDL_RenderCopyF(renderer, (IntPtr)room.roomTexture, (IntPtr)null, ref r);
        }
        else if (room.layer == Layers.Layer2) {
            SDL.SDL_SetRenderTarget(renderer, layer2Texture);
            SDL.SDL_SetRenderDrawColor(renderer,bkgFillModifier, 255,bkgFillModifier, 255);
            SDL.SDL_RenderFillRectF(renderer, ref outline);
            SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
            SDL.SDL_RenderDrawRectF(renderer, ref outline);
            SDL.SDL_RenderCopyF(renderer, (IntPtr)room.roomTexture, (IntPtr)null, ref r);
        }
        else if (room.layer == Layers.Layer3) {
            SDL.SDL_SetRenderTarget(renderer, layer3Texture);
            SDL.SDL_SetRenderDrawColor(renderer, 255, bkgFillModifier, bkgFillModifier, 255);
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
    private bool IsLayerInteractible(Layers layer) {
        return (currentlyFocusedLayers & layer) != 0;
    }
}