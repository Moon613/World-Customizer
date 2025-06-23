using System;
using SDL2;
using System.Linq;
using System.Drawing;

namespace WorldCustomizer;

class Program {
    static IntPtr window;
    static IntPtr renderer;
    static bool running = true;
    static IntPtr ComicMono;

    /// <summary>
    /// Program entry-point
    /// </summary>
    static void Main(string[] args) {
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
    static void Setup() {
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
    static void PollEvents() {
        // Check to see if there are any events and continue to do son until the queue is empty.
        while (SDL.SDL_PollEvent(out SDL.SDL_Event e) == 1) {
            switch (e.type) {
                case SDL.SDL_EventType.SDL_QUIT:
                    running = false;
                    break;
            }
        }
    }

    /// <summary>
    /// Renders to the window.
    /// </summary>
    static void Render() {
        // Sets the color that the screen will be cleared with
        SDL.SDL_SetRenderDrawColor(renderer, 8, 38, 82, 255);

        // Clears the current render surface
        SDL.SDL_RenderClear(renderer);

        // Draw stuff to the screen here.
        SDL.SDL_SetRenderDrawColor(renderer, 255, 0, 0, 255);
        SDL.SDL_GetWindowSize(window, out int width, out int height);
        SDL.SDL_RenderDrawLine(renderer, 0, 0, width, height);

        SDL.SDL_RenderDrawPoint(renderer, 20, 20);
        var rect = new SDL.SDL_Rect() {x = 300, y = 100, w = 50, h = 50};
        SDL.SDL_RenderFillRect(renderer, ref rect);

        WriteText("Hello\nWorld", ComicMono);
        WriteText("Hello World", ComicMono, 200);

        // Switches out the currently presented render surface with the one we just did work on
        SDL.SDL_RenderPresent(renderer);
    }

    /// <summary>
    /// Clean up the resources that were created
    /// </summary>
    static void CleanUp() {
        SDL_ttf.TTF_CloseFont(ComicMono);
        SDL.SDL_DestroyRenderer(renderer);
        SDL.SDL_DestroyWindow(window);
        SDL.SDL_Quit();
    }

    /// <summary>
    /// Draws text to the screen, given a string and font, and has optional parameters for position, size, and text foreground color.
    /// </summary>
    static void WriteText(string text, IntPtr font, int x = 0, int y = 0, Color? color = null, int w = 0, int h = 0) {
        if (font == IntPtr.Zero) {
            return;
        }

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