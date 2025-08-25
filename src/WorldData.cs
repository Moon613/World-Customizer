using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using SDL2;

#nullable enable
namespace WorldCustomizer;
// To load the room images later, keep in mind that the y position in the "map_image_*.txt starts at the bottom of the region image, "map_*.png"
unsafe class WorldData {
    public List<RoomData> roomData;
    public string acronym;
    string fullParentDirName;
    public WorldData(string worldFolderPath, IntPtr renderer) {
        fullParentDirName = worldFolderPath;
        acronym = worldFolderPath.Substring(worldFolderPath.LastIndexOf(Path.DirectorySeparatorChar)+1);
        string roomsPath = worldFolderPath.Substring(0, worldFolderPath.LastIndexOf(Path.DirectorySeparatorChar)+1) + acronym + "-rooms";
        
        string worldFileData = File.ReadAllText(worldFolderPath + Path.DirectorySeparatorChar + "world_"+acronym+".txt");
        int creaturesStart = worldFileData.IndexOf("CREATURES");
        int creaturesEnd = worldFileData.IndexOf("END CREATURES");
        string[] creatureSpawns = worldFileData.Substring(creaturesStart+10, creaturesEnd-creaturesStart-11).Split('\n');
        int roomsStart = worldFileData.IndexOf("ROOMS");
        int roomsEnd = worldFileData.IndexOf("END ROOMS");
        string[] roomConnections = worldFileData.Substring(roomsStart+6, roomsEnd-roomsStart-7).Split('\n');

        roomData = new();
        foreach (string roomFile in Directory.GetFiles(roomsPath).Where(x => x.EndsWith(".txt") && !x.Contains("settings"))) {
            string roomName = Path.GetFileNameWithoutExtension(roomFile);
            string[] devMapData = File.ReadAllText(worldFolderPath + Path.DirectorySeparatorChar + "map_"+acronym+".txt").Split('\n');

            roomData.Add(new RoomData(this, roomName, File.ReadAllText(roomFile), devMapData.FirstOrDefault(x => x.Contains(roomName.ToUpper()+":")) ?? null, creatureSpawns.Where(x => x.Contains(roomName.ToUpper())).ToArray(), roomConnections.FirstOrDefault(x => x.StartsWith(roomName.ToUpper() + " :") || x.StartsWith(roomName.ToUpper() + ":"))));
        }
    }
    /// <summary>
    /// Call this to correctly clean up memory used by this object, otherwise there *WILL* be mem leaks due to loaded textures.
    /// </summary>
    public void Destroy() {
        foreach (RoomData room in roomData) {
            room.Destroy();
        }
    }
}
unsafe class RoomData {
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
    public WorldRenderer.Layers layer;
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
    public IntPtr? roomTexture;
    public IntPtr roomSurface;
    public List<Vector2> roomConnectionPositions;
    public List<string> roomConnections;
    public List<Vector2> creatureSpawnPositions;
    public List<SpawnData> creatureSpawnData;
    public Dictionary<int, int> creatureDenIndexToAbstractNodeMap;
    /// <summary>
    /// This constructor creates an image of the room based on the room data, and fills in it's size
    /// </summary>
    public RoomData(WorldData worldData, string name, string roomFileData, string? devMapData, string[] spawnData, string? roomConnections) {
        Utils.DebugLog(name);
        this.name = name;
        roomTexture = null;

        // Go back 1 to account for the newline char
        int startOfGeoData = roomFileData.Trim().Length-1;
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
        string[] tileData = roomFileData.Substring(startOfGeoData).Trim().Split(['|'], StringSplitOptions.RemoveEmptyEntries).Where(x => !x.Contains('\n')).ToArray();

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

        roomConnectionPositions = new();
        creatureSpawnPositions = new();
        creatureDenIndexToAbstractNodeMap = new();
        // We have the minimum needs to create the room surface, so we make the default now and fill it with useful pixels in the next part.
        // Also here be endianness
        roomSurface = SDL.SDL_CreateRGBSurface(0, (int)size.X, (int)size.Y, 32, 0x000000FF, 0x0000FF00, 0x00FF0000, 0xFF000000);
        SDL.SDL_SetSurfaceBlendMode(roomSurface, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
        try {
            for (int j = 0; j < size.Y; j++) {
                for (int i = 0; i < size.X; i++) {
                    var color = new SDL.SDL_Color(){r=255,g=0,b=0,a=255};
                    int[] singleTileData = Array.ConvertAll(tileData[j + (int)size.Y*i].Split(','), x => Convert.ToInt32(x));

                    if (singleTileData[0] == 1 || singleTileData[0] == 4) {
                        color.r = 0;
                        if (waterLayer == 1 && waterLayer > 0 && size.Y-j < waterLevel+2) {
                            color.b = 255;
                        }
                        if (singleTileData.Length >= 2 && singleTileData[1] == 5) {
                            color.r = 255;
                            color.g = 255;
                            creatureDenIndexToAbstractNodeMap.Add(creatureDenIndexToAbstractNodeMap.Count, creatureSpawnPositions.Count);
                            creatureSpawnPositions.Add(new Vector2(i, j));
                        }
                        if (singleTileData.Length >= 2 && singleTileData[1] == 4) {
                            color.r = 255;
                            color.b = 255;
                            roomConnectionPositions.Add(new Vector2(i, j));
                        }
                        else if (singleTileData.Length >= 3 && singleTileData[1] == 3 && singleTileData[2] == 4) {
                            color.r = 255;
                            color.b = 255;
                            roomConnectionPositions.Add(new Vector2(i, j));
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
                        if (singleTileData[1] == 4) {
                            color.r = 255;
                            color.b = 255;
                            roomConnectionPositions.Add(new Vector2(i, j));
                        }
                        if (singleTileData[1] == 5) {
                            color.r = 255;
                            color.g = 255;
                            creatureDenIndexToAbstractNodeMap.Add(creatureDenIndexToAbstractNodeMap.Count, creatureSpawnPositions.Count);
                            creatureSpawnPositions.Add(new Vector2(i, j));
                        }
                    }

                    Utils.SetPixel(roomSurface, i, j, color);
                }
            }
            for (int i = 0; i < creatureDenIndexToAbstractNodeMap.Count; i++) {
                creatureDenIndexToAbstractNodeMap[i] = creatureDenIndexToAbstractNodeMap[i] + roomConnectionPositions.Count;
            }
        } catch (Exception err) {
            Utils.DebugLog(err);
        }
        
        // Set the dev position if it exists. If it does not, the room gets the default (0,0)
        if (devMapData != null) {
            string[] devMapDataSplit = devMapData.Substring(devMapData.IndexOf(' ')+1).Split(["><"], StringSplitOptions.RemoveEmptyEntries);
            layer = Utils.ByteToLayer(Convert.ToByte(devMapDataSplit[4]));
            devPosition = new Vector2(float.Parse(devMapDataSplit[2]), -float.Parse(devMapDataSplit[3]));
        }
        else {
            layer = WorldRenderer.Layers.Layer1;
            devPosition = Vector2.Zero;
        }

        Utils.DebugLog(roomConnections ?? "");
        this.roomConnections = Enumerable.Repeat("DISCONNECTED", roomConnectionPositions.Count).ToList();
        if (roomConnections != null) {
            string[] roomsConnectedTo = roomConnections.Split(':')[1].Trim().Split(',');

            int i = 0;
            for (; i < roomsConnectedTo.Length && i < this.roomConnections.Count; i++) {
                this.roomConnections[i] = roomsConnectedTo[i].Trim();
            }
            if (i < this.roomConnections.Count) {
                Utils.DebugLog("ERROR, there may be extra room connections in the leditor file or you are missing some connections in your world file.\nMake sure to use DISCONNECTED if you want to leave some connections unused.");
            }
        }

        this.creatureSpawnData = new List<SpawnData>();
        for (int i = 0; i < spawnData.Length; i++) {
            Utils.DebugLog(spawnData[i]);
            // A list of slugcats that spawn the creature or lineage. null value indicates it spawns for any slugcat.
            List<string>? slugCatsThatSpawnThisCreature = null;
            // The pipe a creature can spawn out of. Since individual lines can contain different creatures
            // if they are not lineages, this is a list to acount for each creature. A lineage spawn will
            // only assign one value to the list in index 0.
            List<int> pipeNumber = new (0);
            // Determines if this is a lineage spawn or single-type creature spawn (although those can have multiple creatures in a single line though)
            bool isALineage = false;
            // The creature type of non-lineage creatures.
            List<string> critType = new ();
            // The tags for non-lineage creatures.
            List<string> tags = new (0);
            // The amount of non-lineage creatures that spawn.
            // This means that a single spawn line can spawn an arbitrary amount of the same creature, all from one den.
            List<string> count = new (0);
            // A list of different creature spawn data.
            // The first field is the creature type, the second any tags it may have (none is 0), and the third the chance of progressing
            // to the next creature in the lineage. null when the spawn data parsing detects it is not a lineage spawn.
            List<SpawnData.CreatureData>? lineageSpawns = null;

            // Extract the slugcats from the data
            if (spawnData[i].StartsWith("(")) {
                slugCatsThatSpawnThisCreature = new();

                // The index to start the substring from in the next step, which trims the parenthesis and "X-" from around the slugcat data.
                // It shouldn't be less than 1, thus Math.Max() is used, since if '-' is not found (indicating exclusive modifier) IndexOf()
                // returns -1 (plus one is 0 in this case, but that still causes issues in the next Substring()).
                int indexToStart = Math.Max(1, spawnData[i].Substring(0, spawnData[i].IndexOf(')')).IndexOf('-') + 1);
                // Extract the slugcats by reading the substring from the begining (exluding the exclusive spawn modifier "X-"), 
                // and spliting them with the char ','.
                string[] slugcats = spawnData[i].Substring(indexToStart, spawnData[i].IndexOf(')')-indexToStart).Split(',');
                
                // This checks for if the spawn is exclusive to the slugcats listed, meaning it will spawn for all slugcats EXCEPT the ones listed here.
                bool exclusive = spawnData[i].StartsWith("(X-");

                if (!exclusive) {
                    // Adds each slugcat listed to the slugcats that spawn this creature, by parsing the string into a 'SlugcatStats.Name' ExtEnum.
                    foreach (string cat in slugcats) {
                        slugCatsThatSpawnThisCreature.Add(cat);
                    }
                }
                else {
                    // Iterates over all the current registered slugcats, and adds the ones that are NOT in the 'slugcats' list of strings
                    foreach (string slug in Utils.registeredSlugcats) {
                        if (!slugcats.Contains(slug)) {
                            slugCatsThatSpawnThisCreature.Add(slug);
                        }
                    }
                }
                // The slugcat data has been parsed, so remove it from the data string.
                spawnData[i] = spawnData[i].Substring(spawnData[i].IndexOf(')')+1);
            }
            
            // Extract if it is a lineage spawn, and if it is do that parsing logic
            if (spawnData[i].StartsWith("LINEAGE")) {
                isALineage = true;
                spawnData[i] = spawnData[i].Substring("LINEAGE".Length).TrimStart().TrimStart(':').TrimStart();

                // Get what room the lineage is for. Holdover from copying from my older project, but makes the logic easier so I'm keeping it.
                string room = spawnData[i].Substring(0, Math.Min(spawnData[i].IndexOf(' '), spawnData[i].IndexOf(':')));
                spawnData[i] = spawnData[i].Substring(room.Length).TrimStart().TrimStart(':').TrimStart();

                // Get what den the lineage is for
                string denAsString = spawnData[i].Substring(0, Math.Min(spawnData[i].IndexOf(' '), spawnData[i].IndexOf(':')));
                pipeNumber.Add(int.Parse(denAsString));
                spawnData[i] = spawnData[i].Substring(denAsString.Length).TrimStart().TrimStart(':').TrimStart();

                // Get the creatures and their data (chance to progress to next creature, tags) for the lineage.
                string[] creaturesAndChance = spawnData[i].Split(',');
                lineageSpawns = new List<SpawnData.CreatureData>();
                for (int j = 0; j < creaturesAndChance.Length; j++) {
                    string[] singleCreatureData = creaturesAndChance[j].Trim().Split('-');
                    // Parse the data for tags. If the second string starts with a '{' it is tag data, so use that. Otherwise tag data does not exist,
                    // so set it to 0.
                    string lineageTags = singleCreatureData[1].StartsWith("{")? singleCreatureData[1].Trim(new char[]{'{','}'}) : "";
                    
                    // Parse the data for chance to progress to next creature. If the second string starts with a '{' it is tag data, so use the string in the third
                    // index. Otherwise the chance to progress is in the second string, so use that.
                    string chance = singleCreatureData[1].StartsWith("{")? singleCreatureData[2] : singleCreatureData[1];
                    lineageSpawns.Add(new SpawnData.CreatureData(SpawnData.ConvertAliasToName(singleCreatureData[0]), lineageTags, chance));
                }
            }
            // The logic for if it is a single creature spawn
            else {
                isALineage = false;
                // Get the room from the data, single creature per den spawn data.
                string room = spawnData[i].TrimStart().Substring(0, Math.Min(spawnData[i].IndexOf(' '), spawnData[i].IndexOf(':')));
                // Then trim the data to remove the room information once it has been parsed
                spawnData[i] = spawnData[i].Substring(room.Length).TrimStart().TrimStart(':').TrimStart();
                // Split the data of what creatures can spawn here. Each line for non-lineage spawns can hold multiple creatures, so extract them
                // into a list format.
                string[] creaturesThatSpawnInThisRoom = spawnData[i].Split(',');
                
                for (int j = 0; j < creaturesThatSpawnInThisRoom.Length; j++) {
                    // Copy the string so it is easier to work with, and trim it. Trim is needed because previous split step can leave whitespace
                    // at the start of the string, since the spawn data format allows spaces between creature data after the ',' char.
                    string individualRawCritData = creaturesThatSpawnInThisRoom[j].Trim();

                    // Extract the den the creature spawns from, convert it into an int, and remove it from the raw data.
                    string denAsString = individualRawCritData.Substring(0, Math.Max(individualRawCritData.IndexOf('-'), 1));
                    pipeNumber.Add(int.Parse(denAsString));
                    individualRawCritData = individualRawCritData.Substring(denAsString.Length).TrimStart().TrimStart('-').TrimStart();

                    // Splits the rest of the creature data into a list so that it is easier to work with.
                    // If this step was not taken, it would involve repeatedly checking the return value of IndexOf('-') to make sure
                    // there was no more data left, and how much had been read already.
                    List<string> singleCreatureDataToParse = individualRawCritData.Split('-').ToList();

                    // The creature type is always mandatory, and the first to appear, so the first string entry can be used without checks safely
                    critType.Add(singleCreatureDataToParse[0]);

                    // Utils.DebugLog(individualRawCritData + " ");
                    // singleCreatureDataToParse.ForEach(x => Utils.DebugLog(x));
                    // Check the length of the data list for this creature.
                    // 1 means that only creature data was included, tags and count were unspecified so they both get default values.
                    // 2 means that either tags or count data was included, but not both.
                    // 3 means that creature type, tags, and count were included. In this case, the positions of all data in the list
                    // can be safely assumed to be in the order listed prior
                    if (singleCreatureDataToParse.Count == 1) {
                        tags.Add("");
                        count.Add("1");
                    }
                    else if (singleCreatureDataToParse.Count == 2) {
                        // Check if the second entry is tags or count. Tag data always starts with the '{' char, so that can be used to detect tag data.
                        if (singleCreatureDataToParse[1].StartsWith("{")) {
                            tags.Add(singleCreatureDataToParse[1].Substring(1, singleCreatureDataToParse[1].IndexOf('}')-1));
                            count.Add("1");
                        }
                        // Otherwise, it was count data that was included.
                        else {
                            tags.Add("");
                            count.Add(singleCreatureDataToParse[1]);
                        }
                    }
                    else if (singleCreatureDataToParse.Count == 3) {
                        if (singleCreatureDataToParse[1].StartsWith("{")) {
                            tags.Add(singleCreatureDataToParse[1].Substring(1, singleCreatureDataToParse[1].IndexOf('}')-1));
                            count.Add(singleCreatureDataToParse[2]);
                        }
                        else {
                            tags.Add(singleCreatureDataToParse[2].Substring(1, singleCreatureDataToParse[2].IndexOf('}')-1));
                            count.Add(singleCreatureDataToParse[1]);
                        }
                    }
                }
            }
            // Use different constructors based on if the data is a lineage or individual spawns.
            if (isALineage) {
                this.creatureSpawnData.Add(new SpawnData(slugCatsThatSpawnThisCreature, pipeNumber[0], lineageSpawns!));
                Utils.DebugLog("Converted spawn data: " + this.creatureSpawnData.Last().ToString());
            }
            else {
                for (int j = 0; j < critType.Count; j++){
                    this.creatureSpawnData.Add(new SpawnData(slugCatsThatSpawnThisCreature, pipeNumber[j], SpawnData.ConvertAliasToName(critType[j]!), tags[j], count[j]));
                    Utils.DebugLog("Converted spawn data: " + this.creatureSpawnData.Last().ToString());
                }
            }
        }

        Utils.DebugLog("Dev map data: " + (devMapData ?? "") + "\n");

        SDL_image.IMG_SavePNG(roomSurface, Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "roomTextures" + Path.DirectorySeparatorChar + name.ToUpper()+".png");
    }
    public void Destroy() {
        SDL.SDL_FreeSurface(roomSurface);
        if (roomTexture != null) {
            SDL.SDL_DestroyTexture((IntPtr)roomTexture);
        }
    }
    public override string ToString() {
        return name + $" {devPosition} {size} {layer}";
    }
}
public class SpawnData {
    public SpawnData(List<string>? slugcats, int pipeNumber, string creatureType, string tags = "", string count = "1") {
        this.slugcats = slugcats;
        this.pipeNumber = pipeNumber;
        this.isALineage = false;
        this.creatureData = new CreatureData(creatureType, tags, count);
        this.lineageSpawns = new List<CreatureData>{new CreatureData("NONE", "", "0")};
    }
    public SpawnData(List<string>? slugcats, int pipeNumber, List<CreatureData> lineageSpawns) {
        this.slugcats = slugcats;
        this.pipeNumber = pipeNumber;
        this.isALineage = true;
        this.creatureData = new CreatureData("NONE", "", "1");
        this.lineageSpawns = lineageSpawns;
    }
    public SpawnData(SpawnData rhs) {
        if (rhs.slugcats != null) {
            this.slugcats = new();
            foreach (var cat in rhs.slugcats) {
                this.slugcats.Add(cat);
            }
        }
        else {
            this.slugcats = null;
        }
        this.pipeNumber = rhs.pipeNumber;
        this.isALineage = rhs.isALineage;
        this.creatureData = new CreatureData(rhs.creatureData);
        if (rhs.lineageSpawns != null) {
            this.lineageSpawns = new();
            foreach (var spawn in rhs.lineageSpawns) {
                this.lineageSpawns.Add(new CreatureData(spawn));
            }
        }
        else {
            this.lineageSpawns = new List<CreatureData>{new CreatureData("NONE", "", "0")};
        }
    }
    public override string ToString() {
        string toReturn = "";
        if (slugcats != null) {
            foreach (var scug in slugcats) {
                toReturn += scug + ",";
            }
        }
        else {
            toReturn += "Any Slugcat";
        }
        toReturn += "   ";
        toReturn += pipeNumber.ToString() + ",   ";
        if (!isALineage && creatureData != null) {
            if (creatureData.type != null) {
                toReturn += creatureData.type;
            }
            else {
                toReturn += "NONE";
            }
            toReturn += ",   ";
            toReturn += (creatureData.tags == ""? "No Tags" : creatureData.tags) + ",   ";
            toReturn += creatureData.countOrChance;
        }
        else {
            if (lineageSpawns != null) {
                foreach (CreatureData spawn in lineageSpawns) {
                    toReturn += spawn.type + ", " + spawn.tags + ", " + spawn.countOrChance + ".  ";
                }
            }
        }
        toReturn += ".   ";
        return toReturn;
    }
    public static string ConvertAliasToName(string alias) {
        return alias.ToLower() switch {
            "pink" or "pinklizard" => "PinkLizard",
            "green" or "greenlizard" => "GreenLizard",
            "blue" or "bluelizard" => "BlueLizard",
            "yellow" or "yellowLizard" => "YellowLizard",
            "white" or "whitelizard" => "WhiteLizard",
            "black" or "blacklizard" => "BlackLizard",
            "cyan" or "cyanlizard" => "Cyanlizard",
            "red" or "redlizard" => "RedLizard",
            "spider" => "Spider",
            "smallcentipede" or "small centipede" => "SmallCentipede",
            "centi" or "centipede" => "Centipede",
            "redcenti" or "red centi" or "red centipede" or "redcentipede" => "RedCentipede",
            "dropwig" or "drop wig" or "drop bug" or "dropbug" => "DropBug",
            "big spider" or "bigspider" => "BigSpider",
            "spitter spider" or "spitterspider" => "SpitterSpider",
            "egg bug" or "eggbug" => "EggBug",
            "salamander" => "Salamander",
            "leech" => "Leech",
            "sea leech" or "sealeech" => "SeaLeech",
            "jet fish" or "jetfish" => "JetFish",
            "snail" => "Snail",
            "lev" or "leviathan" or "big eel" or "bigeel" => "BigEel",
            "cicada a" or "cicadaa" => "CicadaA",
            "cicada b" or "cicadab" => "CicadaB",
            "vulture" => "Vulture",
            "king vulture" or "kingvulture" => "KingVulture",
            "needle" or "needle worm" or "bigneedle" or "big needle" or "bigneedleworm" => "BigNeedleWorm",
            "small needle" or "smallneedle" or "smallneedleworm" => "SmallNeedleWorm",
            "centiwing" => "Centiwing",
            "cicada" => "Cicada",
            "mimic" or "pole mimic" or "polemimic" => "PoleMimic",
            "tentacle" or "tentacle plant" or "tentacleplant" => "TentaclePlant",
            "scavenger" => "Scavenger",
            "mouse" or "lantern mouse" or "lanternmouse" => "LanternMouse",
            "worm" or "garbage worm" or "garbageworm" => "GarbageWorm",
            "miros" or "miros bird" or "mirosbird" => "MirosBird",
            "tube" or "tube worm" or "tubeworm" => "TubeWorm",
            "bro" or "brolonglegs" or "bro long legs" or "brotherlonglegs" => "BrotherLongLegs",
            "daddy" or "daddy long legs" or "daddylonglegs" => "DaddyLongLegs",
            "deer" => "Deer",
            "caramel" or "spitlizard" => "SpitLizard",
            "eel" or "eellizard" => "EelLizard",
            "strawberry" or "zooplizard" => "ZoopLizard",
            "aqua centi" or "aquacentipede" or "aqua centipede" or "aquapede" or "aquacenti" => "AquaCenti",
            "mother spider" or "motherspider" => "MotherSpider",
            "yeek" => "Yeek",
            "jungleleech" => "JungleLeech",
            "miros vulture" or "mirosvulture" => "MirosVulture",
            "elite" or "scavenger elite" or "elitescavenger" or "elite scavenger" or "scavengerelite" => "ScavengerElite",
            "terror" or "mother" or "motherlonglegs" or "mother long legs" or "terror long legs" or "terrorlonglegs" => "TerrorLongLegs",
            "inspector" => "Inspector",
            "train" or "trainlizard" => "TrainLizard",
            "fire bug" or "hellbug" or "hell bug" or "firebug" => "FireBug",
            "hunter" or "hunter daddy" or "hunterdaddy" => "HunterDaddy",
            "blizzard" or "blizard" or "blizzard lizard" or "blizzardlizard" => "BlizzardLizard",
            "indigo" or "indigo lizard" or "skink" or "indigolizard" => "IndigoLizard",
            "big moth" or "bigmoth" => "BigMoth",
            "small moth" or "smallmoth" => "SmallMoth",
            "frog" => "Frog",
            "barnacle" => "Barnacle",
            "seapig" or "tardigrade" => "Tardigrade",
            "sky whale" or "skywhale" => "SkyWhale",
            "firesprite" => "FireSprite",
            "drillcrab" => "DrillCrab",
            "sandgrub" => "SandGrub",
            "bigsandgrub" => "BigSandGrub",
            "boxworm" => "BoxWorm",
            "rattler" => "Rattler",
            "templar" or "scavenger templar" or "templarscavenger" or "templar scavenger" or "scavengertemplar" => "ScavengerTemplar",
            "disciple" or "scavengerdisciple" or "scavenger disciple" or "disciplescavenger" or "disciple scavenger" or "scavengerdisciple" => "ScavengerDisciple",
            "loach" => "Loach",
            "rotbehemoth" or "rot behemoth" or "rbehemoth" or "behemoth" or "bigrot" or "rotloach" => "RotLoach",
            "rat" => "Rat",
            _ => alias,
        };
    }
    public List<string>? slugcats;
    public readonly int pipeNumber;
    public bool isALineage;
    public CreatureData creatureData;
    public List<CreatureData> lineageSpawns;
    
    // A nested class to hold information about creature spawns. This helps organize data and reduce field clutter.
    public class CreatureData {
        public CreatureData(string type, string tags, string countOrChance) {
            this.type = type;
            this.tags = tags;
            this.countOrChance = countOrChance;
        }
        public CreatureData(CreatureData rhs) {
            this.type = rhs.type;
            this.tags = rhs.tags;
            this.countOrChance = rhs.countOrChance;
        }
        public string type;
        public string tags;
        public string countOrChance;
    }
}