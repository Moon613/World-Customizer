using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public string selectedSlugcat;
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
    static List<SDL.SDL_Vertex> biggerCircle = [
        new(){position=new SDL.SDL_FPoint(){x=0, y=0}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=0, y=6}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=6, y=0}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        
        new(){position=new SDL.SDL_FPoint(){x=0, y=0}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=6, y=0}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=0, y=-6}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        
        new(){position=new SDL.SDL_FPoint(){x=0, y=0}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=0, y=-6}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=-6, y=0}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        
        new(){position=new SDL.SDL_FPoint(){x=0, y=0}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=-6, y=0}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=0, y=6}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}}
    ];
    static List<SDL.SDL_Vertex> square = [
        new(){position=new SDL.SDL_FPoint(){x=0, y=0}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=-2, y=2}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=2, y=2}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        
        new(){position=new SDL.SDL_FPoint(){x=0, y=0}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=2, y=2}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=2, y=-2}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        
        new(){position=new SDL.SDL_FPoint(){x=0, y=0}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=2, y=-2}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=-2, y=-2}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        
        new(){position=new SDL.SDL_FPoint(){x=0, y=0}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=-2, y=-2}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=-2, y=2}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}}
    ];
    static List<SDL.SDL_Vertex> biggerSquare = [
        new(){position=new SDL.SDL_FPoint(){x=0, y=0}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=-4, y=4}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=4, y=4}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        
        new(){position=new SDL.SDL_FPoint(){x=0, y=0}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=4, y=4}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=4, y=-4}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        
        new(){position=new SDL.SDL_FPoint(){x=0, y=0}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=4, y=-4}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=-4, y=-4}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        
        new(){position=new SDL.SDL_FPoint(){x=0, y=0}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=-4, y=-4}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}},
        new(){position=new SDL.SDL_FPoint(){x=-4, y=4}, color=new SDL.SDL_Color(){r=255,g=255,b=255,a=255}}
    ];
    public int zoom;
    public bool dragged;
    /// <summary>
    /// This is a bitmask for which layers to draw with transparency.<br></br>If a layer's bit is set to 0 then it will be transparent.
    /// </summary>
    public Layers currentlyFocusedLayers;
    /// <summary>
    /// Used when dragging the world view and rooms around, so that it stays relative to the mouse when moving.
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
    public RoomData? currentlyHoveredRoom;
    public IntPtr cutTexture;
    public string? currentlyEditingNodeSourceRoom;
    public int grabbedConnectionIndex = 0;
    public readonly List<string[]> prepareToCutConnections;
    Vector2 scaledMousePos {get {
        SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
        SDL.SDL_GetWindowSize(GetParentWindow().window, out int w, out int h);
        return (size / new Vector2(w, h)) * new Vector2(mouseX, mouseY) + Position;
    }}
    private WorldData? WorldData { get { return GetParentWindow().parentProgram.currentWorld; }}
    public WorldRenderer(Vector2 position, Vector2 size, GenericUIElement parent, IntPtr renderer) : base(position, size, parent) {
        zoom = 1;
        dragged = false;
        currentlyFocusedLayers = Layers.Layer1 | Layers.Layer2 | Layers.Layer3;
        originalSize = size;
        dragPosition = Vector2.Zero;
        currentlyHoveredRoom = null;
        prepareToCutConnections = new List<string[]>();
        currentlyEditingNodeSourceRoom = null;
        selectedSlugcat = "White";
        layer1Texture = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_RGBA8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, (int)size.X, (int)size.Y);
        layer2Texture = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_RGBA8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, (int)size.X, (int)size.Y);
        layer3Texture = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_RGBA8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, (int)size.X, (int)size.Y);
        finalTexture = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_RGBA8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, (int)size.X, (int)size.Y);
        cutTexture = SDL_image.IMG_LoadTexture(renderer, Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "textures" + Path.DirectorySeparatorChar + "cut.png");
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
        SDL.SDL_DestroyTexture(cutTexture);
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
        if (GetParentMainWindow().currentlyFocusedObject != this) {
            return;
        }
        float scrollY = GetParentWindow().parentProgram.scrollY;
        prepareToCutConnections.Clear();
        
        
        if (GetParentWindow().IsFocused) {
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
            // This is used to report if the mouse is currently over a room on the current frame, so that it is not immediantly de-selected when
            // not dragging it around.
            bool mouseOverRoom = false;
            if (WorldData != null) {
                // This candidate room is needed because otherwise rooms before the actual hovered room in the draw order would steal become the
                // currently hovered room, activating all checks for if that room is the current one in the loop when they shouldn't have been true.
                // So this is assigned to that value after the loop.
                RoomData? candidateHoveredRoom = null;
                foreach (RoomData room in WorldData.roomData) {
                    Vector2 roomPosition = dragPosition + room.devPosition*0.5f;
                    if (currentlyEditingNodeSourceRoom == null && !dragged && IsLayerInteractible(room.layer) && scaledMousePos.X > roomPosition.X && scaledMousePos.X < roomPosition.X+room.size.X && scaledMousePos.Y > roomPosition.Y && scaledMousePos.Y < roomPosition.Y+room.size.Y && (currentlyHoveredRoom == null || (currentlyHoveredRoom != null && currentlyHoveredRoom.layer >= room.layer))) {
                        candidateHoveredRoom = room;
                        mouseOverRoom = true;
                    }

                    for (int i = 0; i < room.roomConnectionPositions.Count; i++) {
                        Vector2 connectionInThisRoom = dragPosition + room.devPosition*0.5f + room.roomConnectionPositions[i];
                        bool clickedOnNode = IsLayerInteractible(room.layer) && GetParentWindow().parentProgram.clicked && scaledMousePos.X >= connectionInThisRoom.X-6 && scaledMousePos.X <= connectionInThisRoom.X+6 && scaledMousePos.Y >= connectionInThisRoom.Y-6 && scaledMousePos.Y <= connectionInThisRoom.Y+6 && (currentlyEditingNodeSourceRoom != null || currentlyHoveredRoom == room);
                        // Gates break a lot rn so don't use them.
                        if (room.roomConnections[i].Contains("GATE")) {
                            continue;
                        }
                        // This skips logic that would break if a connection is disconnected, without having to do extra math.
                        else if (room.roomConnections[i] == "DISCONNECTED") {
                            goto ThisConnectionIsDisconnected;
                        }
                        
                        RoomData connectedRoom = WorldData.roomData.First(x => x.name.ToUpper() == room.roomConnections[i]);
                        int indexInConnectedRoomConList = connectedRoom.roomConnections.IndexOf(room.name.ToUpper());
                        if (indexInConnectedRoomConList == -1) {
                            Utils.DebugLog($"Error finding room connection position from {room.name} to {connectedRoom.name}. Removed problematic connection.");
                            room.roomConnections[i] = "DISCONNECTED";
                            continue;
                        }
                        Vector2 connectionInOtherRoomPosition = dragPosition + connectedRoom.devPosition*0.5f + connectedRoom.roomConnectionPositions[indexInConnectedRoomConList];

                        // This code is for cutting a room connection
                        Vector2 centerPoint = connectionInThisRoom + 0.5f*(connectionInOtherRoomPosition - connectionInThisRoom);
                        if (IsLayerInteractible(room.layer) && scaledMousePos.X >= centerPoint.X-10 && scaledMousePos.X <= centerPoint.X+10 && scaledMousePos.Y >= centerPoint.Y-10 && scaledMousePos.Y <= centerPoint.Y+10) {
                            if (GetParentWindow().parentProgram.clicked) {
                                room.roomConnections[i] = "DISCONNECTED";
                                connectedRoom.roomConnections[indexInConnectedRoomConList] = "DISCONNECTED";
                            }
                            else {
                                prepareToCutConnections.Add([room.roomConnections[i], connectedRoom.roomConnections[indexInConnectedRoomConList]]);
                            }
                        }

                        // If the node was clicked on and is connected, disconnect the other end.
                        if (clickedOnNode) {
                            connectedRoom.roomConnections[indexInConnectedRoomConList] = "DISCONNECTED";
                        }
                        ThisConnectionIsDisconnected:
                        if (clickedOnNode) {
                            if (currentlyEditingNodeSourceRoom == null && currentlyHoveredRoom == room) {
                                currentlyHoveredRoom = null;
                                room.roomConnections[i] = "DISCONNECTED";
                                currentlyEditingNodeSourceRoom = room.name.ToUpper();
                                grabbedConnectionIndex = i;
                            }
                            else if (currentlyEditingNodeSourceRoom != null && room.name.ToUpper() != currentlyEditingNodeSourceRoom) {
                                room.roomConnections[i] = currentlyEditingNodeSourceRoom;
                                WorldData.roomData.First(x => x.name.ToUpper() == currentlyEditingNodeSourceRoom).roomConnections[grabbedConnectionIndex] = room.name.ToUpper();
                                currentlyEditingNodeSourceRoom = null;
                                grabbedConnectionIndex = 0;
                            }
                        }
                    }

                    for (int i = 0; i < room.creatureSpawnPositions.Count; i++) {
                        Vector2 denPosition = dragPosition + room.devPosition*0.5f + room.creatureSpawnPositions[i];
                        bool clickedOnNode = IsLayerInteractible(room.layer) && GetParentWindow().parentProgram.rightClicked && scaledMousePos.X >= denPosition.X-4 && scaledMousePos.X <= denPosition.X+4 && scaledMousePos.Y >= denPosition.Y-4 && scaledMousePos.Y <= denPosition.Y+4 && (currentlyEditingNodeSourceRoom != null || currentlyHoveredRoom == room);
                        if (clickedOnNode) {
                            OpenDenMenu(room.creatureSpawnData.FindAll(x => room.creatureDenIndexToAbstractNodeMap[i] == x.pipeNumber && (x.slugcats == null || x.slugcats.Contains(selectedSlugcat))), room.name);
                        }
                    }
                }
                // Assigns the candidate hover room to the currently hovered room if it is not null.
                // Without the null check, the current hovered room would be assigned null right after being assigned
                // a room, messing with the view dragging code.
                if (candidateHoveredRoom != null) {
                    currentlyHoveredRoom = candidateHoveredRoom;
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
    void OpenDenMenu(List<SpawnData> spawnData, string roomName) {
        if (spawnData.Count > 0 && GetParentWindow().updatables.FirstOrDefault(x => x is DenMenu denMenu && denMenu.spawnData.SequenceEqual(spawnData)) == default) {
            Vector2 size = new Vector2(300, 500);
            var denMenu = new DenMenu(GetParentWindow().size/2 - size/2, size, GetParentWindow(), spawnData, roomName);
            GetParentWindow().AddChild(denMenu);
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
            SDL.SDL_SetRenderDrawColor(renderer,bkgFillModifier, (byte)(255 - bkgFillModifier*0.5f), bkgFillModifier, 255);
            SDL.SDL_RenderFillRectF(renderer, ref outline);
            SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
            SDL.SDL_RenderDrawRectF(renderer, ref outline);
            SDL.SDL_RenderCopyF(renderer, (IntPtr)room.roomTexture, (IntPtr)null, ref r);
        }
        else if (room.layer == Layers.Layer3) {
            SDL.SDL_SetRenderTarget(renderer, layer3Texture);
            SDL.SDL_SetRenderDrawColor(renderer, (byte)(255 - bkgFillModifier*0.5f), bkgFillModifier, bkgFillModifier, 255);
            SDL.SDL_RenderFillRectF(renderer, ref outline);
            SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
            SDL.SDL_RenderDrawRectF(renderer, ref outline);
            SDL.SDL_RenderCopyF(renderer, (IntPtr)room.roomTexture, (IntPtr)null, ref r);
        }
        for (int i = 0; i < room.roomConnectionPositions.Count; i++) {
            Vector2 connectionInThisRoom = dragPosition + room.devPosition*0.5f + room.roomConnectionPositions[i];
            // Gates break a lot rn so don't use them.
            if (room.roomConnections[i].Contains("GATE")) {
                continue;
            }
            // If the room connection is being formed by the user, the other end of the connection should be at the mouse position.
            else if (room.name.ToUpper() == currentlyEditingNodeSourceRoom && i == grabbedConnectionIndex) {
                Utils.DrawGeometryWithVertices(renderer, connectionInThisRoom, biggerCircle.ToArray());
                SDL.SDL_RenderDrawLineF(renderer, connectionInThisRoom.X, connectionInThisRoom.Y, scaledMousePos.X, scaledMousePos.Y);
                continue;
            }

            if (room.roomConnections[i] != "DISCONNECTED") {
                RoomData connectedRoom = WorldData!.roomData.First(x => x.name.ToUpper() == room.roomConnections[i]);
                int indexInConnectedRoomConList = connectedRoom.roomConnections.IndexOf(room.name.ToUpper());
                Vector2 connectionInOtherRoomPosition = dragPosition + connectedRoom.devPosition*0.5f + connectedRoom.roomConnectionPositions[indexInConnectedRoomConList];
                
                SDL.SDL_RenderDrawLineF(renderer, connectionInThisRoom.X, connectionInThisRoom.Y, connectionInOtherRoomPosition.X, connectionInOtherRoomPosition.Y);

                // This code is for cutting a room connection
                Vector2 centerPoint = connectionInThisRoom + 0.5f*(connectionInOtherRoomPosition - connectionInThisRoom);
                if (prepareToCutConnections.FirstOrDefault(x => x[0] == room.roomConnections[i] && x[1] == connectedRoom.roomConnections[indexInConnectedRoomConList]) != default) {
                    var rect = new SDL.SDL_FRect(){x=centerPoint.X-10, y=centerPoint.Y-10, w=20, h=20};
                    SDL.SDL_RenderCopyF(renderer, cutTexture, (IntPtr)null, ref rect);
                }
            }

            // Drawing the white diamond over the position of the connection
            bool hoveredOver = IsLayerInteractible(room.layer) && scaledMousePos.X >= connectionInThisRoom.X-6 && scaledMousePos.X <= connectionInThisRoom.X+6 && scaledMousePos.Y >= connectionInThisRoom.Y-6 && scaledMousePos.Y <= connectionInThisRoom.Y+6;
            if (hoveredOver && (currentlyEditingNodeSourceRoom != null || currentlyHoveredRoom == room)) {
                Utils.DrawGeometryWithVertices(renderer, connectionInThisRoom, biggerCircle.ToArray());
            }
            else {
                Utils.DrawGeometryWithVertices(renderer, connectionInThisRoom, circle.ToArray());
            }
        }
        for (int i = 0; i < room.creatureSpawnPositions.Count; i++) {
            Vector2 spawnPosition = dragPosition + room.devPosition*0.5f + room.creatureSpawnPositions[i];
            // Drawing the white square over the position of the creature den
            bool hoveredOver = IsLayerInteractible(room.layer) && scaledMousePos.X >= spawnPosition.X-6 && scaledMousePos.X <= spawnPosition.X+6 && scaledMousePos.Y >= spawnPosition.Y-6 && scaledMousePos.Y <= spawnPosition.Y+6;

            if (hoveredOver && (currentlyEditingNodeSourceRoom != null || currentlyHoveredRoom == room)) {
                Utils.DrawGeometryWithVertices(renderer, spawnPosition, biggerSquare.ToArray());
            }
            else {
                Utils.DrawGeometryWithVertices(renderer, spawnPosition, square.ToArray());
            }
        }
        Utils.WriteText(renderer, IntPtr.Zero, room.name, Utils.currentFont, dragPosition.X+room.devPosition.X*0.5f, dragPosition.Y+room.devPosition.Y*0.5f-11.5f, 11);
        SDL.SDL_SetRenderTarget(renderer, (IntPtr)null);
    }
    private bool IsLayerInteractible(Layers layer) {
        return (currentlyFocusedLayers & layer) != 0;
    }
}