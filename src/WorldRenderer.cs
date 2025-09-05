using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using SDL2;

#nullable enable
#pragma warning disable CA1806
namespace WorldCustomizer;

internal class WorldRenderer : FocusableUIElement, IRenderable {
    [Flags]
    public enum Layers : byte {
        Layer1 = 1,
        Layer2 = 2,
        Layer3 = 4
    }
    public string selectedSlugcat;
    public bool viewSubregions;
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
    public readonly List<RoomConnection> prepareToCutConnections;
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
        prepareToCutConnections = new();
        currentlyEditingNodeSourceRoom = null;
        selectedSlugcat = "White";
        viewSubregions = false;
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
        if (GetParentWindow().parentProgram.pressedKey == SDL.SDL_Keycode.SDLK_d) {
            File.WriteAllText(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "connections.txt", "");
            foreach (var con in WorldData!.roomConnections) {
                File.AppendAllText(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "connections.txt", con.ToString() + "\n");
            }
        }
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
            // This is used to determine if a node was already interacted with, so the room menu doesn't open if so.
            bool intereactedWithNode = false;
            if (WorldData != null) {
                // This candidate room is needed because otherwise rooms before the actual hovered room in the draw order would steal become the
                // currently hovered room, activating all checks for if that room is the current one in the loop when they shouldn't have been true.
                // So this is assigned to that value after the loop.
                RoomData? candidateHoveredRoom = null;
                foreach (RoomData room in WorldData.roomData) {
                    Vector2 roomPosition = dragPosition + room.devPosition*0.5f;
                    if (currentlyEditingNodeSourceRoom == null && !dragged && IsLayerInteractible(room.layer) && scaledMousePos.X > roomPosition.X-2.5f && scaledMousePos.X < roomPosition.X+room.size.X+2.5f && scaledMousePos.Y > roomPosition.Y-12 && scaledMousePos.Y < roomPosition.Y+room.size.Y+4.5f && (currentlyHoveredRoom == null || (currentlyHoveredRoom != null && currentlyHoveredRoom.layer >= room.layer))) {
                        candidateHoveredRoom = room;
                        mouseOverRoom = true;
                    }

                    List<RoomConnection> roomConnections = WorldData.roomConnections.FindAll(x => x.sourceRoom == room.name);
                    for (int i = 0; i < roomConnections.Count; i++) {
                        ConnectionUpdate(room, ref intereactedWithNode, roomConnections[i], false);
                    }
                    roomConnections = WorldData.roomConnections.FindAll(x => x.destinationRoom == room.name);
                    for (int i = 0; i < roomConnections.Count; i++) {
                        ConnectionUpdate(room, ref intereactedWithNode, roomConnections[i], true);
                    }

                    for (int i = 0; i < room.creatureSpawnPositions.Count; i++) {
                        Vector2 denPosition = dragPosition + room.devPosition*0.5f + room.creatureSpawnPositions[i];
                        bool clickedOnNode = IsLayerInteractible(room.layer) && GetParentWindow().parentProgram.rightClicked && scaledMousePos.X >= denPosition.X-4 && scaledMousePos.X <= denPosition.X+4 && scaledMousePos.Y >= denPosition.Y-4 && scaledMousePos.Y <= denPosition.Y+4 && (currentlyEditingNodeSourceRoom != null || currentlyHoveredRoom == room);
                        intereactedWithNode |= clickedOnNode;
                        if (clickedOnNode) {
                            List<SpawnData> spawns = room.creatureSpawnData.FindAll(x => room.creatureDenIndexToAbstractNodeMap[i] == x.pipeNumber && (x.slugcats == null || (!x.exclusive && x.slugcats.Contains(selectedSlugcat)) || (x.exclusive && !x.slugcats.Contains(selectedSlugcat))));
                            if (spawns.Count == 0) {
                                spawns.Add(new SpawnData(null, false, room.creatureDenIndexToAbstractNodeMap[i], "NONE"));
                                room.creatureSpawnData.Add(spawns[0]);
                            }
                            OpenDenMenu(spawns, room.name);
                        }
                    }
                }
                // Assigns the candidate hover room to the currently hovered room if it is not null.
                // Without the null check, the current hovered room would be assigned null right after being assigned
                // a room, messing with the view dragging code.
                if (candidateHoveredRoom != null) {
                    currentlyHoveredRoom = candidateHoveredRoom;
                }
                if (GetParentWindow().IsFocused && GetParentWindow().parentProgram.rightClicked && currentlyEditingNodeSourceRoom != null) {
                    WorldData.roomConnections.First(x => x.sourceRoom == currentlyEditingNodeSourceRoom && x.sourceNodeIndex == grabbedConnectionIndex).destinationRoom = "DISCONNECTED";
                    currentlyEditingNodeSourceRoom = null;
                    grabbedConnectionIndex = 0;
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
            if (GetParentWindow().IsFocused && !intereactedWithNode && GetParentWindow().parentProgram.rightClicked && currentlyHoveredRoom != null) {
                OpenRoomMenu(currentlyHoveredRoom);
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
    void ConnectionUpdate(RoomData room, ref bool intereactedWithNode, RoomConnection roomConnection, bool flipped) {
        Vector2 connectionInThisRoom = dragPosition + room.devPosition*0.5f + roomConnection.GetSourcePosition(flipped);
        bool clickedOnNode = IsLayerInteractible(room.layer) && GetParentWindow().parentProgram.clicked && scaledMousePos.X >= connectionInThisRoom.X-6 && scaledMousePos.X <= connectionInThisRoom.X+6 && scaledMousePos.Y >= connectionInThisRoom.Y-6 && scaledMousePos.Y <= connectionInThisRoom.Y+6 && (currentlyEditingNodeSourceRoom != null || currentlyHoveredRoom == room);
        intereactedWithNode |= clickedOnNode;
        // Gates break a lot rn so don't use them.
        if (roomConnection.GetDestinationRoom(flipped).Contains("GATE")) {
            return;
        }
        // This skips logic that would break if a connection is disconnected, without having to do extra math.
        else if (roomConnection.GetDestinationRoom(flipped) == "DISCONNECTED") {
            goto ThisConnectionIsDisconnected;
        }
        
        RoomData? connectedRoom = WorldData!.roomData.FirstOrDefault(x => x.name.ToUpper() == roomConnection.GetDestinationRoom(flipped));
        if (connectedRoom == default) {
            Utils.DebugLog($"Error finding room connection position from {roomConnection.GetSourceRoom(flipped)} to {roomConnection.GetDestinationRoom(flipped)}. Removed problematic connection.");
            if (flipped) {
                roomConnection.destinationRoom = "DISCONNECTED";
            }
            else {
                roomConnection.sourceRoom = "DISCONNECTED";
            }
            return;
        }
        Vector2 connectionInOtherRoomPosition = dragPosition + connectedRoom.devPosition*0.5f + roomConnection.GetDestinationPosition(flipped);

        // This code is for cutting a room connection
        Vector2 centerPoint = connectionInThisRoom + 0.5f*(connectionInOtherRoomPosition - connectionInThisRoom);
        if (IsLayerInteractible(room.layer) && scaledMousePos.X >= centerPoint.X-10 && scaledMousePos.X <= centerPoint.X+10 && scaledMousePos.Y >= centerPoint.Y-10 && scaledMousePos.Y <= centerPoint.Y+10) {
            if (GetParentWindow().parentProgram.clicked) {
                Utils.DebugLog("Added new connection to roomConnections");
                WorldData!.roomConnections.Add(new RoomConnection(roomConnection.destinationRoom, "DISCONNECTED", roomConnection.destinationPosition, roomConnection.destinationDir, roomConnection.destinationNodeIndex));
                Utils.DebugLog($"Added {WorldData!.roomConnections.Last()}");
                roomConnection.ClearDestinationInfo();
            }
            else {
                prepareToCutConnections.Add(roomConnection);
            }
        }

        // If the node was clicked on and is connected, disconnect the other end.
        if (clickedOnNode) {
            Utils.DebugLog("Added new connection to roomConnections");
            WorldData!.roomConnections.Add(new RoomConnection(roomConnection.GetDestinationRoom(flipped), "DISCONNECTED", roomConnection.GetDestinationPosition(flipped), roomConnection.GetDestinationDir(flipped), roomConnection.GetDestinationNodeIndex(flipped)));
            Utils.DebugLog($"Added {WorldData!.roomConnections.Last()}");
            if (flipped) {
                roomConnection.ClearSourceInfo();
            }
            else {
                roomConnection.ClearDestinationInfo();
            }
        }
        ThisConnectionIsDisconnected:
        // If this node was clicked on...
        if (clickedOnNode) {
            // ...and there is not currently a connection being drawn (and this node's room is being hovered over)...
            if (currentlyEditingNodeSourceRoom == null && currentlyHoveredRoom == room) {
                // ... then we set the node that is to be connected to this.
                currentlyHoveredRoom = null;
                if (flipped) {
                    roomConnection.sourceRoom = "DISCONNECTED";
                }
                else {
                    roomConnection.destinationRoom = "DISCONNECTED";
                }
                currentlyEditingNodeSourceRoom = roomConnection.GetSourceRoom(flipped); //room.name.ToUpper();
                grabbedConnectionIndex = roomConnection.GetSourceNodeIndex(flipped);
            }
            // ... and there is currently a connection already being drawn, ie a previous node from another room was already selected...
            else if (currentlyEditingNodeSourceRoom != null && room.name.ToUpper() != currentlyEditingNodeSourceRoom) {
                // Need to remove the connection, deleting it if it's now empty, then move the other connection's extra data to a new connection, and then copy this connection data over to the other connection.
                Tuple<string, Vector2, int, int> connectionData;
                if (roomConnection.sourceRoom.ToUpper() == room.name.ToUpper()) {
                    connectionData = new(roomConnection.sourceRoom, roomConnection.sourcePosition, roomConnection.sourceDir, roomConnection.sourceNodeIndex);
                    if (roomConnection.destinationRoom == "DISCONNECTED") {
                        WorldData!.roomConnections.Remove(roomConnection);
                        Utils.DebugLog($"Removed {roomConnection}");
                    }
                    else {
                        roomConnection.sourceRoom = "DISCONNECTED";
                    }
                }
                else {
                    connectionData = new(roomConnection.destinationRoom, roomConnection.destinationPosition, roomConnection.destinationDir, roomConnection.destinationNodeIndex);
                    if (roomConnection.sourceRoom == "DISCONNECTED") {
                        WorldData!.roomConnections.Remove(roomConnection);
                        Utils.DebugLog($"Removed {roomConnection}");
                    }
                    else {
                        roomConnection.destinationRoom = "DISCONNECTED";
                    }
                }

                var foundConnection = WorldData!.roomConnections.FirstOrDefault(x => x.sourceRoom.ToUpper() == currentlyEditingNodeSourceRoom.ToUpper() && x.sourceNodeIndex == grabbedConnectionIndex);
                if (foundConnection != default) {
                    if (foundConnection.destinationRoom != "DISCONNECTED") {
                        WorldData!.roomConnections.Add(new RoomConnection(foundConnection.destinationRoom, "DISCONNECTED", foundConnection.destinationPosition, foundConnection.destinationDir, foundConnection.destinationNodeIndex));
                    }
                    Utils.DebugLog($"The current connection is {roomConnection}, and the found connection is: {foundConnection}");
                    foundConnection.destinationRoom = connectionData.Item1;
                    foundConnection.destinationPosition = connectionData.Item2;
                    foundConnection.destinationDir = connectionData.Item3;
                    foundConnection.destinationNodeIndex = connectionData.Item4;
                }
                else {
                    Utils.DebugLog(currentlyEditingNodeSourceRoom.ToUpper() + " " + grabbedConnectionIndex);
                    foundConnection = WorldData!.roomConnections.First(x => x.destinationRoom.ToUpper() == currentlyEditingNodeSourceRoom.ToUpper() && x.destinationNodeIndex == grabbedConnectionIndex);
                    if (foundConnection.sourceRoom != "DISCONNECTED") {
                        WorldData!.roomConnections.Add(new RoomConnection(foundConnection.sourceRoom, "DISCONNECTED", foundConnection.sourcePosition, foundConnection.sourceDir, foundConnection.sourceNodeIndex));
                    }
                    Utils.DebugLog($"The current connection is {roomConnection}, and the found connection is: {foundConnection}");
                    foundConnection.sourceRoom = connectionData.Item1;
                    foundConnection.sourcePosition = connectionData.Item2;
                    foundConnection.sourceDir = connectionData.Item3;
                    foundConnection.sourceNodeIndex = connectionData.Item4;
                }
                Utils.DebugLog($"The connection is now {foundConnection}, was the connection removed? {!WorldData!.roomConnections.Any(x => x == roomConnection)}");
                currentlyEditingNodeSourceRoom = null;
                grabbedConnectionIndex = 0;
            }
        }
    }
    void OpenRoomMenu(RoomData room) {
        GetParentWindow().AddChild(new RoomMenu(scaledMousePos, new Vector2(300, 500), GetParentWindow(), room));
    }
    void OpenDenMenu(List<SpawnData> spawnData, string roomName) {
        Vector2 size = new Vector2(300, 312);
        if (spawnData.Count > 0 && GetParentWindow().updatables.FirstOrDefault(x => x is DenMenu denMenu && denMenu.spawnData.SequenceEqual(spawnData)) == default) {
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
            if (GetParentMainWindow().worldRenderer.viewSubregions) {
                var color = WorldData!.subregionColors[room.subregion];
                SDL.SDL_SetRenderDrawColor(renderer, (byte)Math.Max(0, color.r-bkgFillModifier), (byte)Math.Max(0, color.g-bkgFillModifier), (byte)Math.Max(0, color.b-bkgFillModifier), color.a);
            }
            else {
                SDL.SDL_SetRenderDrawColor(renderer, bkgFillModifier, bkgFillModifier, bkgFillModifier, 255);
            }
            SDL.SDL_RenderFillRectF(renderer, ref outline);
            SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
            SDL.SDL_RenderDrawRectF(renderer, ref outline);
            SDL.SDL_RenderCopyF(renderer, (IntPtr)room.roomTexture, (IntPtr)null, ref r);
        }
        else if (room.layer == Layers.Layer2) {
            SDL.SDL_SetRenderTarget(renderer, layer2Texture);
            if (GetParentMainWindow().worldRenderer.viewSubregions) {
                var color = WorldData!.subregionColors[room.subregion];
                SDL.SDL_SetRenderDrawColor(renderer, (byte)Math.Max(0, color.r-bkgFillModifier), (byte)Math.Max(0, color.g-bkgFillModifier), (byte)Math.Max(0, color.b-bkgFillModifier), color.a);
            }
            else {
                SDL.SDL_SetRenderDrawColor(renderer,bkgFillModifier, (byte)(255 - bkgFillModifier*0.5f), bkgFillModifier, 255);
            }
            SDL.SDL_RenderFillRectF(renderer, ref outline);
            SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
            SDL.SDL_RenderDrawRectF(renderer, ref outline);
            SDL.SDL_RenderCopyF(renderer, (IntPtr)room.roomTexture, (IntPtr)null, ref r);
        }
        else if (room.layer == Layers.Layer3) {
            SDL.SDL_SetRenderTarget(renderer, layer3Texture);
            if (GetParentMainWindow().worldRenderer.viewSubregions) {
                var color = WorldData!.subregionColors[room.subregion];
                SDL.SDL_SetRenderDrawColor(renderer, (byte)Math.Max(0, color.r-bkgFillModifier), (byte)Math.Max(0, color.g-bkgFillModifier), (byte)Math.Max(0, color.b-bkgFillModifier), color.a);
            }
            else {
                SDL.SDL_SetRenderDrawColor(renderer, (byte)(255 - bkgFillModifier*0.5f), bkgFillModifier, bkgFillModifier, 255);
            }
            SDL.SDL_RenderFillRectF(renderer, ref outline);
            SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
            SDL.SDL_RenderDrawRectF(renderer, ref outline);
            SDL.SDL_RenderCopyF(renderer, (IntPtr)room.roomTexture, (IntPtr)null, ref r);
        }
        
        List<RoomConnection> roomConnections = WorldData!.roomConnections.FindAll(x => x.sourceRoom.ToUpper() == room.name.ToUpper());
        List<bool> isSource = Enumerable.Repeat(false, roomConnections.Count).ToList();

        roomConnections.AddRange(WorldData!.roomConnections.FindAll(x => x.destinationRoom.ToUpper() == room.name.ToUpper()));
        isSource.AddRange(Enumerable.Repeat(true, roomConnections.Count-isSource.Count).ToList());

        for (int i = 0; i < roomConnections.Count; i++) {
            Vector2 connectionInThisRoom = dragPosition + room.devPosition*0.5f + roomConnections[i].GetSourcePosition(isSource[i]);
            // Gates break a lot rn so don't use them.
            if (roomConnections[i].GetDestinationRoom(isSource[i]).Contains("GATE")) {
                continue;
            }
            // If the room connection is being formed by the user, the other end of the connection should be at the mouse position.
            else if (room.name.ToUpper() == currentlyEditingNodeSourceRoom && roomConnections[i].GetSourceNodeIndex(isSource[i]) == grabbedConnectionIndex) {
                Utils.DrawGeometryWithVertices(renderer, connectionInThisRoom, biggerCircle.ToArray());
                SDL.SDL_RenderDrawLineF(renderer, connectionInThisRoom.X, connectionInThisRoom.Y, scaledMousePos.X, scaledMousePos.Y);
                continue;
            }

            if (roomConnections[i].GetDestinationRoom(isSource[i]) != "DISCONNECTED") {
                RoomData connectedRoom = WorldData!.roomData.First(x => x.name.ToUpper() == roomConnections[i].GetDestinationRoom(isSource[i]));
                Vector2 connectionInOtherRoomPosition = dragPosition + connectedRoom.devPosition*0.5f + roomConnections[i].GetDestinationPosition(isSource[i]);
                
                SDL.SDL_RenderDrawLineF(renderer, connectionInThisRoom.X, connectionInThisRoom.Y, connectionInOtherRoomPosition.X, connectionInOtherRoomPosition.Y);

                // This code is for cutting a room connection
                Vector2 centerPoint = connectionInThisRoom + 0.5f*(connectionInOtherRoomPosition - connectionInThisRoom);
                if (prepareToCutConnections.FirstOrDefault(x => x == roomConnections[i]) != default) {
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