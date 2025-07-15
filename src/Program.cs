using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using SDL2;

#pragma warning disable CA1806
#nullable enable

namespace WorldCustomizer;

/// <summary>
/// Program entry-point
/// </summary>
class Entry {
    static void Main(string[] args) {
        Program main = new();
        main.Setup();

        try {
        while (main.running) {
            main.PollEvents();
            main.Update();
            main.Render();
            main.clicked = false;
        }
        } catch (Exception err) {
            if (!File.Exists(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "debugLog.txt")) {
                File.Create(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "debugLog.txt");
            }
            File.WriteAllText(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "debugLog.txt", err.ToString());
        }

        main.CleanUp();
    }
}
internal class Program {
    public bool running = true;
    internal bool mouseDown = false;
    internal bool clicked = false;
    internal string? folderToLoadFrom = null;

    #pragma warning disable CS8618
    internal List<WindowRenderCombo> windows;
    #pragma warning restore CS8618

    /// <summary>
    /// Setup all of the SDL resources needed to display a window and draw text.
    /// </summary>
    public void Setup() {
        // Initilizes SDL
        if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) < 0 || SDL_ttf.TTF_Init() < 0 || SDL_image.IMG_Init(SDL_image.IMG_InitFlags.IMG_INIT_PNG) == 0) {
            Utils.DebugLog($"There was an issue initializing SDL. {SDL.SDL_GetError()}");
        }

        // Load fonts
        Utils.currentFont = SDL_ttf.TTF_OpenFont("ComicMono.ttf", 24);
        if (Utils.currentFont == IntPtr.Zero) {
            Utils.DebugLog("There was an error reading ComicMono");
        }

        windows = new List<WindowRenderCombo>();
        WindowRenderCombo mainWindow = new WindowRenderCombo(new Vector2(0, 0), Vector2.Zero, this, "World Customizer", SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE | SDL.SDL_WindowFlags.SDL_WINDOW_MAXIMIZED);
        windows.Add(mainWindow);
        OptionBar optionBar = new OptionBar(new Vector2(mainWindow.size.X, 32), mainWindow);
        mainWindow.AddChild(optionBar);
        optionBar.AssignButtons(Button.CreateButtonsHorizontal(new List<Tuple<string, Action<Button>>>{
            new Tuple<string, Action<Button>>("File", optionBar.OpenFileContextMenu),
            new Tuple<string, Action<Button>>("Preferences", new Action<Button>(optionBar.OpenPreferencesContextMenu))
            }, optionBar, new Vector2(0, 0), 14, 5, 8));
    }

    /// <summary>
    /// Checks to see if there are any events to be processed.
    /// </summary>
    public void PollEvents() {
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
                case SDL.SDL_EventType.SDL_WINDOWEVENT:
                    if (e.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE) {
                        for (int i = 0; i < windows.Count; i++) {
                            if (SDL.SDL_GetWindowID(windows[i].window) == e.window.windowID) {
                                windows[i].Close();
                            }
                        }
                    }
                    break;
            }
        }
    }
    public void OpenFileBrowser() {
        windows.Add(new FileBrowser(Vector2.Zero, FileBrowser.FileBrowserSize, this, "File Browser", SDL.SDL_WindowFlags.SDL_WINDOW_ALWAYS_ON_TOP | SDL.SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS | SDL.SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS, Directory.GetCurrentDirectory()));
    }
    public void Update() {
        for (int i = 0; i < windows.Count; i++) {
            windows[i].Update();
        }
    }
    /// <summary>
    /// Renders to the window.
    /// </summary>
    public void Render() {
        for (int i = 0; i < windows.Count; i++) {
            windows[i].Render();
        }
    }

    /// <summary>
    /// Clean up the resources that were created
    /// </summary>
    public void CleanUp() {
        foreach (WindowRenderCombo window in windows) {
            SDL.SDL_DestroyRenderer(window.renderer);
            SDL.SDL_DestroyWindow(window.window);
        }
        SDL_ttf.TTF_CloseFont(Utils.currentFont);
        SDL_image.IMG_Quit();
        SDL.SDL_Quit();
    }
}