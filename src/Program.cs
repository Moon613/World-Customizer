using System;
using SDL2;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Numerics;

namespace WorldCustomizer;
#pragma warning disable CA1806
#nullable enable

internal class Program {
    private static IntPtr window;
    private static IntPtr renderer;
    private static bool running = true;
    internal static IntPtr ComicMono;
    internal static bool mouseDown = false;
    internal static bool clicked = false;

    #pragma warning disable CS8618
    private static Window mainWindow;
    #pragma warning restore CS8618

    /// <summary>
    /// Program entry-point
    /// </summary>
    private static void Main(string[] args) {
        Setup();

        while (running) {
            PollEvents();
            Render();
            mainWindow.Update();
            clicked = false;
        }

        CleanUp();
    }

    /// <summary>
    /// Setup all of the SDL resources needed to display a window and draw text.
    /// </summary>
    private static void Setup() {
        // Initilizes SDL
        if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) < 0 || SDL_ttf.TTF_Init() < 0) {
            Console.WriteLine($"There was an issue initializing SDL. {SDL.SDL_GetError()}");
        }

        // Create a new window given a title, size, and passes it a flag indicating it should be shown.
        window = SDL.SDL_CreateWindow("World Customizer", SDL.SDL_WINDOWPOS_UNDEFINED, SDL.SDL_WINDOWPOS_UNDEFINED, 640, 480, SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE | SDL.SDL_WindowFlags.SDL_WINDOW_MAXIMIZED);

        if (window == IntPtr.Zero) {
            Console.WriteLine($"There was an issue creating the window. {SDL.SDL_GetError()}");
        }

        // Creates a new SDL hardware renderer using the default graphics device with VSYNC enabled.
        renderer = SDL.SDL_CreateRenderer(window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED | SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);

        if (renderer == IntPtr.Zero) {
            Console.WriteLine($"There was an issue creating the renderer. {SDL.SDL_GetError()}");
        }

        // Load fonts
        ComicMono = SDL_ttf.TTF_OpenFont("ComicMono.ttf", 24);
        if (ComicMono == IntPtr.Zero) {
            Console.WriteLine("There was an error reading ComicMono");
        }

        SDL.SDL_GetWindowSize(window, out int w, out int h);
        mainWindow = new Window(new Vector2(0, 0), new Vector2(w, h));
        OptionBar optionBar = new OptionBar(mainWindow);
        mainWindow.AddChild(optionBar);
        optionBar.AssignButtons(Button.CreateButtonsHorizontal(new List<Tuple<string, Action<Button>>>{
            new Tuple<string, Action<Button>>("File", optionBar.OpenFileContextMenu),
            new Tuple<string, Action<Button>>("Preferences", new Action<Button>(optionBar.OpenPreferencesContextMenu))
            }, optionBar, new Vector2(0, 0), 14, 5, 8));
    }

    /// <summary>
    /// Checks to see if there are any events to be processed.
    /// </summary>
    private static void PollEvents() {
        // Check to see if there are any events and continue to do son until the queue is empty.
        while (SDL.SDL_PollEvent(out SDL.SDL_Event e) == 1) {
            switch (e.type) {
                case SDL.SDL_EventType.SDL_QUIT:
                    running = false;
                    break;
                case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                    clicked = true;
                    mouseDown = true;
                    break;
                case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                    mouseDown = false;
                    break;
            }
        }
    }

    /// <summary>
    /// Renders to the window.
    /// </summary>
    private static void Render() {
        // Sets the color that the screen will be cleared with
        SDL.SDL_SetRenderDrawColor(renderer, 8, 38, 82, 255);

        // Clears the current render surface
        SDL.SDL_RenderClear(renderer);

        // Draw stuff here
        SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
        mainWindow.Render(window, renderer);

        // Switches out the currently presented render surface with the one we just did work on
        SDL.SDL_RenderPresent(renderer);
    }

    /// <summary>
    /// Clean up the resources that were created
    /// </summary>
    private static void CleanUp() {
        SDL_ttf.TTF_CloseFont(ComicMono);
        SDL.SDL_DestroyRenderer(renderer);
        SDL.SDL_DestroyWindow(window);
        SDL.SDL_Quit();
    }

    /// <summary>
    /// Draws text to the screen, given a string and font, and has optional parameters for position, size, and text foreground color.<br />
    /// If Color is left null, the text will be white.
    /// </summary>
    internal static void WriteText(IntPtr renderer, IntPtr window, string text, IntPtr font, int x = 0, int y = 0, int ptsize = 24, Color? color = null, int w = 0, int h = 0) {
        if (font == IntPtr.Zero) {
            return;
        }

        SDL_ttf.TTF_SetFontSize(font, ptsize);

        SDL_ttf.TTF_SizeText(font, text, out int autoWidth, out int autoHeight);

        if (w == 0) {
            w = autoWidth / (1 + text.Count(x => x == '\n'));
        }
        if (h == 0) {
            h = autoHeight * (1 + text.Count(x => x == '\n'));
        }

        // this is the color in rgb format,
        // maxing out all would give you the color white,
        // and it will be your text's color
        SDL.SDL_Color convertedColor = new SDL.SDL_Color();
        if (color == null) {
            convertedColor.r = 255;
            convertedColor.g = 255;
            convertedColor.b = 255;
            convertedColor.a = 255;
        }
        else {
            convertedColor.r = color.Value.R;
            convertedColor.g = color.Value.G;
            convertedColor.b = color.Value.B;
            convertedColor.a = color.Value.A;
        }

        // as TTF_RenderText_Solid could only be used on
        // SDL_Surface then you have to create the surface first
        IntPtr surfaceMessage = SDL_ttf.TTF_RenderUTF8_Solid_Wrapped(ComicMono, text, convertedColor, 0); 

        // now you can convert it into a texture
        IntPtr Message = SDL.SDL_CreateTextureFromSurface(renderer, surfaceMessage);

        SDL.SDL_Rect Message_rect = new SDL.SDL_Rect() {x = x, y = y, w = w, h = h}; //create a rect

        // (0,0) is on the top left of the window/screen,
        // think a rect as the text's box,
        // that way it would be very simple to understand

        // Now since it's a texture, you have to put RenderCopy
        // in your game loop area, the area where the whole code executes

        // you put the renderer's name first, the Message,
        // the crop size (you can ignore this if you don't want
        // to dabble with cropping), and the rect which is the size
        // and coordinate of your texture
        SDL.SDL_RenderCopy(renderer, Message, (IntPtr)null, ref Message_rect);

        // Don't forget to free your surface and texture
        SDL.SDL_FreeSurface(surfaceMessage);
        SDL.SDL_DestroyTexture(Message);
    }
}

