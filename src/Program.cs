using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            // Reset variable for the next loop
            main.clicked = false;
            main.scrollY = 0;
        }
        } catch (Exception err) {
            Utils.DebugLog(err);
            Utils.DebugLog(SDL.SDL_GetError());
        }

        main.CleanUp();
    }
}
internal class Program {
    public bool running = true;
    /// <summary>
    /// Is true if the mouse is being held down.
    /// </summary>
    internal bool mouseDown = false;
    /// <summary>
    /// Is true if the mouse was clicked this frame.
    /// </summary>
    internal bool clicked = false;
    internal float scrollY;
    internal string? folderToLoadFrom = null;
    internal WorldData? currentWorld;

    #pragma warning disable CS8618
    internal List<WindowRenderCombo> windows;
    #pragma warning restore CS8618

    /// <summary>
    /// Setup all of the SDL resources needed to display a window and draw text.<br/>
    /// Also creates the "main" window.
    /// </summary>
    public void Setup() {
        if (File.Exists(Utils.DebugLogPath) && File.ReadAllText(Utils.DebugLogPath) != "") {
            File.WriteAllText(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "debugLog_1.txt", File.ReadAllText(Utils.DebugLogPath));
            File.WriteAllText(Utils.DebugLogPath, "");
        }

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
        WindowRenderCombo mainWindow = new WindowRenderCombo(new Vector2(0, 0), Vector2.Zero, this, "World Customizer", SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE | SDL.SDL_WindowFlags.SDL_WINDOW_MAXIMIZED | SDL.SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS | SDL.SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS);
        windows.Add(mainWindow);
        mainWindow.worldRenderer = new WorldRenderer(Vector2.Zero, mainWindow.size, mainWindow, mainWindow.renderer);
        OptionBar optionBar = new OptionBar(new Vector2(mainWindow.size.X, 32), mainWindow);
        mainWindow.AddChild(optionBar);
        optionBar.AssignButtons(Button.CreateButtonsHorizontal(new List<Tuple<string, Action<Button>>>{
            new Tuple<string, Action<Button>>("File", optionBar.OpenFileContextMenu),
            new Tuple<string, Action<Button>>("Preferences", optionBar.OpenPreferencesContextMenu),
            new Tuple<string, Action<Button>>("Layer 1", optionBar.ToggleLayer),
            new Tuple<string, Action<Button>>("Layer 2", optionBar.ToggleLayer),
            new Tuple<string, Action<Button>>("Layer 3", optionBar.ToggleLayer)
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
                case SDL.SDL_EventType.SDL_MOUSEWHEEL:
                    scrollY = e.wheel.preciseY;
                    break;
            }
        }
    }
    /// <summary>
    /// Creates a new window for browsing files.
    /// </summary>
    public void OpenFileBrowser() {
        windows.Add(new FileBrowser(Vector2.Zero, FileBrowser.FileBrowserSize, this, "File Browser", SDL.SDL_WindowFlags.SDL_WINDOW_ALWAYS_ON_TOP | SDL.SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS | SDL.SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS | SDL.SDL_WindowFlags.SDL_WINDOW_BORDERLESS, Directory.GetCurrentDirectory()));
    }
    /// <summary>
    /// Updates all windows. If <see cref="folderToLoadFrom"/> is not null it will open that region.
    /// </summary>
    public void Update() {
        if (folderToLoadFrom != null) {
            currentWorld?.Destroy();
            currentWorld = new WorldData(folderToLoadFrom, windows[0].renderer);
            foreach (var room in currentWorld.roomData) {
                Utils.DebugLog(room.ToString());
            }
            folderToLoadFrom = null;
            windows[0].worldRenderer.dragPosition = windows[0].size*0.5f;
        }
        windows.Last().Update();
        // for (int i = 0; i < windows.Count; i++) {
        //     windows[i].Update();
        // }
    }
    /// <summary>
    /// Renders all windows.
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
            window.worldRenderer?.Destroy();
        }
        currentWorld?.Destroy();
        SDL_ttf.TTF_CloseFont(Utils.currentFont);
        SDL_image.IMG_Quit();
        SDL.SDL_Quit();
    }
}