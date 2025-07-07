using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using SDL2;

namespace WorldCustomizer;
#nullable enable

/// <summary>
/// The top menu bar, always present and provides a way to save, load, set preferences, ect.
/// </summary>
internal class OptionBar : IRenderable, IAmInteractable {
    List<Button>? options;
    ContextMenu? contextMenu;
    public IAmInteractable Parent { get; set; }
    internal OptionBar(IAmInteractable parent) {
        this.Parent = parent;
        contextMenu = null;
    }
    internal void AssignButtons(List<Button> buttons) {
        options = buttons;
    }
    public void Render(IntPtr window, IntPtr renderer) {
        SDL.SDL_GetWindowSize(window, out int width, out _);
        
        // Draw a background light grey rectangle
        SDL.SDL_SetRenderDrawColor(renderer, 122, 122, 122, 255);
        var rect = new SDL.SDL_Rect() {x = 0, y = 0, w = width, h = 32};
        SDL.SDL_RenderFillRect(renderer, ref rect);

        foreach (Button opt in options) {
            opt.Render(window, renderer);
        }

        // This draws the white border
        SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
        SDL.SDL_RenderDrawRect(renderer, ref rect);

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
        Console.WriteLine("Opened the File Context Menu");
        #pragma warning disable CS8604, IDE0090, IDE0028
        contextMenu = new ContextMenu(new Vector2(0, 32), this);
        List<Button> buttons = Button.CreateButtonsVertical(new List<Tuple<string, Action<Button>>>{
            new Tuple<string, Action<Button>>("File", new Action<Button>(_ => {Console.WriteLine("Clicked the File button");})),
            new Tuple<string, Action<Button>>("Save", new Action<Button>(_ => {Console.WriteLine("Clicked on the Save button");})),
            new Tuple<string, Action<Button>>("Save As", new Action<Button>(_ => {Console.WriteLine("Clicked on the Save As button");})),
            new Tuple<string, Action<Button>>("New", new Action<Button>(_ => {Console.WriteLine("Clicked on the New button");})),
            new Tuple<string, Action<Button>>("Load", new Action<Button>(_ => {Console.WriteLine("Clicked on the Load button");}))
        }, contextMenu, new Vector2(0, 32), 14, 5, 2);
        contextMenu.AssignButtons(buttons, new Vector2(5, 2));
        #pragma warning restore CS8604, IDE0090, IDE0028
    }
    public void OpenPreferencesContextMenu(Button _) {
        Console.WriteLine("Clicked on the Preferences Tab");
        contextMenu = new ContextMenu(new Vector2(42, 32), this);

        List<Button> buttons = Button.CreateButtonsVertical(new List<Tuple<string, Action<Button>>>{
            new Tuple<string, Action<Button>>("Background Color", OpenBackgroundColorSelector)
        }, contextMenu, new Vector2(42, 32), 14, 5, 2);

        contextMenu.AssignButtons(buttons, new Vector2(5, 2));
    }
    public void OpenBackgroundColorSelector(Button _) {
        Console.WriteLine("Clicked the background color selector button");
        ColorSelector colorSelector = new ColorSelector(new Vector2(200, 200), Parent);
        ((Window)Parent).AddChild(colorSelector);
    }
}
/// <summary>
/// A small menu that pops up when clicking or right-clicking on a parent to provide options what to do.
class ContextMenu : IRenderable, IAmInteractable {
    List<Button>? options;
    readonly internal Vector2 position;
    internal int width;
    internal int height;
    internal bool focused;
    internal const string RemoveCtxMenu = "REMOVECTXMENU";
    internal const int GraceDistance = 5;
    public IAmInteractable Parent { get; set; }
    internal ContextMenu(Vector2 position, IAmInteractable parent) {
        this.position = position;
        this.Parent = parent;
        this.focused = false;
    }
    public void AssignButtons(List<Button> buttons, Vector2 textOffset) {
        options = buttons;
        int totalHeight = 0;
        foreach (Button button in options) {
            SDL_ttf.TTF_SizeText(Program.ComicMono, button.text, out _, out int h);
            totalHeight += h;
        }
        SDL_ttf.TTF_SizeText(Program.ComicMono, options.Aggregate("", (max, cur) => max.Length > cur.text.Length ? max : cur.text), out int w, out _);
        this.height = totalHeight + 2*options.Count*(int)textOffset.Y;
        this.width = w + 2*(int)textOffset.X;
    }
    public void Render(IntPtr window, IntPtr renderer) {
        SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
        SDL.SDL_SetRenderDrawColor(renderer, 170, 170, 170, 255);
        var rect = new SDL.SDL_Rect() {x = (int)position.X, y = (int)position.Y, w = width, h = height};
        SDL.SDL_RenderFillRect(renderer, ref rect);

        // Draw each of the options
        foreach (Button opt in options) {
            opt.Render(window, renderer);
        }

    }
    public void Update() {
        SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
        if (focused && (mouseX < position.X-GraceDistance || mouseX > position.X+width+GraceDistance || mouseY < position.Y-GraceDistance || mouseY > position.Y+height+GraceDistance)) {
            Signal(RemoveCtxMenu);
        }
        if (!focused && mouseX >= position.X-GraceDistance && mouseX < position.X+width+GraceDistance && mouseY >= position.Y-GraceDistance && mouseY <= position.Y+height+GraceDistance) {
            focused = true;
        }
        // This will set focused to true is the mouse is too far away, which then leads to the context menu closing because of the first conditional in this function.
        // This can't immediantly remove the menu, because if it did then it would despawn immediantly when opened due to the mouse being too far away.
        if (mouseX < position.X-30 || mouseX > position.X+width+30 || mouseY < position.Y-30 || mouseY > position.Y+height+30) {
            focused = true;
        }
        foreach(Button button in options) {
            button.Update();
        }
    }
    public void Signal(string text) {
        Parent.Signal(text);
    }
    public void Testing() {
        
    }
}
class ColorSelector : IRenderable, IAmInteractable {
    Vector2 position;
    Vector2 size;
    readonly Slider rSlider, gSlider, bSlider;
    readonly Button applyButton;
    readonly Button closeButton;
    public IAmInteractable Parent { get; set; }
    readonly List<SDL.SDL_Vertex> verticies;
    readonly ColorBox colorBox;
    const int RAD = 60;
    internal ColorSelector(Vector2 position, IAmInteractable parent) {
        this.position = position;
        this.Parent = parent;
        size = new Vector2(300, 300);
        rSlider = new Slider(position + new Vector2(20, 270), new Vector2(73, 20), this, 0, 255, true);
        gSlider = new Slider(position + new Vector2(113, 270), new Vector2(73, 20), this, 0, 255, true);
        bSlider = new Slider(position + new Vector2(206, 270), new Vector2(73, 20), this, 0, 255, true);
        this.colorBox = new ColorBox(position + new Vector2(size.X/2-60, size.Y*0.6f), new Vector2(120, 30), new SDL.SDL_Color());
        applyButton = new Button("Apply", this, position+new Vector2(107, 240), 48, 19, 16, new Vector2(18, 2), true, ApplyColorToParentWindow);
        closeButton = new Button("X", this, position+new Vector2(273, 5), 12, 20, 22, new Vector2(5, 5), true, Close);
        
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
        ((Window)Parent).backgroundColor = Color.FromArgb(255, (int)rSlider.value, (int)gSlider.value, (int)bSlider.value);
    }
    public void Close(Button _) {
        ((Window)Parent).RemoveChild(this);
    }
    public void Render(IntPtr window, IntPtr renderer) {
        SDL.SDL_SetRenderDrawColor(renderer, 122, 122, 122, 255);
        var r = new SDL.SDL_Rect(){x=(int)position.X, y=(int)position.Y, w=(int)size.X, h=(int)size.Y};
        SDL.SDL_RenderFillRect(renderer, ref r);

        colorBox.color.r = (byte)rSlider.value;
        colorBox.color.g = (byte)gSlider.value;
        colorBox.color.b = (byte)bSlider.value;
        colorBox.Render(window, renderer);

        SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
        SDL.SDL_RenderDrawRect(renderer, ref r);
        rSlider.Render(window, renderer);
        gSlider.Render(window, renderer);
        bSlider.Render(window, renderer);
        applyButton.Render(window, renderer);
        closeButton.Render(window, renderer);
        Utils.DrawGeometryWithVertices(renderer, position + new Vector2(size.X/2, RAD+RAD/2), verticies.ToArray());
        Vector2 calculated = new Vector2(0, -RAD)*(rSlider.value/255)
            + new Vector2(RAD, 0)*(gSlider.value/255)
            + new Vector2(-RAD, 0)*(bSlider.value/255)
            + new Vector2(0, RAD)*((gSlider.value+bSlider.value)/(255*2))
            + (position + new Vector2(size.X/2, RAD+RAD/2));
        // Change the color of the center of the hexagon to show white/black
        byte blackWhiteVal = (byte)(0.2f*rSlider.value + 0.7f*gSlider.value + 0.1f*bSlider.value);
        for (int i = 2; i < verticies.Count; i += 3) {
            verticies[i] = verticies[i] with {color=new(){r=blackWhiteVal, g=blackWhiteVal, b=blackWhiteVal}};
        }

        SDL.SDL_SetRenderDrawColor(renderer, (byte)(255-blackWhiteVal), (byte)(255-blackWhiteVal), (byte)(255-blackWhiteVal), 255);
        SDL.SDL_RenderDrawLineF(renderer, position.X + size.X/2, position.Y+RAD+RAD/2, calculated.X, calculated.Y);
        
        SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
        SDL.SDL_RenderDrawRect(renderer, ref r);

    }
    public void Signal(string text) {
        
    }
    public void Update() {
        rSlider.Update();
        gSlider.Update();
        bSlider.Update();
        applyButton.Update();
        closeButton.Update();
    }
}
class Slider : IRenderable, IAmInteractable {
    Vector2 position;
    Vector2 size;
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
    public IAmInteractable Parent { get; set; }
    internal Slider(Vector2 position, Vector2 size, IAmInteractable parent, float min, float max, bool horizontal) {
        this.position = position;
        this.size = size;
        this.Parent = parent;
        this.min = min;
        this.max = max;
        value = min;
        grabbed = false;
        this.horizontal = horizontal;
        if (horizontal) {
            this.sliderPosition = new Vector2(position.X, position.Y+5);
            this.sliderSize = new Vector2(10, size.Y-10);
            this.minSliderPos = position.X;
            this.maxSliderPos = position.X + size.X - sliderSize.X;
        }
        else {
            this.sliderPosition = new Vector2(position.X+5, position.Y);
            this.sliderSize = new Vector2(size.X-10, 10);
            this.minSliderPos = position.Y;
            this.maxSliderPos = position.Y + size.Y - sliderSize.Y;
        }
    }
    public void Render(IntPtr window, IntPtr renderer) {
        SDL.SDL_SetRenderDrawColor(renderer, 122, 122, 122, 255);
        var r = new SDL.SDL_Rect(){x=(int)position.X, y=(int)position.Y, w=(int)size.X, h=(int)size.Y};
        SDL.SDL_RenderFillRect(renderer, ref r);
        var sliderRect = new SDL.SDL_Rect(){x=(int)sliderPosition.X, y=(int)sliderPosition.Y, w=(int)sliderSize.X, h=(int)sliderSize.Y};

        if (horizontal) {
            SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
            SDL.SDL_RenderDrawLineF(renderer, position.X+5, position.Y+size.Y/2, position.X+size.X-5, position.Y+size.Y/2);
            if (grabbed) {
                SDL.SDL_SetRenderDrawColor(renderer, 82, 82, 82, 255);
            }
            SDL.SDL_RenderFillRect(renderer, ref sliderRect);
        }

        SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
        SDL.SDL_RenderDrawRect(renderer, ref r);
    }
    public void Signal(string text) {
        
    }
    public void Update() {
        SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
        if (!grabbed && Program.clicked && mouseX > sliderPosition.X && mouseX < sliderPosition.X+sliderSize.X && mouseY > sliderPosition.Y && mouseY < sliderPosition.Y+sliderSize.Y) {
            grabbed = true;
            mouseRelativeGrabPos = new Vector2(mouseX, mouseY) - sliderPosition;
        }
        if (grabbed && !Program.mouseDown) {
            grabbed = false;
        }
        if (grabbed) {
            if (horizontal) {
                sliderPosition.X = Math.Max(minSliderPos, Math.Min(maxSliderPos, mouseX - mouseRelativeGrabPos.X));
                value = Utils.LerpMap(min, max, minSliderPos, maxSliderPos, sliderPosition.X);
            }
        }
    }
}
class ColorBox : IRenderable {
    Vector2 position;
    Vector2 size;
    internal SDL.SDL_Color color;
    internal ColorBox(Vector2 position, Vector2 size, SDL.SDL_Color color) {
        this.position = position;
        this.size = size;
        this.color = color;
    }
    public void Render(IntPtr window, IntPtr renderer) {
        var rect = new SDL.SDL_FRect(){x=position.X, y=position.Y, w=size.X, h=size.Y};
        SDL.SDL_SetRenderDrawColor(renderer, color.r, color.g, color.b, color.a);
        SDL.SDL_RenderFillRectF(renderer, ref rect);

        SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
        SDL.SDL_RenderDrawRectF(renderer, ref rect);
    }
}
class Button : IRenderable, IAmInteractable {
    internal event Action<Button> Clicked;
    internal string text;
    Vector2 position;
    Vector2 textOffset;
    readonly int width;
    readonly int height;
    readonly int ptsize;
    readonly bool hasBorder;
    public IAmInteractable Parent { get; set; }
    internal Button(string text, IAmInteractable parent, Vector2 position, int width, int height, int ptsize, Vector2 textOffset, bool hasBorder, Action<Button> action) {
        this.text = text;
        this.Parent = parent;
        this.position = position;
        this.ptsize = ptsize;
        this.width = width + 2*(int)textOffset.X;
        this.height = height + 2*(int)textOffset.Y;
        this.textOffset = textOffset;
        this.hasBorder = hasBorder;
        this.Clicked += action;
    }
    public void Render(IntPtr window, IntPtr renderer) {
        SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
        var r = new SDL.SDL_Rect() {x = (int)position.X, y = (int)position.Y, w = width, h = height};
        
        if (mouseX >= position.X && mouseX < position.X+width && mouseY >= position.Y && mouseY < position.Y+height) {
            SDL.SDL_SetRenderDrawColor(renderer, 82, 82, 82, 255);
            SDL.SDL_RenderFillRect(renderer, ref r);
        }
        if (hasBorder) {
            SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
            SDL.SDL_RenderDrawRect(renderer, ref r);
        }
        Utils.WriteText(renderer, window, text, Program.ComicMono, (int)position.X+(int)textOffset.X, (int)position.Y+(int)textOffset.Y, ptsize);
    }
    public void Update() {
        SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
        if (Program.clicked && mouseX >= position.X && mouseX < position.X+width && mouseY >= position.Y && mouseY < position.Y+height) {
            Clicked.Invoke(this);
        }
    }
    public void Signal(string text) {
        Parent.Signal(text);
    }
    public static List<Button> CreateButtonsVertical(List<Tuple<string, Action<Button>>> list, IAmInteractable parent, Vector2 position, int ptsize, int xOffset, int yOffset) {
        List<Button> buttonList = new();
        Vector2 drawPosition = position;
        SDL_ttf.TTF_SetFontSize(Program.ComicMono, ptsize);
        SDL_ttf.TTF_SizeText(Program.ComicMono, list.Aggregate("", (max, cur) => max.Length > cur.Item1.Length ? max : cur.Item1), out int width, out _);

        foreach (Tuple<string, Action<Button>> pair in list) {
            SDL_ttf.TTF_SizeText(Program.ComicMono, pair.Item1, out _, out int height);
            buttonList.Add(new Button(pair.Item1, parent, drawPosition, width, height, ptsize, new Vector2(xOffset, yOffset), false, pair.Item2));
            drawPosition.Y += height+2*yOffset;
        }

        return buttonList;
    }
    public static List<Button> CreateButtonsHorizontal(List<Tuple<string, Action<Button>>> list, IAmInteractable parent, Vector2 postition, int ptsize, int xOffset, int yOffset) {
        List<Button> buttonList = new();
        Vector2 drawPosition = postition;
        SDL_ttf.TTF_SetFontSize(Program.ComicMono, ptsize);
        int height = list.Aggregate(0, (max, cur) => {
                SDL_ttf.TTF_SizeText(Program.ComicMono, cur.Item1, out _, out int h);
                return max > h ? max : h;            
            });

        foreach (Tuple<string, Action<Button>> pair in list) {
            SDL_ttf.TTF_SizeText(Program.ComicMono, pair.Item1, out int width, out _);
            buttonList.Add(new Button(pair.Item1, parent, drawPosition, width, height, ptsize, new Vector2(xOffset, yOffset), false, pair.Item2));
            drawPosition.X += width+2*xOffset;
        }

        return buttonList;
    }
}

