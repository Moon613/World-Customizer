using System;
using System.Drawing;
using System.Collections.Generic;
using System.Numerics;
using SDL2;

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
    internal static Color backgroundColor;

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

}