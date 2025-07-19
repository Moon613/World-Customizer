using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using SDL2;

#nullable enable
namespace WorldCustomizer;
// To load the room images later, keep in mind that the y position in the "map_image_*.txt starts at the bottom of the region image, "map_*.png"
unsafe class WorldData {
    public IntPtr mapSurface;
    public List<RoomData> roomData;
    public string acronym;
    string fullParentDirName;
    public WorldData(string worldFolderPath, IntPtr renderer) {
        fullParentDirName = worldFolderPath;
        acronym = worldFolderPath.Substring(worldFolderPath.LastIndexOf(Path.DirectorySeparatorChar)+1);
        string roomsPath = worldFolderPath.Substring(0, worldFolderPath.LastIndexOf(Path.DirectorySeparatorChar)+1) + acronym + "-rooms";

        roomData = new();
        foreach (string roomFile in Directory.GetFiles(roomsPath).Where(x => x.EndsWith(".txt") && !x.Contains("settings"))) {
            string roomName = Path.GetFileNameWithoutExtension(roomFile);
            string[] devMapData = File.ReadAllText(worldFolderPath + Path.DirectorySeparatorChar + "map_" + acronym + ".txt").Split('\n');

            roomData.Add(new RoomData(this, roomName, File.ReadAllText(roomFile), devMapData.FirstOrDefault(x => x.Contains(roomName.ToUpper())) ?? null));
        }
    }
    /// <summary>
    /// Call this to correctly clean up memory used by this object, otherwise there *WILL* be mem leaks due to loaded textures.
    /// </summary>
    public void Destroy() {
        foreach (RoomData room in roomData) {
            room.Destroy();
        }
        SDL.SDL_FreeSurface(mapSurface);
    }
}
unsafe class RoomData {
    public Vector2 imageTexturePosition;
    public Vector2 devPosition;
    /// <summary>
    /// The size of the room in real interactible tiles on both axis
    /// </summary>
    public Vector2 size;
    /// <summary>
    /// The room name, whatever the file is named in the "-rooms" folder
    /// </summary>
    public string name;
    /// <summary>
    /// The layer that the room is on in the in-game slugcat view of the map.<br></br>0 is the top layer and 2 is the bottom layer
    /// </summary>
    public int layer;
    /// <summary>
    /// Water level, 0/-1 if not present.
    /// </summary>
    public int waterLevel;
    /// <summary>
    /// Water layer, 1 if on layer 1, 0 if on layer 2
    /// </summary>
    public int waterLayer;
    /// <summary>
    /// The CPU image of the room constructed from tile data
    /// </summary>
    public IntPtr roomSurface;
    /// <summary>
    /// This constructor creates an image of the room based on the room data, and fills in it's size
    /// </summary>
    public RoomData(WorldData worldData, string name, string roomFileData, string? devMapData) {
        this.name = name;

        // Go back 1 to account for the newline char
        int startOfGeoData = roomFileData.Length-1;
        // Set this to one further back so that it is not a newline char
        char currentChar = roomFileData[startOfGeoData-1];
        // These are the chars I expect in the geo data part of the room.
        char[] acceptableChars = ['|', ',', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'];
        while (acceptableChars.Contains(currentChar)) {
            startOfGeoData--;
            currentChar = roomFileData[startOfGeoData];
        }
        // This moves the start forward to avoid a newline char
        startOfGeoData++;

        // Separate the tile data into an array for each tile.
        string[] tileData = roomFileData.Substring(startOfGeoData).Split(['|'], StringSplitOptions.RemoveEmptyEntries).Where(x => !x.Contains('\n')).ToArray();

        // Get the room size in terms of the amount of tiles inside the interatible part of the room on each axis.
        string[] sizeAndWaterData = roomFileData.Substring(roomFileData.IndexOf('\n'), roomFileData.IndexOf('\n', roomFileData.IndexOf('\n')+1) - roomFileData.IndexOf('\n')).Split('|');
        size.X = Convert.ToInt32(sizeAndWaterData[0].Split('*')[0]);
        size.Y = Convert.ToInt32(sizeAndWaterData[0].Split('*')[1]);
        try {
            waterLevel = Convert.ToInt32(sizeAndWaterData[1]);
        } catch {
            waterLevel = 0;
        }
        try {
            waterLayer = Convert.ToInt32(sizeAndWaterData[2]);
        } catch  {
            waterLayer = 1;
        }

        // We have the minimum needs to create the room surface, so we make the default now and fill it with useful pixels in the next part.
        // Also here be endianness
        roomSurface = SDL.SDL_CreateRGBSurface(0, (int)size.X, (int)size.Y, 32, 0x000000FF, 0x0000FF00, 0x00FF0000, 0xFF000000);
        SDL.SDL_SetSurfaceBlendMode(roomSurface, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
        for (int i = 0; i < size.X; i++) {
            for (int j = 0; j < size.Y; j++) {
                var color = new SDL.SDL_Color(){r=255,g=0,b=0,a=255};
                int[] singleTileData = Array.ConvertAll(tileData[j + (int)size.Y*i].Split(','), x => Convert.ToInt32(x));

                if (singleTileData[0] == 1 || singleTileData[0] == 4) {
                    color.r = 0;
                    if (waterLayer == 1 && waterLayer > 0 && size.Y-j < waterLevel+2) {
                        color.b = 255;
                    }
                }
                else if (singleTileData[0] == 2 || singleTileData[0] == 3) {
                    color.r = 153;
                }
                else if (singleTileData[0] == 0 && singleTileData.Length >= 2) {
                    if (singleTileData[1] == 1 || singleTileData[1] == 2) {
                        color.r = 153;
                    }
                    if (waterLevel > 0 && size.Y-j < waterLevel+2) {
                        color.b = 255;
                    }
                }

                Utils.SetPixel(roomSurface, i, j, color);
            }
        }
        
        if (devMapData != null) {
            string[] devMapDataSplit = devMapData.Substring(devMapData.IndexOf(' ')+1).Split(["><"], StringSplitOptions.RemoveEmptyEntries);
            layer = Convert.ToInt32(devMapDataSplit[4]);
            devPosition = new Vector2(float.Parse(devMapDataSplit[2]), float.Parse(devMapDataSplit[3]));
        }
        else {
            layer = 0;
            devPosition = Vector2.Zero;
        }
        Utils.DebugLog(devMapData ?? "");

        // imageTexturePosition = new Vector2(100);
    }
    public void Destroy() {
        SDL.SDL_FreeSurface(roomSurface);
    }
    public override string ToString() {
        return name + $" {devPosition} {size} {layer}";
    }
}