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
    private static readonly OptionBar optionBar = new OptionBar(new List<string>{"File", "Preferences"});
    internal static IntPtr ComicMono;
    internal static bool mouseDown;

    /// <summary>
    /// Program entry-point
    /// </summary>
    private static void Main(string[] args) {
        Setup();

        while (running) {
            PollEvents();
            Render();
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
        optionBar.Render(window, renderer);

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

interface IRenderable {
    void Render(IntPtr window, IntPtr renderer);
}
interface IAmInteractable {
    void Signal(string text);
}

internal class OptionBar : IRenderable, IAmInteractable {
    readonly List<string> options;
    ContextMenu? contextMenu;
    internal OptionBar(List<string> options) {
        this.options = options;
        contextMenu = null;
    }
    public void Render(IntPtr window, IntPtr renderer) {
        SDL.SDL_GetWindowSize(window, out int width, out _);
        
        // Draw a background light grey rectangle
        SDL.SDL_SetRenderDrawColor(renderer, 122, 122, 122, 255);
        var rect = new SDL.SDL_Rect() {x = 0, y = 0, w = width, h = 30};
        SDL.SDL_RenderFillRect(renderer, ref rect);

        int x = 5;
        foreach (string opt in options) {
            // Get some data stuff needed to draw extra rectangles
            SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
            SDL_ttf.TTF_SizeText(Program.ComicMono, opt, out int autoWidth, out _);
            int textWidth = autoWidth / (1 + opt.Count(x => x == '\n'));

            // If the mouse if hovering over the current option, give it a different background
            if (mouseX >= x-5 && mouseX < x + textWidth + 5 && mouseY <= 30 && contextMenu == null) {
                var r = new SDL.SDL_Rect() {x = x-5, y = 0, w = textWidth+10, h = 30};
                SDL.SDL_SetRenderDrawColor(renderer, 82, 82, 82, 255);
                SDL.SDL_RenderFillRect(renderer, ref r);
                if (Program.mouseDown) {
                    contextMenu = new ContextMenu(new List<string>{"New", "Save", "Save As", "Open"}, new Vector2(mouseX, mouseY), this);
                }
            }

            // Draw the text
            Program.WriteText(renderer, window, opt, Program.ComicMono, x, 8, 14);
            
            // Set the x position of the next option 10 pixels after the current option
            x += textWidth + 10;
        }

        // This draws the white border
        SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
        SDL.SDL_RenderDrawRect(renderer, ref rect);

        // If the context menu is being displayed, then render it.
        contextMenu?.Render(window, renderer);
    }
    public void Signal(string text) {
        if (text == ContextMenu.RemoveCtxMenu) {
            contextMenu = null;
        }
    }
}

class ContextMenu : IRenderable, IAmInteractable {
    readonly List<string> options;
    readonly internal Vector2 position;
    readonly internal int width;
    readonly internal int height;
    readonly IAmInteractable parent;
    internal const string RemoveCtxMenu = "REMOVECTXMENU";
    internal const int GraceDistance = 5;
    readonly static Vector2 TextOffset = new Vector2(5, 2);
    internal ContextMenu(List<string> options, Vector2 position, IAmInteractable parent) {
        this.options = options;
        this.position = position;
        this.parent = parent;
        SDL_ttf.TTF_SizeText(Program.ComicMono, options.Aggregate("", (max, cur) => max.Length > cur.Length ? max : cur), out int w, out int h);
        this.height = (h + (int)TextOffset.Y) * options.Count;
        this.width = w + 2*(int)TextOffset.X;
    }
    public void Render(IntPtr window, IntPtr renderer) {
        SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
        SDL.SDL_SetRenderDrawColor(renderer, 170, 170, 170, 255);
        var rect = new SDL.SDL_Rect() {x = (int)position.X, y = (int)position.Y, w = width, h = height};
        SDL.SDL_RenderFillRect(renderer, ref rect);

        // Draw each of the options
        Vector2 drawOptionPosition = position;
        drawOptionPosition += TextOffset;
        foreach (string opt in options) {
            Program.WriteText(renderer, window, opt, Program.ComicMono, (int)drawOptionPosition.X, (int)drawOptionPosition.Y, 14);
            SDL_ttf.TTF_SizeText(Program.ComicMono, opt, out _, out int autoHeight);
            drawOptionPosition.Y += autoHeight * (1 + opt.Count(x => x == '\n')) + 2;
        }

        if (mouseX < position.X-GraceDistance || mouseX > position.X+width+GraceDistance || mouseY < position.Y-GraceDistance || mouseY > position.Y+height+GraceDistance) {
            Signal(RemoveCtxMenu);
        }
    }
    public void Signal(string text) {
        parent.Signal(text);
    }
}

class Window : IRenderable {
    List<IRenderable> children;
    Vector2 position;
    Vector2 size;
    Window(Vector2 position, Vector2 size) {
        this.position = position;
        this.size = size;
        this.children = new List<IRenderable>();
    }

    public void Render(IntPtr window, IntPtr renderer) {
        foreach (IRenderable child in children) {
            child.Render(window, renderer);
        }
    }

    void Update() {
    }
}