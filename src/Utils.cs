using System;

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
}