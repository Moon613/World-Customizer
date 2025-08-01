using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using SDL2;

namespace WorldCustomizer;
#nullable enable

public static class Utils {
    public static IntPtr currentFont;
    public readonly static string DebugLogPath = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "debugLog.txt";
    public static string[] registeredSlugcats = File.ReadAllLines(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "slugcats.txt");
    internal static WorldRenderer.Layers ByteToLayer(byte l) {
        WorldRenderer.Layers layer = l switch
        {
            1 => WorldRenderer.Layers.Layer2,
            2 => WorldRenderer.Layers.Layer3,
            _ => WorldRenderer.Layers.Layer1,
        };
        return layer;
    }
    public static float Magnitude(this Vector2 vector) {
        return (float)Math.Sqrt(Math.Pow(vector.X, 2) + Math.Pow(vector.Y, 2));
    }
    public static void SetPixel(IntPtr surface, int x, int y, SDL.SDL_Color color) {
        SetPixel(surface, x, y, color.r, color.g, color.b, color.a);
    }
    public static unsafe void SetPixel(IntPtr surface, int x, int y, byte r, byte g, byte b, byte a = 255) {
        SDL.SDL_LockSurface(surface);

        byte* pixelArray = (byte*)((SDL.SDL_Surface*)surface)->pixels;
        byte bytesPerPixel = ((SDL.SDL_PixelFormat*)((SDL.SDL_Surface*)surface)->format)->BytesPerPixel;
        int pitch = ((SDL.SDL_Surface*)surface)->pitch;
        pixelArray[y*pitch + x*bytesPerPixel + 0] = r;
        pixelArray[y*pitch + x*bytesPerPixel + 1] = g;
        pixelArray[y*pitch + x*bytesPerPixel + 2] = b;
        pixelArray[y*pitch + x*bytesPerPixel + 3] = a;

        SDL.SDL_UnlockSurface(surface);
    }
    public static void DebugLog(Vector2 num) {
        DebugLog(num.ToString());
    }
    public static void DebugLog(float num) {
        DebugLog(num.ToString());
    }
    public static void DebugLog(int num) {
        DebugLog(num.ToString());
    }
    public static void DebugLog(byte num) {
        DebugLog(num.ToString());
    }
    public static void DebugLog(Exception err) {
        DebugLog(err.ToString());
    }
    public static void DebugLog(string text) {
        Console.WriteLine(text);
        File.AppendAllText(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "debugLog.txt", text+"\n");
    }
    public static float LerpMap(float lowerRealVal, float upperRealVal, float lowerInverseLerpVal, float upperInverseLerpVal, float x) {
        float newX = InverseLerp(x, lowerInverseLerpVal, upperInverseLerpVal);
        return Lerp(lowerRealVal, upperRealVal, newX);
    }
    public static float InverseLerp(float x, float min, float max) {
        return Math.Max(0, Math.Min(1, (x-min)/(max-min)));
    }
    public static float Lerp(float min, float max, float x) {
        return Math.Max(min, Math.Min(max, min + (max-min) * x));
    }
    public static void DrawGeometryWithVertices(IntPtr renderer, Vector2 center, SDL.SDL_Vertex[] verticies) {
        for (int i = 0; i < verticies.Length; i++) {
            verticies[i].position = new SDL.SDL_FPoint(){x=verticies[i].position.x+center.X, y=verticies[i].position.y+center.Y};
        }
        SDL.SDL_RenderGeometry(renderer, (IntPtr)null, verticies, verticies.Length, null!, 0);
    }
    /// <summary>
    /// Draws text to the screen, given a string and font, and has optional parameters for position, size, and text foreground color.<br />
    /// If Color is left null, the text will be white.
    /// </summary>
    internal static void WriteText(IntPtr renderer, IntPtr window, string text, IntPtr font, float x = 0, float y = 0, int ptsize = 24, SDL.SDL_Color? color = null, float w = 0, float h = 0) {
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
            convertedColor.r = color.Value.r;
            convertedColor.g = color.Value.g;
            convertedColor.b = color.Value.b;
            convertedColor.a = color.Value.a;
        }

        // as TTF_RenderText_Solid could only be used on
        // SDL_Surface then you have to create the surface first
        IntPtr surfaceMessage = SDL_ttf.TTF_RenderUTF8_Solid_Wrapped(currentFont, text, convertedColor, 0);

        // now you can convert it into a texture
        IntPtr Message = SDL.SDL_CreateTextureFromSurface(renderer, surfaceMessage);

        SDL.SDL_FRect Message_rect = new SDL.SDL_FRect() {x = x, y = y, w = w, h = h}; //create a rect

        // (0,0) is on the top left of the window/screen,
        // think a rect as the text's box,
        // that way it would be very simple to understand

        // Now since it's a texture, you have to put RenderCopy
        // in your game loop area, the area where the whole code executes

        // you put the renderer's name first, the Message,
        // the crop size (you can ignore this if you don't want
        // to dabble with cropping), and the rect which is the size
        // and coordinate of your texture
        SDL.SDL_RenderCopyF(renderer, Message, (IntPtr)null, ref Message_rect);

        // Don't forget to free your surface and texture
        SDL.SDL_FreeSurface(surfaceMessage);
        SDL.SDL_DestroyTexture(Message);
    }
}