/// <summary>
/// This interface signifies that a class has components to be rendered to the screen
/// </summary>
interface IRenderable {
    void Render(IntPtr window, IntPtr renderer);
}
/// <summary>
/// This interface signifies that a class is dynamic and requires an Update function, as well as a way to talk to it's parent
/// </summary>
interface IAmInteractable {
    IAmInteractable Parent {get; set;}
    void Signal(string text);
    void Update();
}
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
            new Tuple<string, Action<Button>>("Background Color", new Action<Button>(_ => {Console.WriteLine("Clicked the bkg clr button");}))
        }, contextMenu, new Vector2(42, 32), 14, 5, 2);
        contextMenu.AssignButtons(buttons, new Vector2(5, 2));
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

class Button : IRenderable, IAmInteractable {
    internal event Action<Button> Clicked;
    internal string text;
    Vector2 position;
    Vector2 textOffset;
    readonly int width;
    readonly int height;
    readonly int ptsize;
    public IAmInteractable Parent { get; set; }
    internal Button(string text, IAmInteractable parent, Vector2 position, int width, int height, int ptsize, Vector2 textOffset, Action<Button> action) {
        this.text = text;
        this.Parent = parent;
        this.position = position;
        this.ptsize = ptsize;
        this.width = width + 2*(int)textOffset.X;
        this.height = height + 2*(int)textOffset.Y;
        this.textOffset = textOffset;
        this.Clicked += action;
    }
    public void Render(IntPtr window, IntPtr renderer) {
        SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
        if (mouseX >= position.X && mouseX < position.X+width && mouseY >= position.Y && mouseY < position.Y+height) {
            var r = new SDL.SDL_Rect() {x = (int)position.X, y = (int)position.Y, w = width, h = height};
            SDL.SDL_SetRenderDrawColor(renderer, 82, 82, 82, 255);
            SDL.SDL_RenderFillRect(renderer, ref r);
        }
        Program.WriteText(renderer, window, text, Program.ComicMono, (int)position.X+(int)textOffset.X, (int)position.Y+(int)textOffset.Y, ptsize);
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
            buttonList.Add(new Button(pair.Item1, parent, drawPosition, width, height, ptsize, new Vector2(xOffset, yOffset), pair.Item2));
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
            buttonList.Add(new Button(pair.Item1, parent, drawPosition, width, height, ptsize, new Vector2(xOffset, yOffset), pair.Item2));
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
    public IAmInteractable Parent { get; set; }
    internal Window(Vector2 position, Vector2 size) {
        this.position = position;
        this.size = size;
        this.Parent = null;
        this.renderables = new List<IRenderable>();
        this.updatables = new List<IAmInteractable>();
    }
    public void AddChild(object child) {
        if (child is IRenderable renderable) {
            renderables.Add(renderable);
        }
        if (child is IAmInteractable interactable) {
            updatables.Add(interactable);
        }
    }
    public void Render(IntPtr window, IntPtr renderer) {
        
        foreach (IRenderable child in renderables) {
            child.Render(window, renderer);
        }
    }
    public void Signal(string text) {
    }
    public void Update() {
        foreach (IAmInteractable child in updatables) {
            child.Update();
        }
    }
}