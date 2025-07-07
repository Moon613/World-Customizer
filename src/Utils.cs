using System;
using System.Numerics;
using SDL2;

namespace WorldCustomizer;

public static class Utils {
    public static float LerpMap(float lowerRealVal, float upperRealVal, float lowerInverseLerpVal, float upperInverseLerpVal, float x) {
        float newX = InverseLerp(x, lowerInverseLerpVal, upperInverseLerpVal);
        return Lerp(lowerRealVal, upperRealVal, newX);
    }
    public static float InverseLerp(float x, float min, float max) {
        return Math.Max(0, Math.Min(1, (x-min)/(max-min)));
    }
    public static float Lerp(float min, float max, float x) {
        return Math.Max(min, Math.Min(max, min + max * x));
    }
    public static void DrawGeometryWithVertices(IntPtr renderer, Vector2 center, SDL.SDL_Vertex[] verticies) {
        for (int i = 0; i < verticies.Length; i++) {
            verticies[i].position = new SDL.SDL_FPoint(){x=verticies[i].position.x+center.X, y=verticies[i].position.y+center.Y};
        }
        SDL.SDL_RenderGeometry(renderer, (IntPtr)null, verticies, verticies.Length, null, 0);
    }
}