using System;

namespace WorldCustomizer;

public static class Utils {
    public static float LerpMap(float lowerRealVal, float upperRealVal, float lowerInverseLerpVal, float upperInverseLerpVal, float x) {
        float newX = Math.Max(0, Math.Min(1, (x-lowerInverseLerpVal)/upperInverseLerpVal));
        return lowerRealVal + upperRealVal * newX;
    }
}