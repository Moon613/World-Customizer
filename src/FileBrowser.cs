using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using SDL2;

#nullable enable
namespace WorldCustomizer;

class FileBrowser : WindowRenderCombo {
    public enum Type {
        File,
        Folder
    }
    const int Scroll_Maximum = 10;
    const int Button_Height = 30;
    const int Space_Between_Scrollable_Buttons = Button_Height+5;
    int Scroll_Minimum;
    float currentScroll;
    public static readonly Vector2 FileBrowserSize = new Vector2(640, 360);
    public string currentParentDir;
    List<FileBrowserCheckButton>? fileButtons;
    readonly List<Button> directoryButtons;
    readonly ButtonWithImage quitButton;
    readonly ButtonWithImage selectButton;
    readonly ButtonWithImage enterDirButton;
    readonly ButtonWithImage upDirButton;
    public FileBrowserCheckButton? selected;
    public FileBrowser(Vector2 position, Vector2 size, Program parentProgram, string title, SDL.SDL_WindowFlags windowFlags, string currentParentDir) : base(position, size, parentProgram, title, windowFlags) {
        this.currentParentDir = currentParentDir;
        fileButtons = null;
        backgroundColor = new SDL.SDL_Color(){r=115, g=115, b=115, a=255};
        currentScroll = Scroll_Maximum;
        directoryButtons = new ();
        DriveInfo.GetDrives().Where(x => x.IsReady).ToList().ForEach(info => directoryButtons.Add(new Button(info.RootDirectory.Name, this, new Vector2(55, 200 + 30*directoryButtons.Count), 20, 15, 16, new Vector2(10, 5), true, ChangeDrive)));
        quitButton = new ButtonWithImage("Quit", "quit.png", new Vector2(20, 20), 5, this, new Vector2(35, 140), 20, 15, 16, new Vector2(10, 5), true, Quit);
        selectButton = new ButtonWithImage("Select", "file.png", new Vector2(20, 20), 5, this, new Vector2(30, 100), 30, 15, 16, new Vector2(10, 5), true, Select);
        enterDirButton = new ButtonWithImage("Down", "folder.png", new Vector2(20, 20), 5, this, new Vector2(35, 60), 20, 15, 16, new Vector2(10, 5), true, EnterDirectory);
        upDirButton = new ButtonWithImage("Up", "up_dir.png", new Vector2(20, 20), 5, this, new Vector2(45, 20), -5, 15, 16, new Vector2(10, 5), true, UpDirectory);
    }
    private void ChangeParentDirectory(string newDirectory) {
        fileButtons = null;
        currentScroll = Scroll_Maximum;
        currentParentDir = newDirectory;
        selected = null;
    }
    public void ChangeDrive(Button e) {
        ChangeParentDirectory(e.text);
        Utils.DebugLog(Directory.GetDirectoryRoot(currentParentDir));
    }
    public void UpDirectory(Button _) {
        if (currentParentDir != Directory.GetDirectoryRoot(currentParentDir)) {
            ChangeParentDirectory(Directory.GetParent(currentParentDir).FullName);
        }
    }
    public void EnterDirectory(Button _) {
        if (selected?.type == Type.Folder) {
            ChangeParentDirectory(currentParentDir + Path.DirectorySeparatorChar + selected.text);
        }
    }
    public void Select(Button _) {
        if (selected != null) {
            parentProgram.folderToLoadFrom = currentParentDir + Path.DirectorySeparatorChar + selected.text;
        }
        Utils.DebugLog(parentProgram.folderToLoadFrom);
        Close();
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
        enterDirButton.Render(window, renderer);
        upDirButton.Render(window, renderer);
        foreach (Button button in directoryButtons) {
            button.Render(window, renderer);
        }
        
        if (fileButtons != null) {
            foreach (var button in fileButtons) {
                button.Render(window, renderer);
            }
        }
    }
    public override void Update() {
        if (fileButtons == null) {
            fileButtons = new();
            float fileYPos = Scroll_Maximum;
            const int Ptsize = 18;
            foreach (var dir in Directory.GetDirectories(currentParentDir)) {
                DirectoryInfo directoryInfo = new DirectoryInfo(dir);
                if ((directoryInfo.Attributes & FileAttributes.Hidden) == 0 && (directoryInfo.Attributes & FileAttributes.System) == 0) {
                    string name = Path.GetFileName(dir);
                    SDL_ttf.TTF_SetFontSize(Utils.currentFont, Ptsize);
                    SDL_ttf.TTF_SizeText(Utils.currentFont, name, out int w, out _);
                    fileButtons.Add(new FileBrowserCheckButton(new Vector2(170, fileYPos), new Vector2(w+40, Button_Height), this, Type.Folder, "folder.png", name, Ptsize));
                    fileYPos += Space_Between_Scrollable_Buttons;
                }
            }
            foreach (var file in Directory.GetFiles(currentParentDir)) {
                FileInfo fileInfo = new FileInfo(file);
                if ((fileInfo.Attributes & FileAttributes.Hidden) == 0 && (fileInfo.Attributes & FileAttributes.System) == 0) {
                    string name = Path.GetFileName(file);
                    SDL_ttf.TTF_SetFontSize(Utils.currentFont, Ptsize);
                    SDL_ttf.TTF_SizeText(Utils.currentFont, name, out int w, out _);
                    fileButtons.Add(new FileBrowserCheckButton(new Vector2(170, fileYPos), new Vector2(w+40, Button_Height), this, Type.File, "file.png", name, Ptsize));
                    fileYPos += Space_Between_Scrollable_Buttons;
                }
            }
            Scroll_Minimum = -fileButtons.Count*Space_Between_Scrollable_Buttons + (int)size.Y;
        }
        if (IsFocused && parentProgram.scrollY != 0 && currentScroll <= Scroll_Maximum && currentScroll >= Scroll_Minimum) {
            if (currentScroll+10*parentProgram.scrollY > Scroll_Maximum) {
                parentProgram.scrollY = (Scroll_Maximum-currentScroll)/10;
            }
            if (currentScroll+10*parentProgram.scrollY < Scroll_Minimum) {
                parentProgram.scrollY = (Scroll_Minimum-currentScroll)/10;
            }
            currentScroll += 10*parentProgram.scrollY;
            currentScroll = Math.Max(Scroll_Minimum, Math.Min(Scroll_Maximum, currentScroll));
            foreach (FileBrowserCheckButton button in fileButtons) {
                button.Position += new Vector2(0, 10*parentProgram.scrollY);
            }
        }
        foreach (FileBrowserCheckButton button in fileButtons) {
            button.Update();
        }
        foreach (Button button in directoryButtons) {
            button.Update();
        }

        if (selected == null || selected?.type != Type.Folder) {
            enterDirButton.greyedOut = true;
        }
        else {
            enterDirButton.greyedOut = false;
        }
        if (selected == null) {
            selectButton.greyedOut = true;
        }
        else {
            selectButton.greyedOut = false;
        }
        if (currentParentDir == Directory.GetDirectoryRoot(currentParentDir)) {
            upDirButton.greyedOut = true;
        }
        else {
            upDirButton.greyedOut = false;
        }

        quitButton.Update();
        selectButton.Update();
        enterDirButton.Update();
        upDirButton.Update();
    }
}
class FileBrowserCheckButton : GenericUIElement, IRenderable, IAmInteractable {
    readonly string image;
    public readonly string text;
    public readonly FileBrowser.Type type;
    public int ptsize;
    bool selected;
    public FileBrowserCheckButton(Vector2 position, Vector2 size, GenericUIElement parent, FileBrowser.Type type, string image, string text, int ptsize) : base(position, size, parent) {
        this.image = image;
        this.text = text;
        this.type = type;
        selected = false;
        this.ptsize = ptsize;
    }
    public void Render(IntPtr window, IntPtr renderer) {
        SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
        // Prevent lag by not rendering if it won't be seen anyway.
        if (Position.Y > parent.size.Y || Position.Y < -size.Y) {
            return;
        }

        if (selected) {
            var r = new SDL.SDL_FRect(){x=Position.X, y=Position.Y, w=size.X, h=size.Y};
            SDL.SDL_SetRenderDrawColor(renderer, 39, 150, 214, 255);
            SDL.SDL_RenderFillRectF(renderer, ref r);
        }
        else if (GetParentWindow().IsFocused && mouseX >= Position.X && mouseX < Position.X+size.X && mouseY >= Position.Y && mouseY < Position.Y+size.Y) {
            var r = new SDL.SDL_FRect(){x=Position.X, y=Position.Y, w=size.X, h=size.Y};
            SDL.SDL_SetRenderDrawColor(renderer, 39, 150, 214, 150);
            SDL.SDL_RenderFillRectF(renderer, ref r);
        }

        IntPtr texture = SDL_image.IMG_LoadTexture(renderer, Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "textures" + Path.DirectorySeparatorChar + image);
        if (texture == IntPtr.Zero) {
            Utils.DebugLog("Could not load image");
            Utils.DebugLog(SDL.SDL_GetError());
        }

        var rect = new SDL.SDL_FRect(){x=Position.X+2.5f, y=Position.Y+2.5f, w=25, h=25};
        if (SDL.SDL_RenderCopyF(renderer, texture, (IntPtr)null, ref rect) < 0) {
            Utils.DebugLog("Error Rendering Texture");
            Utils.DebugLog(SDL.SDL_GetError());
        }

        Utils.WriteText(renderer, window, text, Utils.currentFont, Position.X+35, Position.Y+4, ptsize);
        
        SDL.SDL_DestroyTexture(texture);
    }
    public void Signal(string text) {
        throw new NotImplementedException();
    }
    public void Update() {
        SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
        if (GetParentWindow().IsFocused && GetParentWindow().parentProgram.clicked && mouseX >= Position.X && mouseX < Position.X+size.X && mouseY >= Position.Y && mouseY < Position.Y+size.Y) {
            selected = true;
            if (((FileBrowser)GetParentWindow()).selected is FileBrowserCheckButton b && b != this) {
                b.selected = false;
            }
            ((FileBrowser)GetParentWindow()).selected = this;
        }
    }
    internal override WindowRenderCombo GetParentWindow() {
        return parent.GetParentWindow();
    }
}