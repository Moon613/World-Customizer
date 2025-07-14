using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using SDL2;

#nullable enable
namespace WorldCustomizer;

class FileBrowser : WindowRenderCombo {
    public static readonly Vector2 FileBrowserSize = new Vector2(640, 360);
    public string currentParentDir;
    List<FileBrowserCheckButton>? fileButtons;
    readonly ButtonWithImage quitButton;
    readonly ButtonWithImage selectButton;
    public FileBrowser(Vector2 position, Vector2 size, Program parentProgram, string title, SDL.SDL_WindowFlags windowFlags, string currentParentDir) : base(position, size, parentProgram, title, windowFlags) {
        this.currentParentDir = currentParentDir;
        fileButtons = null;
        backgroundColor = new SDL.SDL_Color(){r=200, g=200, b=200, a=255};
        quitButton = new ButtonWithImage("Quit", "quit.png", new Vector2(20, 20), 5, this, new Vector2(35, 300), 20, 15, 16, new Vector2(10, 5), true, Quit);
        selectButton = new ButtonWithImage("Select", "quit.png", new Vector2(20, 20), 5, this, new Vector2(35, 260), 20, 15, 16, new Vector2(10, 5), true, Select);
    }
    public void Select(Button _) {

    }
    public void Quit(Button _) {
        Close();
    }
    public override void Render(IntPtr window, IntPtr renderer) {
        var leftSideOpts = new SDL.SDL_Rect(){x=0, y=0, w=150, h=360};
        SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
        SDL.SDL_RenderDrawRect(renderer, ref leftSideOpts);
        quitButton.Render(window, renderer);
        selectButton.Render(window, renderer);
        
        if (fileButtons != null) {
            foreach (var button in fileButtons) {
                button.Render(window, renderer);
            }
        }
    }
    public override void Update() {
        if (fileButtons == null) {
            fileButtons = new();
            foreach (var dir in Directory.GetDirectories(Directory.GetCurrentDirectory())) {
                fileButtons.Add(new FileBrowserCheckButton(new Vector2(170, 20), new Vector2(50, 50), "folder.png", Path.GetFileNameWithoutExtension(dir)));
            }
        }
        quitButton.Update();
        selectButton.Update();
    }
}
class FileBrowserCheckButton : GenericUIElement, IRenderable, IAmInteractable {
    readonly string image;
    readonly string text;
    public FileBrowserCheckButton(Vector2 position, Vector2 size, string image, string text) : base(position, size, null) {
        this.image = image;
        this.text = text;
    }
    public void Render(IntPtr window, IntPtr renderer) {
        IntPtr texture = SDL_image.IMG_LoadTexture(renderer, "E:/World-Customizer/Build/textures" + Path.DirectorySeparatorChar + image);
        if (texture == IntPtr.Zero) {
            Console.WriteLine("Could not load image");
            Console.WriteLine(SDL.SDL_GetError());
        }

        var rect = new SDL.SDL_FRect(){x=Position.X, y=Position.Y, w=25, h=25};
        if (SDL.SDL_RenderCopyF(renderer, texture, (IntPtr)null, ref rect) < 0) {
            Console.WriteLine("Error Rendering Texture");
            Console.WriteLine(SDL.SDL_GetError());
        }

        Utils.WriteText(renderer, window, text, Utils.currentFont, Position.X+35, Position.Y+4, 18);
        
        SDL.SDL_DestroyTexture(texture);
    }
    public void Signal(string text) {
        throw new NotImplementedException();
    }
    public void Update() {
        throw new NotImplementedException();
    }
    internal override WindowRenderCombo GetParentWindow() {
        throw new NotImplementedException();
    }
}