class Window : IRenderable, IAmInteractable {
    internal readonly List<IRenderable> renderables;
    internal readonly List<IAmInteractable> updatables;
    internal Vector2 position;
    internal Vector2 size;
    internal Color backgroundColor;
    public IAmInteractable Parent { get; set; }
    internal Window(Vector2 position, Vector2 size) {
        this.position = position;
        this.size = size;
        this.Parent = null;
        this.renderables = new List<IRenderable>();
        this.updatables = new List<IAmInteractable>();
        this.backgroundColor = Color.FromArgb(255, 8, 38, 82);
    }
    public void AddChild(object child) {
        if (child is IRenderable renderable) {
            renderables.Add(renderable);
        }
        if (child is IAmInteractable interactable) {
            updatables.Add(interactable);
        }
    }
    public void RemoveChild(object child) {
        if (child is IRenderable renderable) {
            renderables.Remove(renderable);
        }
        if (child is IAmInteractable interactable) {
            updatables.Remove(interactable);
        }
    }
    public void Render(IntPtr window, IntPtr renderer) {
        SDL.SDL_SetRenderDrawColor(renderer, backgroundColor.R, backgroundColor.G, backgroundColor.B, backgroundColor.A);
        var r = new SDL.SDL_Rect(){x=0, y=0, w=(int)size.X, h=(int)size.Y};
        SDL.SDL_RenderFillRect(renderer, ref r);

        SDL.SDL_SetRenderDrawColor(renderer, 128, 128, 128, 1);
        for (int i = 25; i < size.X; i+=50) {
            SDL.SDL_RenderDrawLine(renderer, i, 0, i, (int)size.Y);
        }
        for (int i = 25; i < size.Y; i+=50) {
            SDL.SDL_RenderDrawLine(renderer, 0, i, (int)size.X, i);
        }
        try {
            for (int i = 0; i < renderables.Count; i++) {
                renderables[i].Render(window, renderer);
            }
        } catch (Exception err) {
            Console.WriteLine(err);
        }
    }
    public void Signal(string text) {
    }
    public void Update() {
        try {
            for (int i = 0; i < updatables.Count; i++) {
                updatables[i].Update();
            }
        } catch (Exception err) {
            Console.WriteLine(err);
        }
    }
}