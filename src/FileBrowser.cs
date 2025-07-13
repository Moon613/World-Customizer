using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using SDL2;

#nullable enable
namespace WorldCustomizer;

class FileBrowser : IRenderable, IAmInteractable {
    public static readonly Vector2 FileBrowserSize = new Vector2(640, 360);
    public string currentParentDir;
    List<FileBrowserCheckButton>? buttons;
    public FileBrowser(string currentParentDir) {
        this.currentParentDir = currentParentDir;
        buttons = null;
    }
    public void Render(IntPtr window, IntPtr renderer) {
        var leftSideOpts = new SDL.SDL_Rect(){x=0, y=0, w=150, h=360};
        SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
        SDL.SDL_RenderDrawRect(renderer, ref leftSideOpts);
        if (buttons != null) {
            foreach (var button in buttons) {
                button.Render(renderer, window);
            }
        }
    }
    public void Signal(string text) {
        throw new NotImplementedException();
    }
    public void Update() {
        if (buttons == null) {
            buttons = new();
            foreach (var dir in Directory.GetDirectories(Directory.GetCurrentDirectory())) {
                buttons.Add(new FileBrowserCheckButton(new Vector2(170, 30), new Vector2(50, 50), "folder.png", dir));
            }
        }
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
        IntPtr texture = SDL_image.IMG_LoadTexture(renderer, "textures" + Path.DirectorySeparatorChar + image);
        Console.WriteLine("textures" + Path.DirectorySeparatorChar + image);
        if (texture == IntPtr.Zero) {
            Console.WriteLine("Could not load image");
        }

        var rect = new SDL.SDL_FRect(){x=Position.X, y=Position.Y, w=50, h=50};
        if (SDL.SDL_RenderCopyF(renderer, texture, IntPtr.Zero, ref rect) < 0) {
            Console.WriteLine("Error Rendering Texture");
        }
        
        SDL.SDL_DestroyTexture(texture);
    }
    public void Signal(string text) {
        throw new NotImplementedException();
    }
    public void Update() {
        throw new NotImplementedException();
    }
    internal override Window GetParentWindow() {
        throw new NotImplementedException();
    }
}