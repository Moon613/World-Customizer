using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SDL2;
using System.Threading;
using System.IO;

namespace WorldCustomizer;
#nullable enable

/// <summary>
/// The top menu bar, always present and provides a way to save, load, set preferences, ect.
/// </summary>
internal class OptionBar : GenericUIElement, IRenderable, IAmInteractable {
    List<Button>? options;
    ContextMenu? contextMenu;
    new WindowRenderCombo parent;
    internal OptionBar(Vector2 size, WindowRenderCombo parent) : base(Vector2.Zero, size, parent) {
        this.parent = parent;
        contextMenu = null;
    }
    internal void AssignButtons(List<Button> buttons) {
        options = buttons;
    }
    internal override WindowRenderCombo GetParentWindow() {
        return parent.GetParentWindow();
    }
    public void Render(IntPtr window, IntPtr renderer) {        
        // Draw a background light grey rectangle
        SDL.SDL_SetRenderDrawColor(renderer, 122, 122, 122, 255);
        var rect = new SDL.SDL_FRect() {x = 0, y = 0, w = size.X, h = size.Y};
        SDL.SDL_RenderFillRectF(renderer, ref rect);

        foreach (Button opt in options) {
            opt.Render(window, renderer);
        }

        // This draws the white border
        SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
        SDL.SDL_RenderDrawRectF(renderer, ref rect);

        // If the context menu is being displayed, then render it.
        contextMenu?.Render(window, renderer);
    }
    public void Update() {
        contextMenu?.Update();
        foreach (Button button in options) {
            button.Update();
        }
    }
    public void Signal(string text) {
        if (text == ContextMenu.RemoveCtxMenu) {
            contextMenu = null;
        }
    }
    public void OpenFileContextMenu(Button _) {
        Utils.DebugLog("Opened the File Context Menu");
        #pragma warning disable CS8604, IDE0090, IDE0028
        contextMenu = new ContextMenu(new Vector2(0, 32), this);
        List<Button> buttons = Button.CreateButtonsVertical(new List<Tuple<string, Action<Button>>>{
            new Tuple<string, Action<Button>>("File", new Action<Button>(_ => {Utils.DebugLog("Clicked the File button");})),
            new Tuple<string, Action<Button>>("Save", new Action<Button>(_ => {Utils.DebugLog("Clicked on the Save button");})),
            new Tuple<string, Action<Button>>("Save As", new Action<Button>(_ => {Utils.DebugLog("Clicked on the Save As button");})),
            new Tuple<string, Action<Button>>("New", new Action<Button>(_ => {Utils.DebugLog("Clicked on the New button");})),
            new Tuple<string, Action<Button>>("Load", LoadFile)
        }, contextMenu, new Vector2(0, 0), 14, 5, 2);
        contextMenu.AssignButtons(buttons, new Vector2(5, 2));
        #pragma warning restore CS8604, IDE0090, IDE0028
    }
    public void LoadFile(Button _) {
        GetParentWindow().parentProgram.OpenFileBrowser();
    }
    public void OpenPreferencesContextMenu(Button _) {
        Utils.DebugLog("Clicked on the Preferences Tab");
        contextMenu = new ContextMenu(new Vector2(42, 32), this);

        List<Button> buttons = Button.CreateButtonsVertical(new List<Tuple<string, Action<Button>>>{
            new Tuple<string, Action<Button>>("Background Color", OpenBackgroundColorSelector)
        }, contextMenu, new Vector2(0, 0), 14, 5, 2);

        contextMenu.AssignButtons(buttons, new Vector2(5, 2));
    }
    public void OpenBackgroundColorSelector(Button _) {
        Utils.DebugLog("Clicked the background color selector button");
        ColorSelector colorSelector = new ColorSelector(new Vector2(200, 200), new Vector2(300, 300), parent);
        parent.AddChild(colorSelector);
    }
}
/// <summary>
/// A small menu that pops up when clicking or right-clicking on a parent to provide options what to do.
/// </summary>
class ContextMenu : GenericUIElement, IRenderable, IAmInteractable {
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
        foreach (Button opt in options) {
            opt.Render(window, renderer);
        }
    }
    public void Update() {
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
        foreach(Button button in options) {
            button.Update();
        }
    }
    public void Signal(string text) {
        if (parent is IAmInteractable interactable) {
            interactable.Signal(text);
        }
    }
    internal override WindowRenderCombo GetParentWindow() {
        return parent.GetParentWindow();
    }
}
class ColorSelector : Draggable {
    readonly Slider rSlider, gSlider, bSlider;
    readonly Button applyButton;
    readonly Button closeButton;
    readonly List<SDL.SDL_Vertex> verticies;
    readonly ColorBox colorBox;
    new internal WindowRenderCombo parent;
    const int RAD = 60;
    internal ColorSelector(Vector2 position, Vector2 size, WindowRenderCombo parent) : base (position, size, parent) {
        this.parent = parent;
        rSlider = new Slider(new Vector2(20, 270), new Vector2(73, 20), this, 0, 255, true);
        gSlider = new Slider(new Vector2(113, 270), new Vector2(73, 20), this, 0, 255, true);
        bSlider = new Slider(new Vector2(206, 270), new Vector2(73, 20), this, 0, 255, true);
        this.colorBox = new ColorBox(new Vector2(size.X/2-60, size.Y*0.6f), new Vector2(120, 30), this, new SDL.SDL_Color());
        applyButton = new Button("Apply", this, new Vector2(107, 240), 48, 19, 16, new Vector2(18, 2), true, ApplyColorToParentWindow);
        closeButton = new Button("X", this, new Vector2(273, 5), 12, 20, 22, new Vector2(5, 5), true, Close);
        
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
    }
    public void Close(Button _) {
        parent.RemoveChild(this);
    }
    public override void Render(IntPtr window, IntPtr renderer) {
        SDL.SDL_SetRenderDrawColor(renderer, 122, 122, 122, 255);
        var r = new SDL.SDL_FRect(){x=Position.X, y=Position.Y, w=size.X, h=size.Y};
        SDL.SDL_RenderFillRectF(renderer, ref r);
        
        base.Render(window, renderer);

        colorBox.color.r = (byte)rSlider.value;
        colorBox.color.g = (byte)gSlider.value;
        colorBox.color.b = (byte)bSlider.value;
        colorBox.Render(window, renderer);

        SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
        SDL.SDL_RenderDrawRectF(renderer, ref r);
        rSlider.Render(window, renderer);
        gSlider.Render(window, renderer);
        bSlider.Render(window, renderer);
        applyButton.Render(window, renderer);
        closeButton.Render(window, renderer);
        Utils.DrawGeometryWithVertices(renderer, Position + new Vector2(size.X/2, RAD+RAD/2), verticies.ToArray());
        Vector2 calculated = new Vector2(0, -RAD)*(rSlider.value/255)
            + new Vector2(RAD, 0)*(gSlider.value/255)
            + new Vector2(-RAD, 0)*(bSlider.value/255)
            + new Vector2(0, RAD)*((gSlider.value+bSlider.value)/(255*2))
            + (Position + new Vector2(size.X/2, RAD+RAD/2));
        // Change the color of the center of the hexagon to show white/black
        byte blackWhiteVal = (byte)(0.2f*rSlider.value + 0.7f*gSlider.value + 0.1f*bSlider.value);
        for (int i = 2; i < verticies.Count; i += 3) {
            verticies[i] = verticies[i] with {color=new(){r=blackWhiteVal, g=blackWhiteVal, b=blackWhiteVal}};
        }

        SDL.SDL_SetRenderDrawColor(renderer, (byte)(255-blackWhiteVal), (byte)(255-blackWhiteVal), (byte)(255-blackWhiteVal), 255);
        SDL.SDL_RenderDrawLineF(renderer, Position.X + size.X/2, Position.Y+RAD+RAD/2, calculated.X, calculated.Y);
        
        SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
        SDL.SDL_RenderDrawRectF(renderer, ref r);

    }
    public override void Update() {
        base.Update();
        rSlider.Update();
        gSlider.Update();
        bSlider.Update();
        applyButton.Update();
        closeButton.Update();
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
        return parent.GetParentWindow();
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
        return parent.GetParentWindow();
    }
}
class Button : GenericUIElement, IRenderable, IAmInteractable {
    internal event Action<Button> Clicked;
    internal string text;
    internal Vector2 textOffset;
    readonly int ptsize;
    readonly bool hasBorder;
    SDL.SDL_Color color;
    internal Button(string text, GenericUIElement? parent, Vector2 position, int width, int height, int ptsize, Vector2 textOffset, bool hasBorder, Action<Button> action, SDL.SDL_Color? color = null) : base(position, Vector2.Zero, parent) {
        this.text = text;
        this.size = new Vector2(width + 2*(int)textOffset.X, height + 2*(int)textOffset.Y);
        this.ptsize = ptsize;
        this.textOffset = textOffset;
        this.hasBorder = hasBorder;
        this.Clicked += action;
        this.color = (SDL.SDL_Color)((color==null) ? new SDL.SDL_Color(){r=255, g=255, b=255, a=255} : color);
    }
    public virtual void Render(IntPtr window, IntPtr renderer) {
        SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
        var r = new SDL.SDL_FRect() {x = Position.X, y = Position.Y, w = size.X, h = size.Y};
        
        if (GetParentWindow().IsFocused && mouseX >= Position.X && mouseX < Position.X+size.X && mouseY >= Position.Y && mouseY < Position.Y+size.Y) {
            SDL.SDL_SetRenderDrawColor(renderer, 82, 82, 82, 255);
            SDL.SDL_RenderFillRectF(renderer, ref r);
        }
        if (hasBorder) {
            SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
            SDL.SDL_RenderDrawRectF(renderer, ref r);
        }
        Utils.WriteText(renderer, window, text, Utils.currentFont, (int)Position.X+(int)textOffset.X, (int)Position.Y+(int)textOffset.Y, ptsize, color);
    }
    public void Update() {
        SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
        if (GetParentWindow().IsFocused && GetParentWindow().parentProgram.clicked && mouseX >= Position.X && mouseX < Position.X+size.X && mouseY >= Position.Y && mouseY < Position.Y+size.Y) {
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
    readonly string image;
    Vector2 imageSize;
    readonly float imageXOffset;
    internal ButtonWithImage(string text, string image, Vector2 imageSize, float imageXOffset, GenericUIElement? parent, Vector2 position, int width, int height, int ptsize, Vector2 textOffset, bool hasBorder, Action<Button> action, SDL.SDL_Color? color = null) : base(text, parent, position, width, height, ptsize, textOffset + new Vector2(imageSize.X, 0), hasBorder, action, color) {
        this.image = image;
        this.imageSize = imageSize;
        this.imageXOffset = imageXOffset;
    }
    public override void Render(IntPtr window, IntPtr renderer) {
        base.Render(window, renderer);
        IntPtr texture = SDL_image.IMG_LoadTexture(renderer, "E:/World-Customizer/Build/textures" + Path.DirectorySeparatorChar + image);
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

class WindowRenderCombo : GenericUIElement, IRenderable, IAmInteractable {
    internal readonly List<IRenderable> renderables;
    internal readonly List<IAmInteractable> updatables;
    internal SDL.SDL_Color backgroundColor;
    internal Program parentProgram;
    internal IntPtr window;
    internal IntPtr renderer;
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

        if (size == Vector2.Zero) {
            SDL.SDL_GetWindowSize(window, out int w, out int h);
            this.size = new Vector2(w, h);
        }

        this.parentProgram = parentProgram;
        this.renderables = new List<IRenderable>();
        this.updatables = new List<IAmInteractable>();
        this.backgroundColor = new SDL.SDL_Color(){r=8, g=38, b=82, a=255};
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
        SDL.SDL_SetRenderDrawColor(renderer, 128, 128, 128, 255);
        for (int i = 25; i < size.X; i+=50) {
            SDL.SDL_RenderDrawLineF(renderer, i, 0, i, size.Y);
        }
        for (int i = 25; i < size.Y; i+=50) {
            SDL.SDL_RenderDrawLineF(renderer, 0, i, size.X, i);
        }
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
        try {
            for (int i = 0; i < updatables.Count; i++) {
                updatables[i].Update();
            }
        } catch (Exception err) {
            Utils.DebugLog(err);
        }
    }
    internal override WindowRenderCombo GetParentWindow() {
        return this;
    }
}