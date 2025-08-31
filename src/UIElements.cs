using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SDL2;
using System.IO;
using System.Runtime.CompilerServices;

namespace WorldCustomizer;
#nullable enable

/// <summary>
/// The top menu bar, always present and provides a way to save, load, set preferences, ect.
/// </summary>
internal class OptionBar : FocusableUIElement, IRenderable {
    List<Button>? options;
    ContextMenu? contextMenu;
    new MainWindow parent;
    internal OptionBar(Vector2 size, MainWindow parent) : base(Vector2.Zero, size, parent) {
        this.parent = parent;
        contextMenu = null;
        options = null;
    }
    internal void AssignButtons(List<Button> buttons) {
        options = buttons;
    }
    internal new MainWindow GetParentWindow() {
        return parent.GetParentWindow();
    }
    public void Render(IntPtr window, IntPtr renderer) {        
        // Draw a background light grey rectangle
        SDL.SDL_SetRenderDrawColor(renderer, 122, 122, 122, 255);
        var rect = new SDL.SDL_FRect() {x = 0, y = 0, w = size.X, h = size.Y};
        SDL.SDL_RenderFillRectF(renderer, ref rect);

        foreach (Button opt in options!) {
            opt.Render(window, renderer);
        }

        // This draws the white border
        SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
        SDL.SDL_RenderDrawRectF(renderer, ref rect);

        // If the context menu is being displayed, then render it.
        contextMenu?.Render(window, renderer);
    }
    public override void Update() {
        base.Update();
        contextMenu?.Update();
        foreach (Button button in options!) {
            button.Update();
            if (button.text.StartsWith("Layer ")) {
                button.greyedOut = GetParentMainWindow().worldRenderer.viewSubregions;
            }
        }
    }
    public override void Signal(string text) {
        if (text == ContextMenu.RemoveCtxMenu) {
            contextMenu = null;
        }
    }
    public void OpenFileContextMenu(Button _) {
        Utils.DebugLog("Opened the File Context Menu");
        #pragma warning disable CS8604, IDE0090, IDE0028
        contextMenu = new ContextMenu(new Vector2(0, 32), this);
        List<Button> buttons = Button.CreateButtonsVertical(new List<Tuple<string, Action<Button>>>{
            new Tuple<string, Action<Button>>("Save", SaveToFile),
            new Tuple<string, Action<Button>>("Save As", new Action<Button>(_ => {Utils.DebugLog("Clicked on the Save As button");})),
            new Tuple<string, Action<Button>>("New", new Action<Button>(_ => {Utils.DebugLog("Clicked on the New button");})),
            new Tuple<string, Action<Button>>("Load", LoadFile)
        }, contextMenu, new Vector2(0, 0), 14, 5, 2);
        contextMenu.AssignButtons(buttons, new Vector2(5, 2));
        #pragma warning restore CS8604, IDE0090, IDE0028
    }
    public void SaveToFile(Button _) {
        if (GetParentWindow().parentProgram.currentWorld != null) {
            WorldData currentWorld = GetParentWindow().parentProgram.currentWorld!;
            string RegionFolder = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Saved_Regions" + Path.DirectorySeparatorChar + currentWorld.acronym.ToUpper();
            if (!Directory.Exists(RegionFolder)) {
                Directory.CreateDirectory(RegionFolder);
            }
            if (File.Exists(RegionFolder + Path.DirectorySeparatorChar + "world_"+currentWorld.acronym+".txt")) {
                File.WriteAllText(RegionFolder + Path.DirectorySeparatorChar + "world_"+currentWorld.acronym+".txt", "");
            }
            File.AppendAllText(RegionFolder + Path.DirectorySeparatorChar + "world_"+currentWorld.acronym+".txt", "ROOMS\n");
            foreach (var room in currentWorld.roomData) {
                File.AppendAllText(RegionFolder + Path.DirectorySeparatorChar + "world_"+currentWorld.acronym+".txt", room.name.ToUpper()+" : ");
                for (int i = 0; i < room.roomConnections.Count; i++) {
                    File.AppendAllText(RegionFolder + Path.DirectorySeparatorChar + "world_"+currentWorld.acronym+".txt", room.roomConnections[i].ToUpper()+(i<room.roomConnections.Count-1? ", " : "\n"));
                }
            }
            File.AppendAllText(RegionFolder + Path.DirectorySeparatorChar + "world_"+currentWorld.acronym+".txt", "END ROOMS\n\nCREATURES\n");
            foreach (var room in currentWorld.roomData) {
                for (int i = 0; i < room.creatureSpawnData.Count; i++) {
                    if ((!room.creatureSpawnData[i].isALineage && room.creatureSpawnData[i].creatureData.type == "NONE") || (room.creatureSpawnData[i].isALineage && room.creatureSpawnData[i].lineageSpawns.FirstOrDefault(x => x.type != "NONE") == default)) {
                        continue;
                    }
                    string s = "";
                    // Detect any slugcat-exclusive spawning rules.
                    if (room.creatureSpawnData[i].slugcats != null) {
                        s += "(";
                        if (room.creatureSpawnData[i].exclusive) {
                            s += "X-";
                        }
                        foreach (var cat in room.creatureSpawnData[i].slugcats!) {
                            s += cat.ToString() + ",";
                        }
                        s = s.TrimEnd(',');
                        s += ")";
                    }

                    // Lineage Spawns
                    if (room.creatureSpawnData[i].isALineage) {
                        s += "LINEAGE : ";
                        s += room.name.ToUpper() + " : ";
                        s += room.creatureSpawnData[i].pipeNumber + " : ";
                        foreach (var crit in room.creatureSpawnData[i].lineageSpawns!) {
                            s += crit.type.ToString();
                            if (crit.tags != "") {
                                s += "-{" + crit.tags + "}";
                            }
                            s += "-" + crit.countOrChance;
                            s += ", ";
                        }
                        s = s.TrimEnd();
                        s = s.TrimEnd(',');
                    }
                    // Single creature spawns
                    else {
                        if (File.Exists(RegionFolder + Path.DirectorySeparatorChar + "world_"+currentWorld.acronym+".txt"))
                        s += room.name.ToUpper() + " : ";
                        s += room.creatureSpawnData[i].pipeNumber + "-";
                        s += room.creatureSpawnData[i].creatureData!.type.ToString();
                        if (File.Exists(RegionFolder + Path.DirectorySeparatorChar + "world_"+currentWorld.acronym+".txt"))
                        if (room.creatureSpawnData[i].creatureData!.tags != "") {
                            s += "-{" + room.creatureSpawnData[i].creatureData!.tags + "}";
                        }
                        s += "-" + room.creatureSpawnData[i].creatureData!.countOrChance;
                    }
                    s += "\n";
                    File.AppendAllText(RegionFolder + Path.DirectorySeparatorChar + "world_"+currentWorld.acronym+".txt", s);
                }
            }
            File.AppendAllText(RegionFolder + Path.DirectorySeparatorChar + "world_"+currentWorld.acronym+".txt", "ENDCREATURES");
            File.AppendAllText(RegionFolder + Path.DirectorySeparatorChar + "world_"+currentWorld.acronym+".txt", "\n\nBAT MIGRATION BLOCKAGES\nEND BAT MIGRATION BLOCKAGES");
        }
    }
    public void LoadFile(Button _) {
        Signal(ContextMenu.RemoveCtxMenu);
        GetParentWindow().parentProgram.OpenFileBrowser();
    }
    public void OpenPreferencesContextMenu(Button _) {
        Utils.DebugLog("Clicked on the Preferences Tab");
        contextMenu = new ContextMenu(new Vector2(42, 32), this);

        List<Button> buttons = Button.CreateButtonsVertical(new List<Tuple<string, Action<Button>>>{
            new Tuple<string, Action<Button>>("Background Color", OpenBackgroundColorSelector),
            new Tuple<string, Action<Button>>("Slugcat Selector", OpenSlugcatSelection)
        }, contextMenu, new Vector2(0, 0), 14, 5, 2);

        contextMenu.AssignButtons(buttons, new Vector2(5, 2));
    }
    public void OpenBackgroundColorSelector(Button _) {
        Utils.DebugLog("Clicked the background color selector button");
        ColorSelector colorSelector = new ColorSelector(parent.size/2 - new Vector2(150, 157.5f), new Vector2(300, 315), parent);
        parent.AddChild(colorSelector);
    }
    public void OpenSlugcatSelection(Button _) {
        Utils.DebugLog("Opened the Slugcat Selection Menu");
        string[] slugcats = Utils.registeredSlugcats;
        SlugcatSelector slugcatSelector = new SlugcatSelector(parent.size/2 - new Vector2(85, (40+((slugcats.Length/3)+1)*50)/2), new Vector2(160, 40+((slugcats.Length/3)+1)*50), parent, slugcats);
        parent.AddChild(slugcatSelector);
    }
    internal void ToggleLayer(Button button) {
        if (button.text == "Layer 1" && GetParentWindow().worldRenderer is WorldRenderer worldRenderer1) {
            worldRenderer1.currentlyFocusedLayers ^= WorldRenderer.Layers.Layer1;
        }
        if (button.text == "Layer 2" && GetParentWindow().worldRenderer is WorldRenderer worldRenderer2) {
            worldRenderer2.currentlyFocusedLayers ^= WorldRenderer.Layers.Layer2;
        }
        if (button.text == "Layer 3" && GetParentWindow().worldRenderer is WorldRenderer worldRenderer3) {
            worldRenderer3.currentlyFocusedLayers ^= WorldRenderer.Layers.Layer3;
        }
    }
    internal void ToggleSubregionsView(Button _) {
        GetParentMainWindow().worldRenderer.viewSubregions = !GetParentMainWindow().worldRenderer.viewSubregions;
    }
}
class SlugcatSelector : Draggable, IRenderable {
    readonly List<ButtonWithImage> slugcatButtons;
    public SlugcatSelector(Vector2 position, Vector2 size, GenericUIElement parent, string[] slugcats) : base(position, size, parent) {
        slugcatButtons = new();
        for (int i = 0; i < slugcats.Length; i++) {
            slugcatButtons.Add(new ButtonWithImage(slugcats[i], slugcats[i]+".png", new Vector2(40), 0, this, new Vector2(10+(i*50)%150, 40+(i/3)*50), 40, 40, 0, new Vector2(-40, 0), true, SetSlugcat));
        }
    }
    public override void Render(IntPtr window, IntPtr renderer) {
        base.Render(window, renderer);
        foreach (ButtonWithImage button in slugcatButtons) {
            if (button.text == GetParentMainWindow().worldRenderer.selectedSlugcat) {
                var rect = new SDL.SDL_FRect(){x=button.Position.X, y=button.Position.Y, w=button.size.X, h=button.size.Y};
                SDL.SDL_SetRenderDrawColor(renderer, 39, 150, 214, alpha);
                SDL.SDL_RenderFillRectF(renderer, ref rect);
            }
            button.Render(window, renderer);
        }
    }
    public override void Update() {
        base.Update();
        if (GetParentWindow().IsFocused && GetParentMainWindow().currentlyFocusedObject == this) {
            foreach (ButtonWithImage button in slugcatButtons) {
                button.Update();
            }
        }
    }
    public void SetSlugcat(Button button) {
        GetParentWindow().updatables.RemoveAll(x => x is DenMenu);
        GetParentWindow().renderables.RemoveAll(x => x is DenMenu);
        GetParentMainWindow().worldRenderer.selectedSlugcat = button.text;
    }
}
/// <summary>
/// A small menu that pops up when clicking or right-clicking on a parent to provide options what to do.
/// </summary>
class ContextMenu : FocusableUIElement, IRenderable {
    List<Button>? options;
    internal bool focused;
    internal const string RemoveCtxMenu = "REMOVECTXMENU";
    internal const int GraceDistance = 5;
    internal ContextMenu(Vector2 position, GenericUIElement parent) : base(position, Vector2.Zero, parent) {
        this.focused = false;
    }
    public void AssignButtons(List<Button> buttons, Vector2 textOffset) {
        options = buttons;
        int totalHeight = 0;
        foreach (Button button in options) {
            SDL_ttf.TTF_SizeText(Utils.currentFont, button.text, out _, out int h);
            totalHeight += h;
        }
        SDL_ttf.TTF_SizeText(Utils.currentFont, options.Aggregate("", (max, cur) => max.Length > cur.text.Length ? max : cur.text), out int w, out _);
        int height = totalHeight + 2*options.Count*(int)textOffset.Y;
        int width = w + 2*(int)textOffset.X;
        size = new Vector2(width, height);
    }
    public void Render(IntPtr window, IntPtr renderer) {
        SDL.SDL_SetRenderDrawColor(renderer, 170, 170, 170, 255);
        var rect = new SDL.SDL_FRect() {x = Position.X, y = Position.Y, w = size.X, h = size.Y};
        SDL.SDL_RenderFillRectF(renderer, ref rect);

        // Draw each of the options
        foreach (Button opt in options!) {
            opt.Render(window, renderer);
        }
    }
    public override void Update() {
        base.Update();
        SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
        if (focused && (mouseX < Position.X-GraceDistance || mouseX > Position.X+size.X+GraceDistance || mouseY < Position.Y-GraceDistance || mouseY > Position.Y+size.Y+GraceDistance)) {
            Signal(RemoveCtxMenu);
        }
        if (!focused && mouseX >= Position.X-GraceDistance && mouseX < Position.X+size.X+GraceDistance && mouseY >= Position.Y-GraceDistance && mouseY <= Position.Y+size.Y+GraceDistance) {
            focused = true;
        }
        // This will set focused to true is the mouse is too far away, which then leads to the context menu closing because of the first conditional in this function.
        // This can't immediantly remove the menu, because if it did then it would despawn immediantly when opened due to the mouse being too far away.
        if (mouseX < Position.X-30 || mouseX > Position.X+size.X+30 || mouseY < Position.Y-30 || mouseY > Position.Y+size.Y+30) {
            focused = true;
        }
        foreach(Button button in options!) {
            button.Update();
        }
    }
    public override void Signal(string text) {
        if (parent is IAmInteractable interactable) {
            interactable.Signal(text);
        }
    }
}
class ColorSelector : Draggable {
    readonly Slider rSlider, gSlider, bSlider;
    readonly Button applyButton;
    readonly List<SDL.SDL_Vertex> verticies;
    readonly ColorBox colorBox;
    new internal WindowRenderCombo parent;
    const int RAD = 60;
    readonly ColorBox oldColor;
    internal ColorSelector(Vector2 position, Vector2 size, WindowRenderCombo parent) : base (position, size, parent) {
        this.parent = parent;
        rSlider = new Slider(new Vector2(20, 270), new Vector2(73, 20), this, 0, 255, true);
        gSlider = new Slider(new Vector2(113, 270), new Vector2(73, 20), this, 0, 255, true);
        bSlider = new Slider(new Vector2(206, 270), new Vector2(73, 20), this, 0, 255, true);
        this.colorBox = new ColorBox(new Vector2(size.X/2, size.Y*0.6f), new Vector2(60, 30), this, parent.backgroundColor);
        this.oldColor = new ColorBox(new Vector2(size.X/2-60, size.Y*0.6f), new Vector2(60, 30), this, parent.backgroundColor);
        applyButton = new Button("Apply", this, new Vector2(107, 240), 48, 19, 16, new Vector2(18, 2), true, ApplyColorToParentWindow);
        
        float horizontalPosDist = RAD;
        float verticalPosDist = 0.5f*RAD;
        verticies = [
            // Triangle 1: Red-Orange/Yellow
            new SDL.SDL_Vertex(){position=new SDL.SDL_FPoint(){x=0, y=-RAD}, color=new SDL.SDL_Color(){r=255, g=0, b=0, a=255}},
            new SDL.SDL_Vertex(){position=new SDL.SDL_FPoint(){x=horizontalPosDist, y=-verticalPosDist}, color=new SDL.SDL_Color(){r=255, g=255, b=0, a=255}},
            new SDL.SDL_Vertex(){position=new SDL.SDL_FPoint(){x=0, y=0}, color=new SDL.SDL_Color(){r=255, g=255, b=255, a=255}},
            // Triangle 2: Orange/Yellow-Green
            new SDL.SDL_Vertex(){position=new SDL.SDL_FPoint(){x=horizontalPosDist, y=-verticalPosDist}, color=new SDL.SDL_Color(){r=255, g=255, b=0, a=255}},
            new SDL.SDL_Vertex(){position=new SDL.SDL_FPoint(){x=horizontalPosDist, y=verticalPosDist}, color=new SDL.SDL_Color(){r=0, g=255, b=0, a=255}},
            new SDL.SDL_Vertex(){position=new SDL.SDL_FPoint(){x=0, y=0}, color=new SDL.SDL_Color(){r=255, g=255, b=255, a=255}},
            // Triangle 3: Green-Teal
            new SDL.SDL_Vertex(){position=new SDL.SDL_FPoint(){x=horizontalPosDist, y=verticalPosDist}, color=new SDL.SDL_Color(){r=0, g=255, b=0, a=255}},
            new SDL.SDL_Vertex(){position=new SDL.SDL_FPoint(){x=0, y=RAD}, color=new SDL.SDL_Color(){r=0, g=255, b=255, a=255}},
            new SDL.SDL_Vertex(){position=new SDL.SDL_FPoint(){x=0, y=0}, color=new SDL.SDL_Color(){r=255, g=255, b=255, a=255}},
            // Triangle 4: Teal-Blue
            new SDL.SDL_Vertex(){position=new SDL.SDL_FPoint(){x=0, y=RAD}, color=new SDL.SDL_Color(){r=0, g=255, b=255, a=255}},
            new SDL.SDL_Vertex(){position=new SDL.SDL_FPoint(){x=-horizontalPosDist, y=verticalPosDist}, color=new SDL.SDL_Color(){r=0, g=0, b=255, a=255}},
            new SDL.SDL_Vertex(){position=new SDL.SDL_FPoint(){x=0, y=0}, color=new SDL.SDL_Color(){r=255, g=255, b=255, a=255}},
            // Triangle 5: Blue-Purple
            new SDL.SDL_Vertex(){position=new SDL.SDL_FPoint(){x=-horizontalPosDist, y=verticalPosDist}, color=new SDL.SDL_Color(){r=0, g=0, b=255, a=255}},
            new SDL.SDL_Vertex(){position=new SDL.SDL_FPoint(){x=-horizontalPosDist, y=-verticalPosDist}, color=new SDL.SDL_Color(){r=255, g=0, b=255, a=255}},
            new SDL.SDL_Vertex(){position=new SDL.SDL_FPoint(){x=0, y=0}, color=new SDL.SDL_Color(){r=255, g=255, b=255, a=255}},
            // Triangle 6: Purple-Red
            new SDL.SDL_Vertex(){position=new SDL.SDL_FPoint(){x=-horizontalPosDist, y=-verticalPosDist}, color=new SDL.SDL_Color(){r=255, g=0, b=255, a=255}},
            new SDL.SDL_Vertex(){position=new SDL.SDL_FPoint(){x=0, y=-RAD}, color=new SDL.SDL_Color(){r=255, g=0, b=0, a=255}},
            new SDL.SDL_Vertex(){position=new SDL.SDL_FPoint(){x=0, y=0}, color=new SDL.SDL_Color(){r=255, g=255, b=255, a=255}}
        ];
    }
    public void ApplyColorToParentWindow(Button _) {
        parent.backgroundColor = new SDL.SDL_Color(){r=(byte)rSlider.value, g=(byte)gSlider.value, b=(byte)bSlider.value, a=255};
        oldColor.color = parent.backgroundColor;
    }
    public override void Render(IntPtr window, IntPtr renderer) {        
        base.Render(window, renderer);

        colorBox.color.r = (byte)rSlider.value;
        colorBox.color.g = (byte)gSlider.value;
        colorBox.color.b = (byte)bSlider.value;
        colorBox.color.a = alpha;
        colorBox.Render(window, renderer);
        oldColor.Render(window, renderer);

        rSlider.Render(window, renderer);
        gSlider.Render(window, renderer);
        bSlider.Render(window, renderer);
        Utils.WriteText(renderer, window, Math.Round(rSlider.value).ToString(), Utils.currentFont, rSlider.Position.X+25, rSlider.Position.Y+25, 16);
        Utils.WriteText(renderer, window, Math.Round(gSlider.value).ToString(), Utils.currentFont, gSlider.Position.X+25, gSlider.Position.Y+25, 16);
        Utils.WriteText(renderer, window, Math.Round(bSlider.value).ToString(), Utils.currentFont, bSlider.Position.X+25, bSlider.Position.Y+25, 16);
        applyButton.Render(window, renderer);
        Utils.DrawGeometryWithVertices(renderer, Position + new Vector2(size.X/2, RAD+RAD/2), verticies.ToArray());
        Vector2 calculated = new Vector2(0, -RAD)*(rSlider.value/255)
            + new Vector2(RAD, 0)*(gSlider.value/255)
            + new Vector2(-RAD, 0)*(bSlider.value/255)
            + new Vector2(0, RAD)*((gSlider.value+bSlider.value)/(255*2))
            + (Position + new Vector2(size.X/2, RAD+RAD/2));
        // Change the color of the center of the hexagon to show white/black
        byte blackWhiteVal = (byte)(0.2f*rSlider.value + 0.7f*gSlider.value + 0.1f*bSlider.value);
        for (int i = 2; i < verticies.Count; i += 3) {
            verticies[i] = verticies[i] with {color=new(){r=blackWhiteVal, g=blackWhiteVal, b=blackWhiteVal, a=alpha}};
        }

        SDL.SDL_SetRenderDrawColor(renderer, (byte)(255-blackWhiteVal), (byte)(255-blackWhiteVal), (byte)(255-blackWhiteVal), alpha);
        SDL.SDL_RenderDrawLineF(renderer, Position.X + size.X/2, Position.Y+RAD+RAD/2, calculated.X, calculated.Y);
    }
    public override void Update() {
        base.Update();
        if (GetParentWindow().IsFocused && GetParentMainWindow().currentlyFocusedObject == this) {
            rSlider.Update();
            gSlider.Update();
            bSlider.Update();
            applyButton.Update();
        }
    }
}
class Slider : GenericUIElement, IRenderable, IAmInteractable {
    Vector2 sliderPosition;
    Vector2 sliderSize;
    Vector2 mouseRelativeGrabPos;
    readonly float minSliderPos;
    readonly float maxSliderPos;
    readonly float min;
    readonly float max;
    internal float value;
    readonly bool horizontal;
    bool grabbed;
    internal Slider(Vector2 position, Vector2 size, GenericUIElement parent, float min, float max, bool horizontal) : base(position, size, parent) {
        this.min = min;
        this.max = max;
        value = min;
        grabbed = false;
        this.horizontal = horizontal;
        this.minSliderPos = 0;
        if (horizontal) {
            this.sliderPosition = new Vector2(0, 5);
            this.sliderSize = new Vector2(10, size.Y-10);
            this.maxSliderPos = size.X - sliderSize.X;
        }
        else {
            this.sliderPosition = new Vector2(5, 0);
            this.sliderSize = new Vector2(size.X-10, 10);
            this.maxSliderPos = size.Y - sliderSize.Y;
        }
    }
    public void Render(IntPtr window, IntPtr renderer) {
        SDL.SDL_SetRenderDrawColor(renderer, 122, 122, 122, 255);
        var r = new SDL.SDL_FRect(){x=Position.X, y=Position.Y, w=size.X, h=size.Y};
        SDL.SDL_RenderFillRectF(renderer, ref r);
        var sliderRect = new SDL.SDL_FRect(){x=(int)Position.X+sliderPosition.X, y=(int)Position.Y+sliderPosition.Y, w=(int)sliderSize.X, h=(int)sliderSize.Y};

        if (horizontal) {
            SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
            SDL.SDL_RenderDrawLineF(renderer, Position.X+5, Position.Y+size.Y/2, Position.X+size.X-5, Position.Y+size.Y/2);
            if (grabbed) {
                SDL.SDL_SetRenderDrawColor(renderer, 82, 82, 82, 255);
            }
            SDL.SDL_RenderFillRectF(renderer, ref sliderRect);
        }

        SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
        SDL.SDL_RenderDrawRectF(renderer, ref r);
    }
    public void Signal(string text) {
        
    }
    public void Update() {
        SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
        if (!grabbed && GetParentWindow().parentProgram.clicked && mouseX > Position.X+sliderPosition.X && mouseX < Position.X+sliderPosition.X+sliderSize.X && mouseY > Position.Y+sliderPosition.Y && mouseY < Position.Y+sliderPosition.Y+sliderSize.Y) {
            grabbed = true;
            mouseRelativeGrabPos = new Vector2(mouseX, mouseY) - sliderPosition;
        }
        if (grabbed && !GetParentWindow().parentProgram.mouseDown) {
            grabbed = false;
        }
        if (grabbed) {
            if (horizontal) {
                sliderPosition.X = Math.Max(minSliderPos, Math.Min(maxSliderPos, mouseX - mouseRelativeGrabPos.X));
                value = Utils.LerpMap(min, max, minSliderPos, maxSliderPos, sliderPosition.X);
            }
        }
    }
    internal override WindowRenderCombo GetParentWindow(){
        return parent!.GetParentWindow();
    }
}
class ColorBox : GenericUIElement, IRenderable {
    internal SDL.SDL_Color color;
    internal ColorBox(Vector2 position, Vector2 size, GenericUIElement parent, SDL.SDL_Color color) : base(position, size, parent) {
        this.color = color;
    }
    public void Render(IntPtr window, IntPtr renderer) {
        var rect = new SDL.SDL_FRect(){x=Position.X, y=Position.Y, w=size.X, h=size.Y};
        SDL.SDL_SetRenderDrawColor(renderer, color.r, color.g, color.b, color.a);
        SDL.SDL_RenderFillRectF(renderer, ref rect);

        SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
        SDL.SDL_RenderDrawRectF(renderer, ref rect);
    }
    internal override WindowRenderCombo GetParentWindow() {
        return parent!.GetParentWindow();
    }
}
class Button : GenericUIElement, IRenderable, IAmInteractable {
    internal event Action<Button> Clicked;
    internal string text;
    internal Vector2 textOffset;
    readonly int ptsize;
    readonly bool hasBorder;
    SDL.SDL_Color textColor;
    internal bool greyedOut;
    internal object? Data { get; set; }
    internal Button(string text, GenericUIElement? parent, Vector2 position, int width, int height, int ptsize, Vector2 textOffset, bool hasBorder, Action<Button> action, SDL.SDL_Color? textColor = null, bool greyedOut = false, object? extraData = null) : base(position, Vector2.Zero, parent) {
        this.text = text;
        this.size = new Vector2(width + 2*(int)textOffset.X, height + 2*(int)textOffset.Y);
        this.ptsize = ptsize;
        this.textOffset = textOffset;
        this.hasBorder = hasBorder;
        this.Clicked += action;
        this.textColor = (SDL.SDL_Color)((textColor==null) ? new SDL.SDL_Color(){r=255, g=255, b=255, a=255} : textColor);
        this.greyedOut = greyedOut;
        Data = extraData;
    }
    public virtual void Render(IntPtr window, IntPtr renderer) {
        SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
        var r = new SDL.SDL_FRect() {x = Position.X, y = Position.Y, w = size.X, h = size.Y};
        
        if (GetParentWindow().IsFocused && (GetParentWindow() is MainWindow mainWindow && mainWindow.currentlyFocusedObject == parent || GetParentWindow() is not MainWindow) && mouseX >= Position.X && mouseX < Position.X+size.X && mouseY >= Position.Y && mouseY < Position.Y+size.Y) {
            SDL.SDL_SetRenderDrawColor(renderer, 82, 82, 82, 255);
            SDL.SDL_RenderFillRectF(renderer, ref r);
        }
        if (hasBorder) {
            SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
            SDL.SDL_RenderDrawRectF(renderer, ref r);
        }
        Utils.WriteText(renderer, window, text, Utils.currentFont, (int)Position.X+(int)textOffset.X, (int)Position.Y+(int)textOffset.Y, ptsize, textColor);
        if (greyedOut) {
            SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 200);
            SDL.SDL_RenderFillRectF(renderer, ref r);
        }
    }
    public void Update() {
        SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
        if (!greyedOut && GetParentWindow().IsFocused && (GetParentWindow() is MainWindow mainWindow && mainWindow.currentlyFocusedObject == parent || GetParentWindow() is not MainWindow) && GetParentWindow().parentProgram.clicked && mouseX >= Position.X && mouseX < Position.X+size.X && mouseY >= Position.Y && mouseY < Position.Y+size.Y) {
            Clicked.Invoke(this);
        }
    }
    public void Signal(string text) {
        if (parent is IAmInteractable interactable) {
            interactable.Signal(text);
        }
    }
    public static List<Button> CreateButtonsVertical(List<Tuple<string, Action<Button>>> list, GenericUIElement parent, Vector2 position, int ptsize, int xOffset, int yOffset) {
        List<Button> buttonList = new();
        Vector2 drawPosition = position;
        SDL_ttf.TTF_SetFontSize(Utils.currentFont, ptsize);
        SDL_ttf.TTF_SizeText(Utils.currentFont, list.Aggregate("", (max, cur) => max.Length > cur.Item1.Length ? max : cur.Item1), out int width, out _);

        foreach (Tuple<string, Action<Button>> pair in list) {
            SDL_ttf.TTF_SizeText(Utils.currentFont, pair.Item1, out _, out int height);
            buttonList.Add(new Button(pair.Item1, parent, drawPosition, width, height, ptsize, new Vector2(xOffset, yOffset), false, pair.Item2));
            drawPosition.Y += height+2*yOffset;
        }

        return buttonList;
    }
    public static List<Button> CreateButtonsHorizontal(List<Tuple<string, Action<Button>>> list, GenericUIElement parent, Vector2 postition, int ptsize, int xOffset, int yOffset) {
        List<Button> buttonList = new();
        Vector2 drawPosition = postition;
        SDL_ttf.TTF_SetFontSize(Utils.currentFont, ptsize);
        int height = list.Aggregate(0, (max, cur) => {
                SDL_ttf.TTF_SizeText(Utils.currentFont, cur.Item1, out _, out int h);
                return max > h ? max : h;            
            });

        foreach (Tuple<string, Action<Button>> pair in list) {
            SDL_ttf.TTF_SizeText(Utils.currentFont, pair.Item1, out int width, out _);
            buttonList.Add(new Button(pair.Item1, parent, drawPosition, width, height, ptsize, new Vector2(xOffset, yOffset), false, pair.Item2));
            drawPosition.X += width+2*xOffset;
        }

        return buttonList;
    }
    internal override WindowRenderCombo GetParentWindow() {
        return parent.GetParentWindow();
    }
}
class ButtonWithImage : Button {
    internal string image;
    internal Vector2 imageSize;
    readonly float imageXOffset;
    internal ButtonWithImage(string text, string image, Vector2 imageSize, float imageXOffset, GenericUIElement? parent, Vector2 position, int width, int height, int ptsize, Vector2 textOffset, bool hasBorder, Action<Button> action, SDL.SDL_Color? color = null, object? extraData = null) : base(text, parent, position, width, height, ptsize, textOffset + new Vector2(imageSize.X, 0), hasBorder, action, color, extraData: extraData) {
        this.image = image;
        this.imageSize = imageSize;
        this.imageXOffset = imageXOffset;
    }
    public override void Render(IntPtr window, IntPtr renderer) {
        base.Render(window, renderer);
        IntPtr texture = SDL_image.IMG_LoadTexture(renderer, Utils.TexturesPath + image);
        if (texture == IntPtr.Zero) {
            Utils.DebugLog("Could not load image");
            Utils.DebugLog(SDL.SDL_GetError());
        }

        var rect = new SDL.SDL_FRect(){x=Position.X+imageXOffset, y=Position.Y+textOffset.Y/2, w=imageSize.X, h=imageSize.Y};
        if (SDL.SDL_RenderCopyF(renderer, texture, (IntPtr)null, ref rect) < 0) {
            Utils.DebugLog("Error Rendering Texture");
            Utils.DebugLog(SDL.SDL_GetError());
        }

        SDL.SDL_DestroyTexture(texture);
    }
}
class CheckBox : GenericUIElement, IRenderable, IAmInteractable {
    public bool active;
    public IntPtr activeImage;
    public IntPtr? inactiveImage;
    public CheckBox(Vector2 position, Vector2 size, GenericUIElement parent, bool active, string activeImage, string? inactiveImage = null) : base(position, size, parent) {
        this.active = active;
        this.activeImage = SDL_image.IMG_LoadTexture(GetParentWindow().renderer, Utils.TexturesPath + activeImage);
        this.inactiveImage = inactiveImage==null? null : SDL_image.IMG_LoadTexture(GetParentWindow().renderer, Utils.TexturesPath + inactiveImage);
    }
    ~CheckBox() {
        SDL.SDL_DestroyTexture(activeImage);
        if (inactiveImage != null) {
            SDL.SDL_DestroyTexture((nint)inactiveImage);
        }
    }
    public void Render(nint window, nint renderer) {
        SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
        var rect = new SDL.SDL_FRect(){x=Position.X, y=Position.Y, w=size.X, h=size.Y};
        if (GetParentWindow().IsFocused && (GetParentWindow() is MainWindow mainWindow && mainWindow.currentlyFocusedObject == parent || GetParentWindow() is not MainWindow) && mouseX >= Position.X && mouseX < Position.X+size.X && mouseY >= Position.Y && mouseY < Position.Y+size.Y) {
            SDL.SDL_SetRenderDrawColor(renderer, 82, 82, 82, 255);
            SDL.SDL_RenderFillRectF(renderer, ref rect);
        }
        SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
        SDL.SDL_RenderDrawRectF(renderer, ref rect);
        if (active) {
            SDL.SDL_RenderCopyF(renderer, activeImage, (IntPtr)null, ref rect);
        }
        else if (inactiveImage != null) {
            SDL.SDL_RenderCopyF(renderer, (nint)inactiveImage, (IntPtr)null, ref rect);
        }
    }
    public void Signal(string text) {
        throw new NotImplementedException();
    }
    public void Update() {
        SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
        if (GetParentWindow().IsFocused && (GetParentWindow() is MainWindow mainWindow && mainWindow.currentlyFocusedObject == parent || GetParentWindow() is not MainWindow) && GetParentWindow().parentProgram.clicked && mouseX >= Position.X && mouseX < Position.X+size.X && mouseY >= Position.Y && mouseY < Position.Y+size.Y) {
            active = !active;
        }
    }
    internal override WindowRenderCombo GetParentWindow() {
        return parent!.GetParentWindow();
    }
}
class ScrollButton : GenericUIElement, IRenderable, IAmInteractable {
    public float currentValue, min, max;
    Vector2 textOffset;
    readonly int ptsize;
    readonly string format;
    SDL.SDL_Color textColor;
    float increaseBy;
    public ScrollButton(Vector2 position, Vector2 size, GenericUIElement parent, float min, float max, Vector2 textOffset, int ptsize, string format, float? currentValue = null, float? increaseBy = null, SDL.SDL_Color? textColor = null) : base(position, size, parent) {
        this.currentValue = currentValue ?? min;
        this.min = min;
        this.max = max;
        this.textOffset = textOffset;
        this.ptsize = ptsize;
        this.format = format;
        this.textColor = textColor ?? new SDL.SDL_Color(){r=255, g=255, b=255, a=255};
        this.increaseBy = increaseBy ?? 1;
    }
    public void Render(nint window, nint renderer) {
        SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
        var r = new SDL.SDL_FRect() {x = Position.X, y = Position.Y, w = size.X, h = size.Y};
        
        if (GetParentWindow().IsFocused && (GetParentWindow() is MainWindow mainWindow && mainWindow.currentlyFocusedObject == parent || GetParentWindow() is not MainWindow) && mouseX >= Position.X && mouseX < Position.X+size.X && mouseY >= Position.Y && mouseY < Position.Y+size.Y) {
            SDL.SDL_SetRenderDrawColor(renderer, 82, 82, 82, 255);
            SDL.SDL_RenderFillRectF(renderer, ref r);
        }
        SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
        SDL.SDL_RenderDrawRectF(renderer, ref r);
        Utils.WriteText(renderer, window, Math.Round(currentValue, 2).ToString(format), Utils.currentFont, (int)Position.X+(int)textOffset.X, (int)Position.Y+(int)textOffset.Y, ptsize, textColor);
    }
    public void Signal(string text) {
        throw new NotImplementedException();
    }
    public void Update() {
        SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
        if (GetParentWindow().IsFocused && (GetParentWindow() is MainWindow mainWindow && mainWindow.currentlyFocusedObject == parent || GetParentWindow() is not MainWindow) && mouseX >= Position.X && mouseX < Position.X+size.X && mouseY >= Position.Y && mouseY < Position.Y+size.Y) {
            if (GetParentWindow().parentProgram.scrollY != 0) {
                currentValue = Math.Max(min, Math.Min(max, currentValue+(increaseBy*GetParentWindow().parentProgram.scrollY)));
            }
            if (GetParentWindow().parentProgram.clicked) {
                currentValue = Math.Max(min, float.MinValue);
            }
            else if (GetParentWindow().parentProgram.rightClicked) {
                currentValue = Math.Min(max, float.MaxValue);
            }
        }
    }
    internal override WindowRenderCombo GetParentWindow() {
        return parent!.GetParentWindow();
    }
}
class TextField : GenericUIElement, IAmInteractable, IRenderable {
    public string text;
    int ptsize, currentStartChar, currentEditChar;
    float verticalTextOffset;
    public TextField(Vector2 position, Vector2 size, GenericUIElement parent, string text, int ptsize, float verticalTextOffset) : base(position, size, parent) {
        this.text = text;
        this.ptsize = ptsize;
        currentStartChar = 0;
        currentEditChar = 0;
        this.verticalTextOffset = verticalTextOffset;
    }
    public void Render(nint window, nint renderer) {
        string textToDraw;
        if (GetParentWindow().currentlyEditedTextField == this) {
            textToDraw = currentEditChar==text.Length? text+"|" : text.Insert(currentEditChar+1, "|");
        }
        else {
            textToDraw = text;
        }
        textToDraw = textToDraw.Substring(currentStartChar);

        // SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
        var rect = new SDL.SDL_FRect(){x=Position.X, y=Position.Y, w=size.X, h=size.Y};

        SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
        SDL.SDL_RenderDrawRectF(renderer, ref rect);
        // if (GetParentWindow().IsFocused && (GetParentWindow() is MainWindow mainWindow && mainWindow.currentlyFocusedObject == parent || GetParentWindow() is not MainWindow) && mouseX >= Position.X && mouseX < Position.X+size.X && mouseY >= Position.Y && mouseY < Position.Y+size.Y) {
        //     SDL.SDL_SetRenderDrawColor(renderer, 82, 82, 82, 255);
        //     SDL.SDL_RenderFillRectF(renderer, ref rect);
        // }

        SDL_ttf.TTF_SetFontSize(Utils.currentFont, ptsize);
        SDL_ttf.TTF_SizeText(Utils.currentFont, textToDraw, out int textWidth, out _);
        // If the text width is bigger than the TextField's length, then it needs to be cut off.
        if (textWidth > size.X) {
            float percent = size.X / textWidth;
            int endIndex = (int)(textToDraw.Length * percent);
            // This Math.Min is just to prevent any Out of Bounds exceptions.
            textToDraw = textToDraw.Substring(0, Math.Min(textToDraw.Length, endIndex));
        }

        Utils.WriteText(renderer, window, textToDraw, Utils.currentFont, Position.X, Position.Y+verticalTextOffset, ptsize);
    }
    public void Signal(string text) {
        throw new NotImplementedException();
    }
    public void Update() {
        SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
        if (GetParentWindow().currentlyEditedTextField != this && GetParentWindow().parentProgram.clicked && mouseX >= Position.X && mouseX <= Position.X+size.X && mouseY >= Position.Y && mouseY <= Position.Y+size.Y) {
            GetParentWindow().currentlyEditedTextField = this;
            currentEditChar = text.Length-1;
            SDL_ttf.TTF_SetFontSize(Utils.currentFont, ptsize);
            SDL_ttf.TTF_SizeText(Utils.currentFont, text, out int textWidth, out int _);
            float percent = size.X / textWidth;
            int index = (int)(percent * text.Length);
            currentStartChar = Math.Max(0, text.Length-index);
        }
        if (GetParentWindow().currentlyEditedTextField != this) {
            currentEditChar = 0;
            currentStartChar = 0;
        }
        SDL.SDL_Keycode? pressedKey = GetParentWindow().parentProgram.pressedKey;
        if (GetParentWindow().currentlyEditedTextField == this && pressedKey != null) {
            SDL_ttf.TTF_SetFontSize(Utils.currentFont, ptsize);
            SDL_ttf.TTF_SizeText(Utils.currentFont, text, out int textWidth, out int _);
            if (pressedKey == SDL.SDL_Keycode.SDLK_BACKSPACE && text.Length > 0) {
                text = text.Substring(0, currentEditChar) + text.Substring(Math.Min(currentEditChar+1, text.Length));
                currentStartChar = Math.Max(currentStartChar-1, 0);
                currentEditChar = Math.Max(currentEditChar-1, -1);
            }
            else if (pressedKey >= SDL.SDL_Keycode.SDLK_SPACE && pressedKey <= SDL.SDL_Keycode.SDLK_z) {
                text = currentEditChar==text.Length? text+(char)pressedKey.Value : (currentEditChar==-1? (char)pressedKey.Value+text : text.Insert(currentEditChar+1, ((char)pressedKey.Value).ToString()));
                if (textWidth >= size.X) {
                    currentStartChar++;
                }
                currentEditChar++;
            }
            else if (pressedKey == SDL.SDL_Keycode.SDLK_LEFT) {
                currentEditChar = Math.Max(currentEditChar-1, -1);
                if (currentEditChar < currentStartChar && currentEditChar >= 0) {
                    currentStartChar = currentEditChar;
                }
            }
            else if (pressedKey == SDL.SDL_Keycode.SDLK_RIGHT) {
                currentEditChar = Math.Min(currentEditChar+1, text.Length-1);
                int amountOfDisplayedChars = (int)(text.Length*(size.X/textWidth))-2;
                if (currentEditChar > amountOfDisplayedChars && currentStartChar < text.Length-amountOfDisplayedChars && currentEditChar != text.Length-1) {
                    currentStartChar++;
                }
            }
        }
    }
    internal override WindowRenderCombo GetParentWindow() {
        return parent!.GetParentWindow();
    }
}
class RoomMenu : Draggable {
    RoomData roomData;
    ScrollButton layerSelect;
    TextField subregion;
    public RoomMenu(Vector2 position, Vector2 size, GenericUIElement parent, RoomData roomData) : base(position, size, parent) {
        this.roomData = roomData;
        this.layerSelect = new ScrollButton(new Vector2(70, 40), new Vector2(20, 25), this, 1, 3, new Vector2(5), 16, "0", Utils.LayerToByte(roomData.layer)+1, 1);
        this.subregion = new TextField(new Vector2(10, 95), new Vector2(size.X-20, 20), this, roomData.subregion, 16, 2.5f);
    }
    public override void Render(IntPtr window, IntPtr renderer) {
        base.Render(window, renderer);
        Utils.WriteText(renderer, window, roomData.name.ToUpper(), Utils.currentFont, Position.X+5, Position.Y+5, 16, new SDL.SDL_Color(){r=255, g=255, b=255, a=alpha});
        Utils.WriteText(renderer, window, "Layer:", Utils.currentFont, Position.X+10, Position.Y+42.5f, 16, new SDL.SDL_Color(){r=255, g=255, b=255, a=alpha});
        layerSelect.Render(window, renderer);
        Utils.WriteText(renderer, window, "Subregion:", Utils.currentFont, Position.X+10, Position.Y+75, 16);
        subregion.Render(window, renderer);
    }
    public override void Update() {
        base.Update();
        layerSelect.Update();
        roomData.layer = Utils.ByteToLayer((byte)(layerSelect.currentValue-1));
        subregion.Update();
        roomData.subregion = subregion.text;
        if (!GetParentWindow().parentProgram.currentWorld!.subregionColors.ContainsKey(roomData.subregion)) {
            GetParentWindow().parentProgram.currentWorld!.subregionColors.Add(roomData.subregion, new SDL.SDL_Color(){r=(byte)Utils.RNG.Next(128, 256), g=(byte)Utils.RNG.Next(128, 256), b=(byte)Utils.RNG.Next(128, 256), a=255});
        }
    }
}
class DenMenu : Draggable {
    internal List<SpawnData> spawnData;
    readonly string roomName;
    private int currentSpawnIndex;
    Button prevButton, nextButton, deleteButton, newButton, addLineageSpawn, deleteLineageSpawn;
    internal ButtonWithImage singleCreatureSelect;
    internal ScrollButton singleCreatureCount;
    internal TextField singleCreatureTags;
    internal List<ButtonWithImage> lineageCreatureSelects;
    internal List<ScrollButton> lineageCreatureProgressions;
    internal List<TextField> lineageTags;
    Button[] slugcatButtons;
    CheckBox lineageToggle, exclusiveToggle;
    int heightForBelowSlugcatButtons;
    readonly static Vector2 buttonTextOffset = new Vector2(5, 6);
    const int StartHeightOfSlugcatButtons = 90;
    const int SlugButtonRowHeight = 35;
    const int SlugButtonHeight = 16;
    const int LineageCreatureButtonWidth = 220;
    public DenMenu(Vector2 position, Vector2 size, GenericUIElement parent, List<SpawnData> spawnData, string roomName) : base(position, size, parent) {
        this.spawnData = spawnData;
        this.roomName = roomName;
        currentSpawnIndex = 0;
        prevButton = new Button("Previous Spawn", this, new Vector2(10, 25), 125, 15, 16, new Vector2(5), true, ChangeCurrentSpawn);
        nextButton = new Button("Next Spawn", this, new Vector2(150, 25), 90, 15, 16, new Vector2(5), true, ChangeCurrentSpawn);
        deleteButton = new Button("Delete", this, new Vector2(135, 55), 55, 15, 16, new Vector2(5), true, DeleteSpawn);
        newButton = new Button("New", this, new Vector2(210, 55), 30, 15, 16, new Vector2(10, 5), true, NewSpawn);
        exclusiveToggle = new CheckBox(new Vector2(100, 55), new Vector2(25, 25), this, spawnData[currentSpawnIndex].exclusive, "checkmark.png", "x.png");
        slugcatButtons = new Button[Utils.registeredSlugcats.Length];
        for (int i = 0; i < Utils.registeredSlugcats.Length; i++) {
            slugcatButtons[i] = new Button(Utils.registeredSlugcats[i].Substring(0, 2), this, new Vector2((10+i*40)%280, StartHeightOfSlugcatButtons+(i/7*SlugButtonRowHeight)), 19, SlugButtonHeight, 16, new Vector2(5, 5), true, UpdateSelectedSlugcats, extraData: Utils.registeredSlugcats[i]);
        }
        heightForBelowSlugcatButtons = StartHeightOfSlugcatButtons+((Utils.registeredSlugcats.Length-1)/7*SlugButtonRowHeight)+25+SlugButtonHeight;
        lineageToggle = new CheckBox(new Vector2(175, heightForBelowSlugcatButtons-2.5f), new Vector2(25), this, spawnData[0].isALineage, "checkmark.png", "x.png");

        lineageCreatureProgressions = new();
        lineageCreatureSelects = new();
        lineageTags = new();
        Vector2 noneImageSize = Utils.GetImageSize(Utils.TexturesPath+"NONE.png");
        if (!spawnData[currentSpawnIndex].isALineage) {
            Vector2 imageSize = Utils.GetImageSize(Utils.TexturesPath+spawnData[currentSpawnIndex].creatureData.type+".png");
            float scaledImageX = imageSize.X*(20f/imageSize.Y);

            singleCreatureSelect = new ButtonWithImage(spawnData[currentSpawnIndex].creatureData.type, spawnData[currentSpawnIndex].creatureData.type+".png", new Vector2(scaledImageX, 20), 2.5f, this, new Vector2(135, heightForBelowSlugcatButtons+31), (int)(150-2*scaledImageX), 15, 15, buttonTextOffset, true, OpenCreatureSelector, extraData: -1);

            singleCreatureCount = new ScrollButton(new Vector2(80, heightForBelowSlugcatButtons+60), new Vector2(30, 20), this, 1, int.MaxValue, new Vector2(5,2.5f), 16, "0", int.Parse(spawnData[currentSpawnIndex].creatureData.countOrChance));

            singleCreatureTags = new TextField(new Vector2(10, heightForBelowSlugcatButtons+105), new Vector2(280, 30), this, spawnData[currentSpawnIndex].creatureData.tags, 17, 5);

            lineageCreatureSelects.Add(new ButtonWithImage("NONE", "NONE.png", new Vector2(noneImageSize.X*(20f/noneImageSize.Y), 20), 2.5f, this, new Vector2(60, heightForBelowSlugcatButtons+40), (int)(LineageCreatureButtonWidth-2*noneImageSize.X), 15, 15, buttonTextOffset, true, OpenCreatureSelector, extraData: 0));
            lineageCreatureProgressions.Add(new ScrollButton(new Vector2(10, heightForBelowSlugcatButtons+42.5f), new Vector2(45, 20), this, 0, 1, new Vector2(5, 2.5f), 16, "F2", 0, 0.01f));
            lineageTags.Add(new TextField(new Vector2(10, heightForBelowSlugcatButtons+70), new Vector2(279, 30), this, "", 17, 5));
            size.Y = 312;
        }
        else {
            singleCreatureSelect = new ButtonWithImage("NONE", "NONE.png", new Vector2(noneImageSize.X*(20f/noneImageSize.Y), 20), 2.5f, this, new Vector2(135, heightForBelowSlugcatButtons+31), (int)(150-2*noneImageSize.X), 15, 15, buttonTextOffset, true, OpenCreatureSelector, extraData: -1);
            singleCreatureCount = new ScrollButton(new Vector2(80, heightForBelowSlugcatButtons+60), new Vector2(30, 20), this, 1, int.MaxValue, new Vector2(5,2.5f), 16, "0");
            singleCreatureTags = new TextField(new Vector2(10, heightForBelowSlugcatButtons+105), new Vector2(280, 30), this, "", 17, 5);

            for (int i = 0; i < spawnData[currentSpawnIndex].lineageSpawns.Count; i++) {
                Vector2 imageSize = Utils.GetImageSize(Utils.TexturesPath+spawnData[currentSpawnIndex].lineageSpawns[i].type+".png");
                float scaledImageX = imageSize.X*(20f/imageSize.Y);

                lineageCreatureSelects.Add(new ButtonWithImage(spawnData[currentSpawnIndex].lineageSpawns[i].type, spawnData[currentSpawnIndex].lineageSpawns[i].type+".png", new Vector2(scaledImageX, 20), 2.5f, this, new Vector2(60, heightForBelowSlugcatButtons+40+i*65), (int)(LineageCreatureButtonWidth-2*scaledImageX), 15, 15, buttonTextOffset, true, OpenCreatureSelector, extraData: i));

                lineageCreatureProgressions.Add(new ScrollButton(new Vector2(10, heightForBelowSlugcatButtons+42.5f+i*65), new Vector2(45, 20), this, 0, 1, new Vector2(5,2.5f), 16, "F2", float.Parse(spawnData[currentSpawnIndex].lineageSpawns[i].countOrChance), 0.01f));

                lineageTags.Add(new TextField(new Vector2(10, heightForBelowSlugcatButtons+70+i*65), new Vector2(279, 30), this, spawnData[currentSpawnIndex].lineageSpawns[i].tags, 17, 5));
            }
            this.size.Y = 312 + 65*(spawnData[currentSpawnIndex].lineageSpawns.Count-1);
        }
        addLineageSpawn = new Button("+", this, new Vector2(size.X/2 + 15, heightForBelowSlugcatButtons+40+(lineageCreatureSelects.Count)*65+10), 10, 15, 18, new Vector2(5, 2.5f), true, AddLineageSpawn);
        deleteLineageSpawn = new Button("-", this, new Vector2(size.X/2 - 25, heightForBelowSlugcatButtons+40+(lineageCreatureSelects.Count)*65+10), 10, 15, 18, new Vector2(5, 2.5f), true, DeleteLineageSpawn, greyedOut: lineageCreatureSelects.Count <= 1);
    }
    public override void Render(nint window, nint renderer) {
        base.Render(window, renderer);
        Utils.WriteText(renderer, window, roomName.ToUpper() + ", Den: " + spawnData[currentSpawnIndex].pipeNumber, Utils.currentFont, Position.X+5, Position.Y+5, 16, new SDL.SDL_Color(){r=255, g=255, b=255, a=alpha});
        prevButton.Render(window, renderer);
        deleteButton.Render(window, renderer);
        newButton.Render(window, renderer);
        nextButton.Render(window, renderer);
        Utils.WriteText(renderer, window, "Exclusive:", Utils.currentFont, Position.X+10, Position.Y+60, 17);
        exclusiveToggle.Render(window, renderer);
        foreach (Button button in slugcatButtons) {
            var rect = new SDL.SDL_FRect(){x=button.Position.X, y=button.Position.Y, w=button.size.X, h=button.size.Y};
            bool containsSlugcat = spawnData[currentSpawnIndex].slugcats?.Contains((string)button.Data!) ?? false;
            if ((!spawnData[currentSpawnIndex].exclusive && containsSlugcat) || (spawnData[currentSpawnIndex].exclusive && !containsSlugcat)) {
                SDL.SDL_SetRenderDrawColor(renderer, 0, 255, 0, alpha);
                SDL.SDL_RenderFillRectF(renderer, ref rect);
            }
            else if (spawnData[currentSpawnIndex].slugcats != null) {
                SDL.SDL_SetRenderDrawColor(renderer, 255, 0, 0, alpha);
                SDL.SDL_RenderFillRectF(renderer, ref rect);
            }
            button.Render(window, renderer);
        }
        Utils.WriteText(renderer, window, "Is this a Lineage?", Utils.currentFont, Position.X+10, Position.Y+heightForBelowSlugcatButtons, 16);
        lineageToggle.Render(window, renderer);
        if (!spawnData[currentSpawnIndex].isALineage) {
            Utils.WriteText(renderer, window, "Creature Type:", Utils.currentFont, Position.X+10, Position.Y+heightForBelowSlugcatButtons+35, 16);
            singleCreatureSelect.Render(window, renderer);
            
            Utils.WriteText(renderer, window, "Amount:", Utils.currentFont, Position.X+10, Position.Y+heightForBelowSlugcatButtons+60, 16);
            singleCreatureCount.Render(window, renderer);

            Utils.WriteText(renderer, window, "Tags:", Utils.currentFont, Position.X+10, Position.Y+heightForBelowSlugcatButtons+85, 16);
            singleCreatureTags.Render(window, renderer);
        }
        else {
            for (int i = 0; i < lineageCreatureSelects.Count; i++) {
                lineageCreatureSelects[i].Render(window, renderer);
                lineageCreatureProgressions[i].Render(window, renderer);
                lineageTags[i].Render(window, renderer);
            }
            addLineageSpawn.Render(window, renderer);
            deleteLineageSpawn.Render(window, renderer);
        }
    }
    public override void Update() {
        base.Update();
        prevButton.greyedOut = spawnData.Count <= 1;
        nextButton.greyedOut = spawnData.Count <= 1;
        prevButton.Update();
        deleteButton.Update();
        newButton.Update();
        nextButton.Update();
        exclusiveToggle.Update();
        spawnData[currentSpawnIndex].exclusive = exclusiveToggle.active;
        foreach (Button button in slugcatButtons) {
            button.Update();
        }
        lineageToggle.Update();
        spawnData[currentSpawnIndex].isALineage = lineageToggle.active;
        if (!spawnData[currentSpawnIndex].isALineage) {
            singleCreatureSelect.Update();
            singleCreatureCount.Update();
            singleCreatureTags.Update();
        }
        else {
            for (int i = 0; i < lineageCreatureSelects.Count; i++) {
                lineageCreatureSelects[i].Update();
                lineageCreatureProgressions[i].Update();
                spawnData[currentSpawnIndex].lineageSpawns[i].countOrChance = lineageCreatureProgressions[i].currentValue.ToString("F2");
                lineageTags[i].Update();
                spawnData[currentSpawnIndex].lineageSpawns[i].tags = lineageTags[i].text;
            }
            addLineageSpawn.Update();
            deleteLineageSpawn.Update();
        }
    }
    private void NewSpawn(Button _) {
        SpawnData newSpawnData = new SpawnData(null, false, spawnData[currentSpawnIndex].pipeNumber, "NONE");
        if (currentSpawnIndex == spawnData.Count-1) {
            spawnData.Add(newSpawnData);
        }
        else {
            spawnData.Insert(currentSpawnIndex+1, newSpawnData);
        }
        currentSpawnIndex++;
        ChangeCurrentSpawn(_);
        Utils.DebugLog(spawnData[currentSpawnIndex].ToString());
    }
    public void DeleteLineageSpawn(Button _) {
        spawnData[currentSpawnIndex].lineageSpawns.RemoveAt(spawnData[currentSpawnIndex].lineageSpawns.Count-1);
        lineageCreatureSelects.RemoveAt(lineageCreatureSelects.Count-1);
        lineageCreatureProgressions.RemoveAt(lineageCreatureProgressions.Count-1);
        lineageTags.RemoveAt(lineageTags.Count-1);
        addLineageSpawn._position.Y -= 65;
        deleteLineageSpawn._position.Y -= 65;
        size.Y -= 65;
        if (lineageCreatureSelects.Count == 1) {
            deleteLineageSpawn.greyedOut = true;
        }
    }
    public void AddLineageSpawn(Button _) {
        spawnData[currentSpawnIndex].lineageSpawns.Add(new SpawnData.CreatureData("NONE", "", "0"));
        Vector2 noneImageSize = Utils.GetImageSize(Utils.TexturesPath + "NONE.png");
        lineageCreatureSelects.Add(new ButtonWithImage("NONE", "NONE.png", new Vector2(noneImageSize.X*(20f/noneImageSize.Y), 20), 2.5f, this, new Vector2(60, heightForBelowSlugcatButtons+40+lineageCreatureSelects.Count*65), (int)(LineageCreatureButtonWidth-2*noneImageSize.X), 15, 15, buttonTextOffset, true, OpenCreatureSelector, extraData: lineageCreatureSelects.Count));
        lineageCreatureProgressions.Add(new ScrollButton(new Vector2(10, heightForBelowSlugcatButtons+42.5f+lineageCreatureProgressions.Count*65), new Vector2(45, 20), this, 0, 1, new Vector2(5,2.5f), 16, "F2", 0, 0.01f));
        lineageTags.Add(new TextField(new Vector2(10, heightForBelowSlugcatButtons+70+lineageTags.Count*65), new Vector2(280, 30), this, "NONE", 17, 5));
        addLineageSpawn._position.Y += 65;
        deleteLineageSpawn._position.Y += 65;
        size.Y += 65;
        deleteLineageSpawn.greyedOut = false;
    }
    public void OpenCreatureSelector(Button e) {
        CreatureSelector creatureSelector = new CreatureSelector(e._position, new Vector2(300, 650), parent!, this, (int)e.Data!);
        GetParentWindow().AddChild(creatureSelector);
    }
    public void ChangeCurrentSpawn(Button e) {
        if (e == prevButton) {
            currentSpawnIndex = currentSpawnIndex-1<0? spawnData.Count-1 : currentSpawnIndex-1;
        }
        else if (e == nextButton) {
            currentSpawnIndex = (currentSpawnIndex+1)%spawnData.Count;
        }
        GetParentWindow().updatables.RemoveAll(x => x is CreatureSelector creatureSelector && creatureSelector.denMenu == this);
        GetParentWindow().renderables.RemoveAll(x => x is CreatureSelector creatureSelector && creatureSelector.denMenu == this);
        ButtonInfoUpdate(singleCreatureSelect, spawnData[currentSpawnIndex].creatureData?.type ?? "NONE");
        lineageToggle.active = spawnData[currentSpawnIndex].isALineage;
    }
    public void DeleteSpawn(Button _) {
        GetParentWindow().parentProgram.currentWorld?.roomData.First(x => x.name == roomName).creatureSpawnData.Remove(spawnData[currentSpawnIndex]);
        spawnData.RemoveAt(currentSpawnIndex);
        if (spawnData.Count == 0) {
            Close(_);
        }
        else {
            if (currentSpawnIndex >= spawnData.Count) {
                currentSpawnIndex--;
            }
            ChangeCurrentSpawn(_);
        }
    }
    public void UpdateSelectedSlugcats(Button e) {
        if (spawnData[currentSpawnIndex].slugcats == null) {
            spawnData[currentSpawnIndex].slugcats = new();
        }
        if (!spawnData[currentSpawnIndex].slugcats!.Remove((string)e.Data!)) {
            spawnData[currentSpawnIndex].slugcats!.Add((string)e.Data!);
        }
        if (spawnData[currentSpawnIndex].slugcats!.Count == 0) {
            spawnData[currentSpawnIndex].slugcats = null;
        }
    }
    public static void ButtonInfoUpdate(ButtonWithImage button, string text) {
        button.text = text;
        button.image = text+".png";
        Vector2 imageSize = Utils.GetImageSize(Utils.TexturesPath+button.image);
        button.imageSize.X = imageSize.X*(20f/imageSize.Y);
        button.textOffset.X = buttonTextOffset.X + button.imageSize.X;
    }
    class CreatureSelector : Draggable {
        public readonly DenMenu denMenu;
        readonly int creatureToAlter;
        List<List<ButtonWithImage>> creatureButtonPages;
        ButtonWithImage nextButton, prevButton;
        int currentPage;
        public CreatureSelector(Vector2 position, Vector2 size, GenericUIElement parent, DenMenu denMenu, int creatureToAlter) : base(position, size, parent) {
            this.denMenu = denMenu;
            this.creatureToAlter = creatureToAlter;
            currentPage = 0;
            // Each page can fit 78 creatures on it.
            creatureButtonPages = Enumerable.Repeat(new List<ButtonWithImage>(), Utils.registeredCreatures.Length/78+1).ToList();
            for (int i = 0; i < Utils.registeredCreatures.Length; i++) {
                const int MaxSize = 40;
                Vector2 imageSize = Utils.GetImageSize(Utils.TexturesPath + Utils.registeredCreatures[i]+".png");
                imageSize.X = Math.Min(MaxSize, imageSize.X);
                imageSize.Y = Math.Min(MaxSize, imageSize.Y);
                
                creatureButtonPages[i/78].Add(new ButtonWithImage("", Utils.registeredCreatures[i]+".png", imageSize, (MaxSize-imageSize.X)/2, this, new Vector2(20+((i%78)%6)*45, 40+((i%78)/6)*45), MaxSize, MaxSize-(MaxSize-(int)imageSize.Y), 0, new Vector2(-imageSize.X, (MaxSize-imageSize.Y)/2), true, SetCreature, extraData: Utils.registeredCreatures[i]));
            }
            nextButton = new ButtonWithImage("", "NEXT.png", new Vector2(36, 26), 5, this, size - new Vector2(60, 28), -20, 25, 0, new Vector2(0, 0), true, NextPage){greyedOut=creatureButtonPages.Count <= 1};
            prevButton = new ButtonWithImage("", "PREV.png", new Vector2(36, 26), 4, this, new Vector2(15, size.Y-28), -20, 25, 0, new Vector2(0, 0), true, PrevPage){greyedOut=creatureButtonPages.Count <= 1};
        }
        public override void Update() {
            base.Update();
            if (!GetParentWindow().updatables.Contains(denMenu)) {
                GetParentWindow().RemoveChild(this);
                return;
            }
            SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
            if (mouseX >= Position.X && mouseX <= Position.X+size.X && mouseY >= Position.Y && mouseY <= Position.Y+size.Y) {
                foreach (var button in creatureButtonPages[currentPage]) {
                    button.Update();
                }
            }
            prevButton.Update();
            nextButton.Update();
        }
        public override void Render(nint window, nint renderer) {
            base.Render(window, renderer);
            Utils.WriteText(renderer, window, "Creature Selector", Utils.currentFont, Position.X+5, Position.Y+5, 18);
            if (creatureToAlter == -1) {
                SDL.SDL_RenderDrawLineF(renderer, Position.X, Position.Y, denMenu.singleCreatureSelect.Position.X, denMenu.singleCreatureSelect.Position.Y);
            }
            else {
                SDL.SDL_RenderDrawLineF(renderer, Position.X, Position.Y, denMenu.lineageCreatureSelects[creatureToAlter].Position.X, denMenu.lineageCreatureSelects[creatureToAlter].Position.Y);
            }
            foreach (var button in creatureButtonPages[currentPage]) {
                button.Render(window, renderer);
            }
            prevButton.Render(window, renderer);
            nextButton.Render(window, renderer);
        }
        public void SetCreature(Button e) {
            if (creatureToAlter == -1) {
                denMenu.spawnData[denMenu.currentSpawnIndex].creatureData.type = (string)e.Data!;
                ButtonInfoUpdate(denMenu.singleCreatureSelect, (string)e.Data!);
            }
            else {
                denMenu.spawnData[denMenu.currentSpawnIndex].lineageSpawns[creatureToAlter].type = (string)e.Data!;
                ButtonInfoUpdate(denMenu.lineageCreatureSelects[creatureToAlter], (string)e.Data!);
            }
        }
        public void NextPage(Button _) {
            currentPage = (currentPage+1)%creatureButtonPages.Count;
        }
        public void PrevPage(Button _) {
            currentPage = (currentPage+1)%creatureButtonPages.Count;
        }
    }
}
internal class WindowRenderCombo : GenericUIElement, IRenderable, IAmInteractable {
    internal readonly List<IRenderable> renderables;
    internal readonly List<IAmInteractable> updatables;
    internal SDL.SDL_Color backgroundColor;
    internal Program parentProgram;
    internal IntPtr window;
    internal IntPtr renderer;
    internal TextField? currentlyEditedTextField;
    internal bool IsFocused => ((SDL.SDL_GetWindowFlags(window) & (int)SDL.SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS) | (SDL.SDL_GetWindowFlags(window) & (int)SDL.SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS)) == ((int)SDL.SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS | (int)SDL.SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS);
    internal WindowRenderCombo(Vector2 position, Vector2 size, Program parentProgram, string title, SDL.SDL_WindowFlags windowFlags) : base(position, size, null) {
        // Create a new window given a title, size, and passes it a flag indicating it should be shown.
        window = SDL.SDL_CreateWindow(title, SDL.SDL_WINDOWPOS_UNDEFINED, SDL.SDL_WINDOWPOS_UNDEFINED, (int)size.X, (int)size.Y, windowFlags);
        if (window == IntPtr.Zero) {
            Utils.DebugLog($"There was an issue creating the window. {SDL.SDL_GetError()}");
        }

        // Creates a new SDL hardware renderer using the default graphics device with VSYNC enabled.
        renderer = SDL.SDL_CreateRenderer(window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED | SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);
        if (renderer == IntPtr.Zero) {
            Utils.DebugLog($"There was an issue creating the renderer. {SDL.SDL_GetError()}");
        }

        SDL.SDL_SetRenderDrawBlendMode(renderer, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);

        if (size == Vector2.Zero) {
            SDL.SDL_GetWindowSize(window, out int w, out int h);
            this.size = new Vector2(w, h);
        }

        this.parentProgram = parentProgram;
        this.renderables = new List<IRenderable>();
        this.updatables = new List<IAmInteractable>();
        this.backgroundColor = new SDL.SDL_Color(){r=8, g=38, b=82, a=255};
        this.currentlyEditedTextField = null;
    }
    public void AddChild(GenericUIElement child) {
        if (child is IRenderable renderable) {
            renderables.Add(renderable);
        }
        if (child is IAmInteractable interactable) {
            updatables.Add(interactable);
        }
    }
    public void RemoveChild(GenericUIElement child) {
        if (child is IRenderable renderable) {
            renderables.Remove(renderable);
        }
        if (child is IAmInteractable interactable) {
            updatables.Remove(interactable);
        }
    }
    public void Close() {
        Utils.DebugLog("Removed a window");
        parentProgram.windows.Remove(this);
        SDL.SDL_DestroyRenderer(renderer);
        SDL.SDL_DestroyWindow(window);
    }
    public void Render() {
        // Sets the color that the screen will be cleared with
        SDL.SDL_SetRenderDrawColor(renderer, backgroundColor.r, backgroundColor.g, backgroundColor.b, backgroundColor.a);

        // Clears the current render surface
        SDL.SDL_RenderClear(renderer);

        Render(window, renderer);

        // Switches out the currently presented render surface with the one we just did work on
        SDL.SDL_RenderPresent(renderer);
    }
    public virtual void Render(IntPtr window, IntPtr renderer) {
        try {
            for (int i = 0; i < renderables.Count; i++) {
                renderables[i].Render(window, renderer);
            }
        } catch (Exception err) {
            Utils.DebugLog(err);
        }
    }
    public void Signal(string text) {
    }
    public virtual void Update() {
        if (parentProgram.clicked || parentProgram.rightClicked) {
            currentlyEditedTextField = null;
        }
        for (int i = 0; i < updatables.Count; i++) {
            updatables[i].Update();
        }
    }
    internal override WindowRenderCombo GetParentWindow() {
        return this;
    }
}
class MainWindow : WindowRenderCombo {
    internal WorldRenderer worldRenderer;
    internal FocusableUIElement? currentlyFocusedObject;
    internal FocusableUIElement? elementToFocus;
    internal MainWindow(Vector2 position, Vector2 size, Program parentProgram, string title, SDL.SDL_WindowFlags windowFlags) : base(position, size, parentProgram, title, windowFlags) {
        worldRenderer = new WorldRenderer(Vector2.Zero, this.size, this, renderer);
        this.currentlyFocusedObject = null;
        this.elementToFocus = null;
    }
    public override void Render(IntPtr window, IntPtr renderer) {
        SDL.SDL_SetRenderDrawColor(renderer, 128, 128, 128, 255);
        for (int i = 25; i < size.X; i+=50) {
            SDL.SDL_RenderDrawLineF(renderer, i, 0, i, size.Y);
        }
        for (int i = 25; i < size.Y; i+=50) {
            SDL.SDL_RenderDrawLineF(renderer, 0, i, size.X, i);
        }
        worldRenderer.Render(window, renderer);
        base.Render(window, renderer);
    }
    public override void Update() {
        if (elementToFocus != null) {
            if (currentlyFocusedObject is WorldRenderer worldRenderer && elementToFocus != worldRenderer) {
                worldRenderer.dragged = false;
            }
            currentlyFocusedObject = elementToFocus;
            elementToFocus = null;
        }
        // The world renderer should be updated first so that it does not steal focus from any other objects
        worldRenderer.Update();
        base.Update();
        if (parentProgram.currentWorld != null) {
            SDL.SDL_SetWindowTitle(window, "World Customizer (" + parentProgram.currentWorld.acronym.ToUpper() + ")");
        }
    }
    internal new MainWindow GetParentWindow() {
        return this;
    }
}